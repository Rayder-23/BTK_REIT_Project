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
├── Controllers/     One controller per domain module (12 controllers)
├── Models/          EF Core entities + ReitContext (DB-First)
├── Services/        IAuditService, AuditService, IValidationService, ValidationService
├── Scripts/         SQL test suite and teardown scripts
├── Notes/           Developer notes, install log, audit reports
├── Program.cs       DI registration, middleware pipeline
└── appsettings.json Connection string placeholder (no secrets here)
```

### Domain Model (core relationships)

- **TrustFund** — central entity; owns one **Property**, has many FundDetails, Dividends, Payments, RentalIncomes, Expenses, Transfers
- **Shareholder** — investors with KYC fields (CNIC, NTN, Passport); linked to FundDetails, Dividends, Payments, Transfers, Shbkaccounts
- **FundDetail** — join table: Shareholder ↔ TrustFund (ownership stake with percentage)
- **AdminUser** — authenticates via `/api/auth/login`; approves Payments, Transfers, Expenses; creates Logs and Configurations
- **Log** — full audit trail; records `ActionDetails` and `OldInfo` per event; joined to `AdminUser` for display

### Key Business Rules

- The REIT entity (`sh_id = 1`) must hold a minimum 10% stake in every fund at all times
- Active `FundDetail` percentages for a fund must always sum to 100%
- All multi-step writes use `CreateExecutionStrategy` + `BeginTransactionAsync`
- Every state change, login, and financial action is logged via `IAuditService`

### Services

| Service | Interface | Role |
|---------|-----------|------|
| `AuditService` | `IAuditService` | Fire-and-forget audit log writer. Validates table name against a whitelist of 13 valid tables. Caller owns the transaction and `SaveChangesAsync`. |
| `ValidationService` | `IValidationService` | CNIC / NTN / Passport format validation for shareholder KYC fields. |

### CORS

A `"BlazorClient"` policy allows requests from the Blazor UI (`https://localhost:7235`, `http://localhost:5221`).

## API Endpoints

### Auth — `/api/auth`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/auth/login` | Authenticate admin. Returns `UserSession` (UserId, UserName, SecurityLevel, Token). Audits the login event. |

### Properties — `/api/properties`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/properties/onboard` | Onboard a property and create its trust fund |
| `GET` | `/api/properties/{id}` | Get a single property by ID |
| `GET` | `/api/properties/{id}/summary` | Get property with fund ownership summary |
| `PATCH` | `/api/properties/{id}` | Update property fields |

### Shareholders — `/api/shareholders`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/shareholders` | Register a new shareholder (with KYC validation) |
| `POST` | `/api/shareholders/account` | Add a bank account to an existing shareholder |
| `GET` | `/api/shareholders/{id}` | Get shareholder profile |
| `GET` | `/api/shareholders/{id}/portfolio` | Get shareholder's fund holdings |
| `PATCH` | `/api/shareholders/{id}/status` | Activate or deactivate a shareholder |
| `PATCH` | `/api/shareholders/{id}` | Update shareholder fields |

### Trust Funds — `/api/trustfunds`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/trustfunds/{id}` | Get trust fund record |
| `GET` | `/api/trustfunds/fund/{fundId}/details` | Get all ownership stakes for a fund |
| `PATCH` | `/api/trustfunds/{id}` | Update trust fund fields |

### Fund Details — `/api/funddetails`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/funddetails/{id}` | Get a single fund detail record |

### Transfers — `/api/transfers`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/transfers/initiate` | Initiate a share transfer between shareholders |
| `POST` | `/api/transfers/complete/{id}` | Complete (admin-approve) a pending transfer |
| `GET` | `/api/transfers` | List all transfers |

### Payments — `/api/payment`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/payment` | Record a new shareholder payment |
| `PATCH` | `/api/payment/complete/{id}` | Mark a payment as fully settled |
| `PATCH` | `/api/payment/add-payment/{id}` | Record an additional payment installment |
| `GET` | `/api/payment` | List all payments |

### Rental Income — `/api/rental`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/rental/record` | Record a new rental income entry |
| `PATCH` | `/api/rental/receive-payment/{id}` | Mark rental income as received |
| `POST` | `/api/rental/distribute/{id}` | Distribute received rental income to fund shareholders |
| `GET` | `/api/rental` | List all rental income records |

### Dividends — `/api/dividend`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/dividend/calculate` | Calculate dividends for a period |
| `POST` | `/api/dividend/confirm-payout/{id}` | Confirm and record a dividend payout |
| `GET` | `/api/dividend` | List all dividend records |

### Expenses — `/api/expense`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/expense/record` | Record a new fund expense |
| `PATCH` | `/api/expense/settle/{id}` | Mark an expense as settled |
| `PATCH` | `/api/expense/dispute/{id}` | File a dispute against an expense |
| `GET` | `/api/expense` | List all expense records |

### Config — `/api/config`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/config` | Get all configuration entries (grouped by key) |
| `POST` | `/api/config/set` | Upsert a configuration key |
| `PATCH` | `/api/config/{key}` | Append or remove a value from a config list |

### Logs — `/api/log`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/log/recent` | Get audit logs. Filters: `tableName`, `userId`, `search`, `dateFrom`, `dateTo`, `limit` (default 200, max 1000). Returns entries with joined admin `UserName`. |
| `GET` | `/api/log/admins` | List all admin users (id + name) for filter dropdowns. |

## Testing

An integration test suite lives in `Scripts/REIT_Test_Suite.http` (VS Code REST Client format).  
Run teardown after each test run with `Scripts/teardown_test_run.sql`.

See `Notes/` for developer notes on fixes, Scalar UI setup, and install history.
