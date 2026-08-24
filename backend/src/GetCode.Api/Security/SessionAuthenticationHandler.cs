using System.Security.Claims;
using System.Text.Encodings.Web;
using GetCode.Application.Identity;
using GetCode.Application.SiteHosts;
using Microsoft.AspNetCore.Authentication;
using AuthResult = Microsoft.AspNetCore.Authentication.AuthenticateResult;
using Microsoft.Extensions.Options;

namespace GetCode.Api.Security;

/// <summary>
/// M09-001: turns the host-scoped session cookie into a ClaimsPrincipal.
///
/// This is authentication only — it answers "which user is this?". What that
/// user may do is decided exclusively by authorization policies backed by
/// IAuthorizationService (deny-by-default effective permissions). The frontend
/// receives capability information purely for navigation UX; it is never a
/// security boundary.
/// </summary>
public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SessionService sessions,
    ICurrentSite currentSite)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";

    protected override async Task<AuthResult> HandleAuthenticateAsync()
    {
        var token = Request.Cookies[Endpoints.AuthEndpoints.CookieNameFor(currentSite.Site.Key)];
        if (string.IsNullOrEmpty(token))
        {
            return AuthResult.NoResult();
        }

        var validation = await sessions.ValidateAsync(token, currentSite.Site.Key, Context.RequestAborted);
        if (validation is not SessionValidationResult.Success success)
        {
            return AuthResult.Fail("session invalid for this host");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, success.UserId.ToString()),
            new("gc.session", success.SessionId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
