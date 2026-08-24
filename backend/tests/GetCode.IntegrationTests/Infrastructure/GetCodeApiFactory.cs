using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GetCode.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory wired to the isolated PostgreSQL container from the
/// "database" collection. The production composition root is used unchanged
/// except for the DbContext connection string, so tests exercise the real
/// persistence registration path.
/// </summary>
public sealed class GetCodeApiFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", database.ConnectionString);
        builder.ConfigureTestServices(services =>
        {
            // Replace only the options registration; keep everything else identical to production.
            services.RemoveAll(typeof(DbContextOptions<GetCodeDbContext>));
            services.AddDbContext<GetCodeDbContext>(options => options.UseNpgsql(database.ConnectionString));
        });
    }
}
