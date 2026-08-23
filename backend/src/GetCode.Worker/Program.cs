using GetCode.Infrastructure;
using GetCode.Infrastructure.Observability.Logging;
using GetCode.Persistence;
using GetCode.Worker;
using Serilog;

const string serviceName = "getcode-worker";
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var logRoot = Environment.GetEnvironmentVariable("LogStorage__RootPath") ?? "./logs";
var instanceId = StructuredLogging.GetInstanceId();
var appVersion = typeof(WorkerHeartbeat).Assembly.GetName().Version?.ToString() ?? "dev";
Log.Logger = StructuredLogging.CreateBootstrapLogger(serviceName, instanceId, logRoot, environment, appVersion);

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog(Log.Logger, dispose: false);
    builder.Services.AddGetCodePersistence(builder.Configuration);
    builder.Services.AddGetCodeInfrastructure(builder.Configuration, serviceName, instanceId, builder.Environment.IsDevelopment());
    builder.Services.AddHostedService<WorkerHeartbeat>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "worker.terminated_unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
