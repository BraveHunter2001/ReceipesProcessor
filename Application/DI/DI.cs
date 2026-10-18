using Application.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DI;

public static class DI
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string tesseractPath)
    {
        services.AddScoped<ReceiptTextService>();
        services.AddSingleton<TextExctractor>(e => new TextExctractor(tesseractPath));

        return services;
    }
}
