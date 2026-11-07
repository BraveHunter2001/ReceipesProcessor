using Application.Configuration;
using Application.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application.DI;

public static class DI
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        string tesseractPath,
        IConfiguration configuration)
    {
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));
        services.Configure<ReceiptProcessingSettings>(configuration.GetSection(ReceiptProcessingSettings.SectionName));
        services.AddHttpClient("Ollama", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddScoped<ReceiptTextService>();
        services.AddScoped<ReceiptService>();
        services.AddSingleton<TextExctractor>(provider =>
            new TextExctractor(tesseractPath, provider.GetRequiredService<Infrastructure.PaddleOcrClient>()));

        return services;
    }
}
