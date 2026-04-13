# BTK REIT Management System

Full-stack management platform for the BTK REIT. Three-project .NET 10 solution covering API, shared contracts, and a Blazor Server UI.

## Solution Structure

```
BTK_REIT/
├── BTK_REIT_API/        ASP.NET Core 10 Web API — business logic, EF Core, SQL Server
├── BTK_REIT_Shared/     Class library — DTOs shared between API and UI
├── BTK_REIT_UI/         Blazor Server 10 — dashboard frontend
└── BTK_REIT.slnx        Solution file
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 10, Entity Framework Core 10 |
| Database | SQL Server Express (`TAZ\SQLEXPRESS`, database `REIT`) |
| API Docs | Scalar / OpenAPI (`/scalar/v1`) |
| Frontend | Blazor Server (.NET 10) |
| Shared | .NET 10 Class Library |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server Express with a database named `REIT`
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## First-Time Setup

```bash
# 1. Clone
git clone <repo-url>
cd BTK_REIT_Project

# 2. Set the connection string (stored in user secrets, never committed)
cd BTK_REIT_API
.\setup-secrets.ps1

# 3. Apply EF Core migrations
dotnet ef database update
```

## Running the Solution

Open two terminals from the solution root:

```bash
# Terminal 1 — API (http://localhost:5039)
cd BTK_REIT_API
dotnet run

# Terminal 2 — UI (https://localhost:7235)
cd BTK_REIT_UI
dotnet run
```

Navigate to `https://localhost:7235` in your browser.  
API docs (Scalar) are at `http://localhost:5039/scalar/v1`.

## Domain Modules

| Module | API Route | Description |
|--------|-----------|-------------|
| Properties | `/api/properties` | Onboard properties and trust funds |
| Shareholders | `/api/shareholders` | Register shareholders and bank accounts |
| Trust Funds | `/api/trustfunds` | View fund details and ownership |
| Transfers | `/api/transfers` | Initiate and complete share transfers |
| Payments | `/api/payment` | Track and settle shareholder investments |
| Rental Income | `/api/rental` | Record and receive rental payments |
| Dividends | `/api/dividend` | Calculate and confirm dividend payouts |
| Expenses | `/api/expense` | Record, settle, and dispute fund expenses |
| Config | `/api/config` | System-wide key/value settings |
| Logs | `/api/log` | Audit trail |

## Project READMEs

- [BTK_REIT_API/README.md](BTK_REIT_API/README.md) — API setup, architecture, business rules
- [BTK_REIT_Shared/README.md](BTK_REIT_Shared/README.md) — Shared DTO library
- [BTK_REIT_UI/README.md](BTK_REIT_UI/README.md) — Blazor UI setup and pages
