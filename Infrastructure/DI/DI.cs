using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DI;

public static class DI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
        });
        services.AddSingleton<S3StorageProvider>();
        services.Configure<S3Settings>(configuration.GetSection(S3Settings.SectionName));
        services.Configure<PaddleOcrSettings>(configuration.GetSection(PaddleOcrSettings.SectionName));
        services.AddHttpClient<PaddleOcrClient>();
   
        return services;
    }
}
