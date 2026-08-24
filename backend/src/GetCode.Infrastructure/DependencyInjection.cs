using GetCode.Application.Common;
using GetCode.Application.Identity;
using GetCode.Application.Notifications;
using GetCode.Application.Providers;
using GetCode.Domain.Identity;
using GetCode.Infrastructure.Common;
using GetCode.Infrastructure.Identity;
using GetCode.Infrastructure.Observability.Logging;
using GetCode.Infrastructure.Notifications.Sms.Kavenegar;
using GetCode.Infrastructure.Providers.Fake;
using GetCode.Infrastructure.Providers.FiveSim;
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

        // M04-002: 5SIM virtual-number adapter — secret-driven, opt-in.
        var fiveSim = configuration.GetSection(FiveSimOptions.SectionName).Get<FiveSimOptions>() ?? new FiveSimOptions();
        if (fiveSim.Enabled)
        {
            services.AddSingleton(fiveSim);
            services.AddHttpClient(FiveSimVirtualNumberProvider.ProviderKeyValue, client =>
                {
                    client.BaseAddress = new Uri(fiveSim.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(fiveSim.TimeoutSeconds, 1, 120));
                })
                .AddTypedClient<FiveSimVirtualNumberProvider>()
                .Services.AddSingleton<IVirtualNumberProvider>(sp => sp.GetRequiredService<FiveSimVirtualNumberProvider>());
        }

        // M04-008: Kavenegar outbound user-SMS adapter — secret-driven, opt-in.
        var kavenegar = configuration.GetSection(KavenegarOptions.SectionName).Get<KavenegarOptions>() ?? new KavenegarOptions();
        if (kavenegar.Enabled)
        {
            services.AddSingleton(kavenegar);
            services.AddHttpClient("kavenegar-sms", client =>
                {
                    client.BaseAddress = new Uri(kavenegar.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(kavenegar.TimeoutSeconds, 1, 120));
                })
                .AddTypedClient<KavenegarSmsNotificationSender>()
                .Services.AddSingleton<ISmsNotificationPort>(sp => sp.GetRequiredService<KavenegarSmsNotificationSender>());
        }

        return services;
    }
}
