# Bahria Town REIT — Management API

ASP.NET Core Web API (.NET 10) for managing Real Estate Investment Trust operations including property onboarding, shareholder management, rental income, dividend distribution, and expense tracking.

## Tech Stack

- **Framework**: ASP.NET Core 10 Web API
- **ORM**: Entity Framework Core 10 (Code-First)
- **Database**: SQL Server (SQLEXPRESS)
- **Docs**: Scalar / OpenAPI

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server Express (`TAZ\SQLEXPRESS`) with a database named `REIT`

### Setup

1. Clone the repository
2. Configure user secrets (never commit credentials):
   ```bash
   .\setup-secrets.ps1
   ```
3. Apply migrations:
   ```bash
   dotnet ef database update
   ```
4. Run the API:
   ```bash
   dotnet run
   ```

API is available at `http://localhost:5039` — Scalar docs at `/scalar/v1`.

## API Modules

| Module | Base Route | Description |
|--------|-----------|-------------|
| Properties | `/api/Property` | Onboard properties and trust funds |
| Shareholders | `/api/Shareholder` | Register shareholders and bank accounts |
| Trust Funds | `/api/TrustFunds` | View fund details |
| Transfers | `/api/Transfer` | Initiate and complete ownership transfers |
| Payments | `/api/Payment` | Track and settle shareholder payments |
| Rental Income | `/api/Rental` | Record and receive rental payments |
| Dividends | `/api/Dividend` | Calculate and confirm dividend payouts |
| Expenses | `/api/Expense` | Record, settle, and dispute fund expenses |
| Configurations | `/api/Config` | Manage system-wide key/value settings |

## Key Concepts

- All multi-step writes use `CreateExecutionStrategy` + `BeginTransactionAsync` for ACID compliance
- Every state change is logged via `IAuditService` to the `Logs` table
- REIT entity (`sh_id = 1`) must retain a minimum 10% stake in all funds
- Ownership percentages across active `FundDetails` must always sum to 100%
