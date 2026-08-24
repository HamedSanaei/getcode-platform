using GetCode.Api.Endpoints;
using GetCode.Api.Middleware;
using GetCode.Contracts.System;
using GetCode.Infrastructure;
using GetCode.Infrastructure.Observability.Logging;
using GetCode.Persistence;
using Serilog;

const string serviceName = "getcode-api";
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var logRoot = Environment.GetEnvironmentVariable("LogStorage__RootPath") ?? "./logs";
var instanceId = StructuredLogging.GetInstanceId();
var appVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev";
Log.Logger = StructuredLogging.CreateBootstrapLogger(serviceName, instanceId, logRoot, environment, appVersion);

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(Log.Logger, dispose: false);

    builder.Services.AddOpenApi();
    builder.Services.Configure<SiteHostOptions>(builder.Configuration.GetSection(SiteHostOptions.SectionName));
    builder.Services.AddScoped<CurrentSiteAccessor>();
    builder.Services.AddScoped<GetCode.Application.SiteHosts.ICurrentSite>(sp => sp.GetRequiredService<CurrentSiteAccessor>());
    builder.Services.AddGetCodePersistence(builder.Configuration);
    builder.Services.AddGetCodeInfrastructure(builder.Configuration, serviceName, instanceId, builder.Environment.IsDevelopment());
    // Identity use cases (session/cookie strategy arrives with M02-002; no public endpoints yet).
    builder.Services.AddScoped<GetCode.Application.Identity.IdentityService>();
    // Catalog admin/read use cases (public catalog API surface arrives with the storefront milestone).
    builder.Services.AddScoped<GetCode.Application.Catalog.CatalogAdminService>();
    builder.Services.AddScoped<GetCode.Application.Catalog.CatalogQueryService>();
    builder.Services.AddScoped<GetCode.Application.Catalog.ProductSkuAdminService>();
    builder.Services.AddScoped<GetCode.Application.Catalog.ProductCatalogQueryService>();
    builder.Services.AddScoped<GetCode.Application.Providers.ProviderAdminService>();
    // Authorization administration (M02-004): deny-by-default, privilege changes audited via outbox.
    builder.Services.AddScoped<GetCode.Application.Authorization.AuthorizationAdminService>();
    builder.Services.AddScoped<GetCode.Application.Authorization.IAuthorizationService, GetCode.Application.Authorization.EffectiveAuthorizationService>();
    // Wallet use cases (M05-003): ledger-based, idempotent, optimistic-concurrency guarded money mutations.
    builder.Services.AddScoped<GetCode.Application.Wallets.WalletService>();

    var app = builder.Build();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SiteHostResolutionMiddleware>();
    app.UseSerilogRequestLogging();

    // Public API contract surface (M03-004): OpenAPI document served in all environments.
    app.MapOpenApi();
    app.MapCatalogEndpoints();
    app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
    app.MapGet("/", (IHostEnvironment env) => Results.Ok(new ApiInfoResponse("getcode-api", typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev", env.EnvironmentName))).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "host.terminated_unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
