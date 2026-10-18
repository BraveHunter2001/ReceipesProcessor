using Application.DI;
using Infrastructure.DI;
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
    app.MapGet("/api/test", () => "Hi world from beck");

    await app.RunAsync();
} catch (Exception ex)
{
    Log.Logger.Fatal(ex.ToString());
}

