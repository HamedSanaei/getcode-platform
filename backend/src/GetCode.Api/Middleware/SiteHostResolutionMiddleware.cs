using GetCode.Application.SiteHosts;
using Microsoft.Extensions.Options;

namespace GetCode.Api.Middleware;

public sealed class SiteHostResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOptions<SiteHostOptions> options, CurrentSiteAccessor accessor, IHostEnvironment environment)
    {
        var configured = options.Value;
        var host = context.Request.Host.Host;
        var entry = configured.Hosts.FirstOrDefault(x => string.Equals(x.Host, host, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            if (configured.RejectUnknownHosts && !environment.IsDevelopment())
            {
                context.Response.StatusCode = StatusCodes.Status421MisdirectedRequest;
                return;
            }

            entry = configured.Hosts.FirstOrDefault(x => x.Key == configured.CanonicalKey) ?? configured.Hosts.FirstOrDefault();
        }

        if (entry is null || !Uri.TryCreate(entry.PublicBaseUrl, UriKind.Absolute, out var publicBaseUri))
        {
            throw new InvalidOperationException("SiteHosts configuration must contain at least one valid host.");
        }

        accessor.Set(new SiteDescriptor(entry.Key, entry.Host, publicBaseUri, entry.BrandKey, entry.IsCanonical));
        await next(context);
    }
}
