# BTK_REIT_API

ASP.NET Core 10 Web API — the backend engine for the BTK REIT Management System.

## Tech Stack

- **Framework**: ASP.NET Core 10 Web API
- **ORM**: Entity Framework Core 10 (DB-First scaffold)
- **Database**: SQL Server Express (`TAZ\SQLEXPRESS`, database `REIT`)
- **API Docs**: Scalar / OpenAPI (`/scalar/v1`)
- **Shared contracts**: `BTK_REIT_Shared` (project reference)

## Local Setup

### Prerequisites

- .NET 10 SDK
- SQL Server Express with a database named `REIT`
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

### Connection String

The connection string is stored in **user secrets** and never committed. Run once after cloning:

```powershell
.\setup-secrets.ps1
```

To inspect or update manually:

```bash
dotnet user-secrets list
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

### Database

```bash
# Apply all pending migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Drop and rebuild (dev only)
dotnet ef database drop --force && dotnet ef database update
```

### Run

```bash
dotnet run
# HTTP  → http://localhost:5039
# HTTPS → https://localhost:7083
# Scalar docs → http://localhost:5039/scalar/v1
```

## Architecture

### Project Layout

```
BTK_REIT_API/
├── Controllers/     One controller per domain module
├── Models/          EF Core entities + ReitContext (DB-First)
├── Services/        IAuditService, IValidationService
├── Scripts/         SQL test suite and teardown scripts
├── Notes/           Developer notes (install log, fix notes)
├── Program.cs       DI registration, middleware pipeline
└── appsettings.json Connection string placeholder (no secrets here)
```

### Domain Model (core relationships)

- **TrustFund** — central entity; owns one **Property**, has many FundDetails, Dividends, Payments, RentalIncomes, Expenses, Transfers
- **Shareholder** — investors with KYC; linked to FundDetails, Dividends, Payments, Transfers, Shbkaccounts
- **FundDetail** — join table: Shareholder ↔ TrustFund (ownership stake with percentage)
- **AdminUser** — approves Payments, Transfers, Expenses; creates Logs and Configurations
- **Log** — full audit trail; records old/new values per field for every state change

### Key Business Rules

- The REIT entity (`sh_id = 1`) must hold a minimum 10% stake in every fund at all times
- Active `FundDetail` percentages for a fund must always sum to 100%
- All multi-step writes use `CreateExecutionStrategy` + `BeginTransactionAsync`
- Every state change is logged via `IAuditService`

### CORS

A `"BlazorClient"` policy allows requests from the Blazor UI (`https://localhost:7235`, `http://localhost:5221`).

## API Modules

| Module | Base Route | Key Endpoints |
|--------|-----------|---------------|
| Properties | `/api/properties` | `POST /onboard`, `GET /{id}`, `GET /{id}/summary` |
| Shareholders | `/api/shareholders` | `POST /`, `POST /account`, `GET /{id}`, `GET /{id}/portfolio` |
| Trust Funds | `/api/trustfunds` | `GET /{id}`, `GET /fund/{fundId}/details` |
| Transfers | `/api/transfers` | `POST /initiate`, `POST /complete/{id}` |
| Payments | `/api/payment` | `POST /`, `PATCH /complete/{id}`, `PATCH /add-payment/{id}` |
| Rental | `/api/rental` | `POST /record`, `PATCH /receive-payment/{id}` |
| Dividend | `/api/dividend` | `POST /calculate`, `POST /confirm-payout/{id}` |
| Expense | `/api/expense` | `POST /record`, `PATCH /settle/{id}`, `PATCH /dispute/{id}` |
| Config | `/api/config` | `GET /`, `POST /set`, `PATCH /{key}` |
| Logs | `/api/log` | `GET /`, `GET /table/{tableName}`, `GET /record/{tableName}/{id}` |

## Testing

An integration test suite lives in `Scripts/REIT_Test_Suite.http` (VS Code REST Client format).  
Run teardown after each test run with `Scripts/teardown_test_run.sql`.

See `Notes/` for developer notes on fixes, Scalar UI setup, and install history.
