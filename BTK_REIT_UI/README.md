# BTK_REIT_UI

Blazor Server (.NET 10) dashboard frontend for the BTK REIT Management System.

## Tech Stack

- **Framework**: Blazor Server (.NET 10)
- **Render mode**: `InteractiveServer` (server-side SignalR connection)
- **Styling**: Bootstrap 5 + custom REIT dashboard CSS (`wwwroot/app.css`)
- **Font**: Inter (Google Fonts) via `app.css`
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
│   │   ├── NavMenu.razor           Sidebar navigation (4 sections, 13 links)
│   │   └── NavMenu.razor.css       Scoped sidebar styles
│   ├── Pages/
│   │   ├── Home.razor              Landing / login page  (/)
│   │   ├── Properties.razor        Property list  (/properties)
│   │   ├── OnboardProperty.razor   Onboard a new property  (/onboard-property)
│   │   ├── PropertyDetail.razor    Property + fund detail  (/property/{id})
│   │   ├── Shareholders.razor      Shareholder registry  (/shareholders)
│   │   ├── AddShareholder.razor    Register shareholder  (/add-shareholder)
│   │   ├── ShareholderDetail.razor Shareholder profile + portfolio  (/shareholder/{id})
│   │   ├── TrustFunds.razor        Trust fund list  (/trustfunds)
│   │   ├── TrustFundDetail.razor   Fund stakes + details  (/trustfund/{id})
│   │   ├── Transfers.razor         Share transfer history  (/transfers)
│   │   ├── RentalIncome.razor      Rental income records  (/rental)
│   │   ├── Dividends.razor         Dividend distributions  (/dividend)
│   │   ├── Configurations.razor    System config editor  (/configurations)
│   │   ├── AuditLogs.razor         Audit trail viewer  (/logs)  [root admin only]
│   │   ├── NotFound.razor          404 handler
│   │   └── Error.razor             Unhandled error page
│   ├── App.razor                   Root HTML document
│   ├── Routes.razor                Router + layout assignment
│   └── _Imports.razor              Global @using directives
├── wwwroot/
│   ├── app.css                     Global styles, REIT layout, table/badge utilities
│   └── favicon.png
├── Program.cs                      DI registration, middleware pipeline
└── appsettings.json                Base configuration (no secrets)
```

## Navigation Map

The sidebar organises all pages into four sections:

**PORTFOLIO**
| Page | Route | Description |
|------|-------|-------------|
| Properties | `/properties` | Property list; links to detail and onboarding |
| Shareholders | `/shareholders` | Shareholder registry; links to detail and add |
| Trust Funds | `/trustfunds` | Fund list; links to fund detail pages |

**OPERATIONS**
| Page | Route | Description |
|------|-------|-------------|
| Onboard Property | `/onboard-property` | Multi-step form to register a property + trust fund |
| Add Shareholder | `/add-shareholder` | Register a new shareholder with KYC fields |

**TRANSACTIONS**
| Page | Route | Description |
|------|-------|-------------|
| Transfers | `/transfers` | Share transfer history |
| Rental Income | `/rental` | Rental income records; receive and distribute payments |
| Dividends | `/dividend` | Dividend calculation and payout confirmation |
| Expenses | `/expense` | Fund expenses, settlements, and disputes |

**SYSTEM**
| Page | Route | Description |
|------|-------|-------------|
| Config | `/configurations` | System-wide key/value config editor |
| Audit Logs | `/logs` | Full audit trail with filtering; **root admin only** (SecurityLevel = 1) |

## Authentication & Access Control

The login page (`/`) accepts admin credentials via `POST /api/auth/login`. On success a `UserSession` is stored in `AuthService` (scoped singleton) and includes `UserId`, `UserName`, `SecurityLevel`, and `Token`.

Pages that require authentication check `AuthService.CurrentSession` in `OnAfterRenderAsync`. The Audit Logs page additionally enforces `SecurityLevel == 1` (root admin); lower-level admins see an access-denied screen.

## Audit Logs Page (`/logs`)

- **Master ledger table**: high-density `table-sm` view with Timestamp, Admin, Action (color-coded badge), Entity, Record ID, Description
- **Color-coded action badges**: Create (green), Update (blue), Delete (red), Login (cyan), Transfer (orange), Dividend (orange)
- **Filter bar**: Admin dropdown, Entity/Table dropdown, date range pickers, description search (Enter to apply), result limit selector
- **Expandable rows**: click any row to reveal the full action description, OldInfo diff (Previous State vs After Change), notes, and metadata

## HttpClient Configuration

Registered in `Program.cs` as a scoped service pointing to the API:

```csharp
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5039") });
```

To change the API address (e.g. for staging), update this value.

## CSS Utilities

`wwwroot/app.css` includes reusable classes used across all pages:

| Class | Purpose |
|-------|---------|
| `reit-table` | Styled data table with dark header and hover rows |
| `badge-active` | Green status pill |
| `badge-pending` | Amber status pill |
| `badge-inactive` | Grey status pill |
| `badge-completed` | Teal status pill |
| `badge-disputed` | Red status pill |
| `reit-loading` | Italic grey "loading" paragraph |
| `reit-error` | Red bordered error box |
| `audit-badge--insert` | Green action badge (Create) |
| `audit-badge--update` | Blue action badge (Update) |
| `audit-badge--delete` | Red action badge (Delete) |
| `audit-badge--login` | Cyan action badge (Login) |
| `audit-badge--transfer` | Orange action badge (Transfer/Dividend) |
