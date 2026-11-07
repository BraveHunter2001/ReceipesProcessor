using Application.Configuration;
using Application.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DI;

public static class DI
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        string tesseractPath,
        IConfiguration configuration)
    {
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));
        services.AddScoped<ReceiptTextService>();
        services.AddScoped<ReceiptService>();
        services.AddSingleton<TextExctractor>(e => new TextExctractor(tesseractPath));

        return services;
    }
}
