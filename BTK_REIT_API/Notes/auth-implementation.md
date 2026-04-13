# BTK REIT — Authentication Implementation Report

**Date:** 2026-04-13  
**Scope:** BTK_REIT_UI + BTK_REIT_API + BTK_REIT_Shared  
**Credentials:** `adminRoot` / `adminR123`

---

## Overview

A session-based authentication system was implemented across the three-project solution. The API validates credentials and issues a session token. The UI persists the session in browser local storage and enforces route protection entirely within Blazor's rendering pipeline — no ASP.NET Core HTTP authentication middleware is involved on the UI side.

---

## Architecture

```
Browser (Local Storage)
    │  btk_session → UserSession JSON
    │
    ▼
BTK_REIT_UI (Blazor Server)
    ├── AuthInitializer.razor       — rehydrates session on first render
    ├── Services/AuthService.cs     — owns CurrentSession, drives login/logout
    ├── Services/ReitAuthStateProvider.cs  — supplies ClaimsPrincipal to Blazor
    ├── Components/Routes.razor     — AuthorizeRouteView + <NotAuthorized> redirect
    └── Components/Pages/Home.razor — login form (EmptyLayout, AllowAnonymous)
         │
         │  POST /api/auth/login
         ▼
BTK_REIT_API (ASP.NET Core Web API)
    └── Controllers/AuthController.cs — queries AdminUsers, returns UserSession
```

---

## Files Changed

### BTK_REIT_Shared

| File | Change |
|------|--------|
| `DTOs/LoginRequest.cs` | New — `UserName` + `Password` with `[Required]` validation |
| `DTOs/UserSession.cs` | New — `UserId`, `UserName`, `SecurityLevel`, `Token` |

### BTK_REIT_API

| File | Change |
|------|--------|
| `Controllers/AuthController.cs` | New — `POST /api/auth/login` endpoint |

### BTK_REIT_UI

| File | Change |
|------|--------|
| `Services/ReitAuthStateProvider.cs` | New — custom `AuthenticationStateProvider` |
| `Services/AuthService.cs` | New — session bridge between localStorage and auth state |
| `Services/NoOpAuthHandler.cs` | New — present but unused; left in place |
| `Components/AuthInitializer.razor` | New — invisible `InteractiveServer` component for session rehydration |
| `Components/RedirectToLogin.razor` | New — navigates to `/` when `<NotAuthorized>` fires |
| `Components/Layout/EmptyLayout.razor` | New — blank layout for the login page (no sidebar/topbar) |
| `Components/Layout/TopbarUser.razor` | New — `InteractiveServer` child for logout button and username display |
| `Components/Layout/MainLayout.razor` | Updated — `@rendermode` removed; `<TopbarUser />` replaces inline auth code |
| `Components/Pages/Home.razor` | Rewritten — login form with show-password toggle; `@layout EmptyLayout`, `[AllowAnonymous]` |
| `Components/Routes.razor` | Updated — `AuthorizeRouteView` with `<NotAuthorized>` and `<Authorizing>` slots |
| `Components/App.razor` | Updated — `<AuthInitializer />` and `<CascadingAuthenticationState>` wrapper |
| `Program.cs` | Updated — auth services registered (see below) |
| `BTK_REIT_UI.csproj` | Updated — `Blazored.LocalStorage 4.5.0` added |

---

## API Endpoint

### `POST /api/auth/login`

**Request body** (`LoginRequest`):
```json
{ "userName": "adminRoot", "password": "adminR123" }
```

**Response 200 OK** (`UserSession`):
```json
{
  "userId": 1,
  "userName": "adminRoot",
  "securityLevel": 1,
  "token": "base64-guid-string"
}
```

**Response 401 Unauthorized:**
```json
{ "message": "Invalid username or password." }
```

**Logic:**
1. Query `AdminUsers` table for matching `UserName` + `Password` (plain-text comparison — see Security Notes)
2. If found: update `LastLogin` timestamp, return `UserSession` with a `Guid`-derived token
3. If not found: return `401 Unauthorized`

---

## UI Auth Flow

### Login
1. User submits credentials on `Home.razor` (`/`)
2. `POST api/auth/login` is called via `HttpClient`
3. On success: `AuthService.LoginAsync(session)` is called
   - Writes `UserSession` JSON to localStorage key `btk_session`
   - Calls `ReitAuthStateProvider.NotifyAuthChanged(session)` which builds a `ClaimsPrincipal` with `NameIdentifier`, `Name`, and `SecurityLevel` claims
4. Blazor re-renders; `Nav.NavigateTo("/properties")` redirects the user

### Session Rehydration (page refresh)
1. `AuthInitializer.razor` fires `OnAfterRenderAsync(firstRender: true)`
2. `AuthService.InitializeAsync()` reads `btk_session` from localStorage
3. If a session exists, `ReitAuthStateProvider.NotifyAuthChanged(session)` restores the authenticated state silently

### Logout
1. Topbar logout button (arrow-out icon) calls `AuthService.LogoutAsync()`
2. `btk_session` is removed from localStorage
3. `ReitAuthStateProvider.NotifyAuthChanged(null)` sets an empty `ClaimsPrincipal`
4. `Nav.NavigateTo("/")` sends the user back to the login page

### Route Protection
- `Routes.razor` uses `AuthorizeRouteView` instead of `RouteView`
- When a user navigates to any route unauthenticated, `<NotAuthorized>` renders `<RedirectToLogin />`, which calls `Nav.NavigateTo("/")`
- `Home.razor` carries `@attribute [AllowAnonymous]` so it is always accessible
- No `[Authorize]` attributes are placed on protected pages — `AuthorizeRouteView` protects all routes by default

---

## Service Registration (`Program.cs`)

```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ReitAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ReitAuthStateProvider>());
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthService>();
```

**Why no `AddAuthentication()`:**  
Blazor Server's `AuthorizeRouteView` reads auth state from the cascaded `AuthenticationState` (supplied by `ReitAuthStateProvider`), not from ASP.NET Core's HTTP authentication middleware. Adding `AddAuthentication` with any scheme — including a no-op — causes the HTTP pipeline's challenge/forbid flow to intercept Blazor route rendering, producing spurious 401 or 403 responses before the Blazor circuit gets to evaluate anything.

---

## Key Design Decisions

### Layout isolation for the login page
`MainLayout` renders the full shell (sidebar + topbar). The login page must not inherit this — it uses `@layout EmptyLayout`, a minimal layout that renders only `@Body`. This means the sidebar is invisible to unauthenticated users and cannot be used to navigate around the login gate.

### `@rendermode` cannot be set on layouts
Blazor layouts receive `@Body` as a `RenderFragment` parameter. `RenderFragment` is a delegate (arbitrary code) and cannot be serialized across a Blazor Server circuit boundary. Setting `@rendermode InteractiveServer` on a layout therefore throws:

> `InvalidOperationException: Cannot pass the parameter 'Body' to component 'MainLayout' with rendermode 'InteractiveServerRenderMode'`

The solution is to keep layouts render-mode-free and extract any interactive behaviour (logout button, username display) into a dedicated child component — `TopbarUser.razor` — which declares its own `@rendermode InteractiveServer`.

### `[Authorize]` on Blazor pages causes 403
Placing `@attribute [Microsoft.AspNetCore.Authorization.Authorize]` on a Blazor page component looks correct but interacts badly with a custom auth setup that has no HTTP authentication scheme. The attribute is evaluated by the HTTP middleware layer *before* Blazor rendering begins. With no configured scheme the middleware issues a 403 Forbidden response. `AuthorizeRouteView` is the correct and sufficient mechanism for Blazor Server route protection — no page-level `[Authorize]` attributes are needed.

---

## Known Limitations / Security Notes

- **Passwords are stored and compared as plain text.** This is acceptable for an internal management system in the current development phase but must be replaced with `BCrypt` or `PBKDF2` hashing before any production deployment.
- **The session token is a base64-encoded `Guid`.** It is not a signed JWT. It is not validated on subsequent requests — the UI trusts its own localStorage. This is sufficient for a Blazor Server app where the circuit is server-side, but API endpoints are currently open (no bearer token validation on the API side).
- **No token expiry.** Sessions persist in localStorage until the user explicitly logs out. A `LoginTime` field on `UserSession` and a client-side expiry check in `AuthService.InitializeAsync()` would close this gap.
- **API endpoints are unauthenticated.** Any HTTP client that knows the port can call `GET /api/properties` etc. without a token. A future phase should add JWT bearer validation to the API and have the UI attach the session token as an `Authorization: Bearer` header on every `HttpClient` request.
