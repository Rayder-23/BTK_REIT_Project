using Blazored.LocalStorage;
using BTK_REIT_Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BTK_REIT_UI.Services;

/// <summary>
/// Scoped service that owns the current session and bridges
/// Blazored.LocalStorage ↔ AuthenticationStateProvider.
/// </summary>
public class AuthService
{
    private const string SessionKey = "btk_session";

    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authProvider;

    public UserSession? CurrentSession { get; private set; }

    public AuthService(ILocalStorageService localStorage, AuthenticationStateProvider authProvider)
    {
        _localStorage = localStorage;
        _authProvider  = authProvider;
    }

    /// <summary>Called once on app start to rehydrate session from local storage.</summary>
    public async Task InitializeAsync()
    {
        var saved = await _localStorage.GetItemAsync<UserSession>(SessionKey);
        if (saved is not null)
        {
            CurrentSession = saved;
            ((ReitAuthStateProvider)_authProvider).NotifyAuthChanged(saved);
        }
    }

    public async Task LoginAsync(UserSession session)
    {
        CurrentSession = session;
        await _localStorage.SetItemAsync(SessionKey, session);
        ((ReitAuthStateProvider)_authProvider).NotifyAuthChanged(session);
    }

    public async Task LogoutAsync()
    {
        CurrentSession = null;
        await _localStorage.RemoveItemAsync(SessionKey);
        ((ReitAuthStateProvider)_authProvider).NotifyAuthChanged(null);
    }
}
