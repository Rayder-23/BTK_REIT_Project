using BTK_REIT_Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BTK_REIT_UI.Services;

/// <summary>
/// Custom AuthenticationStateProvider backed by a simple session token.
/// Notified by AuthService whenever login/logout occurs.
/// </summary>
public class ReitAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public void NotifyAuthChanged(UserSession? session)
    {
        if (session is not null)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Name,           session.UserName),
                new Claim("SecurityLevel",           session.SecurityLevel.ToString()),
            ], "btk-auth");

            _currentUser = new ClaimsPrincipal(identity);
        }
        else
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }
}
