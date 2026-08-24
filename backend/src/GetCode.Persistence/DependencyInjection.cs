using GetCode.Application.Catalog;
using GetCode.Application.Identity;
using GetCode.Persistence.Catalog;
using GetCode.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddGetCodePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<GetCodeDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIdentityAuditTrail, IdentityAuditTrail>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IProductSkuRepository, ProductSkuRepository>();
        services.AddScoped<GetCode.Application.Providers.IProviderRepository, ProviderRepository>();
        services.AddScoped<GetCode.Application.Providers.IProviderMappingRepository, ProviderMappingRepository>();
        services.AddScoped<IOutboxCollector, OutboxCollector>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();
        return services;
    }
}
