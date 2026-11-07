using Application.DI;
using Application.Service;
using Infrastructure.DI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReceipesProcessor.BackgroundServices;
using Serilog;
using Telegram.Bot;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
try
{
    Log.Logger.Information("Start hosting app");

    var builder = WebApplication.CreateBuilder(args);

    bool telegramBotEnabled = builder.Configuration.GetValue("TelegramBot:Enabled", false);
    string dbConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionStrings.DefaultConnection");
    string tessdataFolderName = builder.Configuration.GetSection("TesseractSettings:FolderName").Value ?? "tessdata";
    string absoluteTessdataPath = Path.Combine(AppContext.BaseDirectory, tessdataFolderName);

    builder.Services.AddInfrastructure(dbConnection);
    builder.Services.AddApplication(absoluteTessdataPath, builder.Configuration);
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));
    if (telegramBotEnabled)
    {
        string token = builder.Configuration.GetRequiredSection("botToken").Value!;
        builder.Services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(token));
        builder.Services.AddHostedService<TelegramBotBackgroundService>();
    }
    builder.Services.AddHostedService<ReceiptExctractTextBackgroundService>();
    builder.Services.AddHostedService<ReceiptProccedBackgroundService>();

    var app = builder.Build();
    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
    app.MapGet("/api/receipts", async ([FromServices] ReceiptService service) =>
    {
        var reseponse = await service.GetList();
        return Results.Ok(reseponse);
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

