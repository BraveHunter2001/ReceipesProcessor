using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI;

public static class DI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
        });
        services.AddSingleton<S3StorageProvider>();

        return services;
    }
}
