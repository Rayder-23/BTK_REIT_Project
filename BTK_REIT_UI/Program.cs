using Blazored.LocalStorage;
using BTK_REIT_UI.Components;
using BTK_REIT_UI.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ── Razor + Blazor Server ────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Authorization infrastructure ────────────────────────────────────────────
// No AddAuthentication — Blazor Server uses AuthorizeRouteView + AuthenticationStateProvider
// for route protection, not ASP.NET Core's HTTP authentication middleware.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ReitAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ReitAuthStateProvider>());

// ── Local storage (Blazored) + Auth service ──────────────────────────────────
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthService>();

// ── HTTP client pointing at the BTK REIT API ────────────────────────────────
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5039") });

var app = builder.Build();

// ── HTTP pipeline ────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
