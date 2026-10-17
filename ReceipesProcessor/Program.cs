using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReceipesProcessor.Service;
using Serilog;
using Telegram.Bot;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Logger.Information("Start hosting app");

    var builder = Host.CreateApplicationBuilder(args);
    string token = builder.Configuration.GetRequiredSection("botToken").Value!;

    builder.Services.AddSerilog();
    builder.Services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(token));
    builder.Services.AddScoped<ReceiptService>();
    builder.Services.AddScoped<TextExctractor>();
    builder.Services.AddHostedService<TelegramBotBackgroundService>();

    var host = builder.Build();
    await host.RunAsync();
} catch (Exception ex)
{
    Log.Logger.Fatal(ex.ToString());
}

