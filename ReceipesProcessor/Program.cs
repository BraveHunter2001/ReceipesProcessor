using Application.DI;
using Application.Service;
using Infrastructure.DI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReceipesProcessor.BackgroundServices;
using Serilog;
using Telegram.Bot;

try
{
    var builder = WebApplication.CreateBuilder(args);
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
    Log.Logger.Information("Start hosting app");

    var telegramSettings = builder.Configuration.GetSection("TelegramBot").Get<ReceipesProcessor.Configuration.TelegramBotSettings>() ?? new();
    telegramSettings.Token ??= builder.Configuration["botToken"];
    string dbConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionStrings.DefaultConnection");
    string tessdataFolderName = builder.Configuration.GetSection("TesseractSettings:FolderName").Value ?? "tessdata";
    string absoluteTessdataPath = Path.Combine(AppContext.BaseDirectory, tessdataFolderName);

    builder.Services.AddInfrastructure(dbConnection, builder.Configuration);
    builder.Services.AddApplication(absoluteTessdataPath, builder.Configuration);
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));
    if (telegramSettings.Enabled)
    {
        if (string.IsNullOrWhiteSpace(telegramSettings.Token))
            throw new InvalidOperationException("TelegramBot:Token (or legacy botToken) is required when TelegramBot:Enabled=true.");
        string token = telegramSettings.Token;
        builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        builder.Services.AddHostedService<TelegramBotBackgroundService>();
    }
    builder.Services.AddHostedService<ReceiptExctractTextBackgroundService>();
    builder.Services.AddHostedService<ReceiptProccedBackgroundService>();

    var app = builder.Build();
    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
    app.MapGet("/api/receipts", async (
        int? page,
        int? pageSize,
        [FromServices] ReceiptService service,
        CancellationToken cancellationToken) =>
    {
        var requestedPage = page ?? 1;
        var requestedPageSize = pageSize ?? 10;
        if (requestedPage < 1 || requestedPageSize < 1 || requestedPageSize > 50)
        {
            return Results.BadRequest(new { error = "page должен быть не меньше 1, а pageSize — от 1 до 50." });
        }

        var response = await service.GetListAsync(requestedPage, requestedPageSize, cancellationToken);
        return Results.Ok(response);
    });
    app.MapGet("/api/receipts/{id:int}", async (
        int id,
        [FromServices] ReceiptService service,
        CancellationToken cancellationToken) =>
    {
        var receipt = await service.GetDetailsAsync(id, cancellationToken);
        return receipt is null ? Results.NotFound() : Results.Ok(receipt);
    });
    app.MapGet("/api/receipts/{id:int}/image", async (
        int id,
        [FromServices] ReceiptService service) =>
    {
        try
        {
            var image = await service.GetImageAsync(id);
            return Results.Stream(image.FileStream, image.ContentType);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    });
    app.MapPost("/api/receipts/upload", async (
        IFormFile file,
        [FromServices] ReceiptService service,
        CancellationToken cancellationToken) =>
    {
        if (file.Length == 0 || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Загрузите непустой файл изображения." });
        }

        await using var stream = file.OpenReadStream();
        var receipt = await service.CreateFromImageAsync(stream, file.ContentType, cancellationToken);
        return Results.Ok(receipt);
    // TODO: вернуть antiforgery-защиту после добавления cookie-аутентификации
    // и передачи токена из webclient.
    }).DisableAntiforgery();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex.ToString());
}

