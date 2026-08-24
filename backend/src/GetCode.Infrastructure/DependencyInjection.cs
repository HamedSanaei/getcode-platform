using GetCode.Application.Common;
using GetCode.Application.Identity;
using GetCode.Application.Providers;
using GetCode.Domain.Identity;
using GetCode.Infrastructure.Common;
using GetCode.Infrastructure.Identity;
using GetCode.Infrastructure.Observability.Logging;
using GetCode.Infrastructure.Providers.Fake;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGetCodeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string instanceId,
        bool enableFakeProvider)
    {
        services.Configure<LogStorageOptions>(configuration.GetSection(LogStorageOptions.SectionName));
        services.AddSingleton(new LogServiceIdentity(serviceName, instanceId));
        services.AddHostedService<LogArchiveHostedService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISessionTokenProvider, CryptographicSessionTokens>();
        services.AddSingleton(CredentialPolicy.Default);

        if (enableFakeProvider)
        {
            services.AddSingleton<IVirtualNumberProvider, FakeVirtualNumberProvider>();
        }

        return services;
    }
}
