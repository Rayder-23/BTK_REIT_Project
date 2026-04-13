# BTK_REIT_UI

Blazor Server (.NET 10) dashboard frontend for the BTK REIT Management System.

## Tech Stack

- **Framework**: Blazor Server (.NET 10)
- **Render mode**: `InteractiveServer` (server-side SignalR connection)
- **Styling**: Bootstrap 5 + custom REIT dashboard CSS (`wwwroot/app.css`)
- **Shared contracts**: `BTK_REIT_Shared` (project reference)
- **API communication**: `HttpClient` injected via DI, base address points to `BTK_REIT_API`

## Prerequisites

- .NET 10 SDK
- `BTK_REIT_API` running on `http://localhost:5039`

## Run

```bash
cd BTK_REIT_UI
dotnet run
# https://localhost:7235
# http://localhost:5221
```

The API must be started first. See [BTK_REIT_API/README.md](../BTK_REIT_API/README.md) for API setup.

## Project Layout

```
BTK_REIT_UI/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        Shell: sidebar + topbar + content area
│   │   ├── MainLayout.razor.css    (scoped — not used)
│   │   ├── NavMenu.razor           9-domain sidebar navigation
│   │   └── NavMenu.razor.css       Scoped sidebar styles
│   ├── Pages/
│   │   ├── Home.razor              Landing page  (/)
│   │   ├── Shareholders.razor      Shareholder list  (/shareholders)
│   │   ├── Counter.razor           Demo counter  (/counter)
│   │   ├── Weather.razor           Demo weather  (/weather)
│   │   ├── NotFound.razor          404 handler
│   │   └── Error.razor             Unhandled error page
│   ├── App.razor                   Root HTML document
│   ├── Routes.razor                Router + layout assignment
│   └── _Imports.razor              Global @using directives
├── wwwroot/
│   ├── app.css                     Global styles + REIT layout + table/badge utilities
│   └── favicon.png
├── Program.cs                      DI registration, middleware pipeline
└── appsettings.json                Base configuration (no secrets)
```

## Navigation Domains

The sidebar groups all 9 modules into three sections:

**PORTFOLIO**
- `/properties` — Property list and fund details
- `/shareholders` — Shareholder registry
- `/trustfunds` — Trust fund ownership view

**TRANSACTIONS**
- `/transfers` — Share transfer history
- `/rental` — Rental income records
- `/dividend` — Dividend distributions
- `/expense` — Fund expenses

**SYSTEM**
- `/config` — System configuration
- `/logs` — Audit log viewer

## HttpClient Configuration

Registered in `Program.cs` as a scoped service pointing to the API:

```csharp
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5039") });
```

To change the API address (e.g. for staging), update this value. A future improvement would move it to `appsettings.json`.

## CSS Utilities

`wwwroot/app.css` includes reusable classes for all future pages:

| Class | Purpose |
|-------|---------|
| `reit-table` | Styled data table with dark header and hover rows |
| `badge-active` | Green status pill |
| `badge-pending` | Amber status pill |
| `badge-inactive` | Grey status pill |
| `reit-loading` | Italic grey "loading" paragraph |
| `reit-error` | Red bordered error box |
