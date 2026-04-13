using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace BTK_REIT_UI.Services;

/// <summary>
/// Satisfies the IAuthenticationService dependency required by AuthorizeRouteView.
/// Auth state is supplied entirely by ReitAuthStateProvider / AuthService —
/// this handler is never actually invoked for challenge or sign-in.
/// </summary>
public class NoOpAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoOpAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Return a successful but anonymous ticket so ASP.NET Core's middleware
        // does not issue a 401 challenge. Blazor's AuthorizeRouteView reads the
        // ClaimsPrincipal from the cascaded AuthenticationState (ReitAuthStateProvider)
        // — not from this ticket — so authorization still works correctly.
        var principal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity());
        var ticket = new AuthenticationTicket(principal, "BtkScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
