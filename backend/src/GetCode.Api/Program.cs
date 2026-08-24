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

    var app = builder.Build();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SiteHostResolutionMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

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
