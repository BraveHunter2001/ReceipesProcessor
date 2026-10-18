using Application.DI;
using Infrastructure;
using Infrastructure.DI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReceipesProcessor.BackgroundServices;
using ReceipesProcessor.Response;
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

    string token = builder.Configuration.GetRequiredSection("botToken").Value!;
    string dbConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionStrings.DefaultConnection");
    string tessdataFolderName = builder.Configuration.GetSection("TesseractSettings:FolderName").Value ?? "tessdata";
    string absoluteTessdataPath = Path.Combine(AppContext.BaseDirectory, tessdataFolderName);

    builder.Services.AddInfrastructure(dbConnection);
    builder.Services.AddApplication(absoluteTessdataPath);
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));
    builder.Services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(token));


    builder.Services.AddHostedService<TelegramBotBackgroundService>();
    builder.Services.AddHostedService<ReceiptExctractTextBackgroundService>();
    builder.Services.AddHostedService<ReceiptProccedBackgroundService>();

    var app = builder.Build();
    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
    app.MapGet("/api/receipts", async ([FromServices] ApplicationContext context) =>
    {
        var receipts = await context.Receipts.Include(r=>r.Items).OrderByDescending(r => r.Id).ToListAsync();
        var reseponse = receipts.ConvertAll(r => new ReceiptsListItem(r));
        return Results.Ok(reseponse);
    });

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex.ToString());
}

