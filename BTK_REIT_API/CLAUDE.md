# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (HTTP on :5039, HTTPS on :7083)
dotnet run

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef database drop --force
```

No test project exists yet. No linting tools are configured.

## Architecture

ASP.NET Core Web API (.NET 10) for BTK REIT operations. Uses Entity Framework Core with SQL Server (SQLEXPRESS, database name `REIT`).

### Domain Model

All entities and relationships are defined in [Models/ReitContext.cs](Models/ReitContext.cs). The core entities and their relationships:

- **TrustFund** — central entity; owns one **Property** and has many FundDetails, Dividends, Payments, RentalIncomes, Expenses, and Transfers
- **Shareholder** — investors with KYC fields (CNIC, NTN, Passport); linked to FundDetails, Dividends, Payments, Transfers, and Shbkaccounts (bank accounts)
- **AdminUser** — system admins with security levels; approves Payments, Transfers, and Expenses; creates audit Logs and Configurations
- **FundDetail** — join table between Shareholder and TrustFund representing ownership stakes
- **Dividend** — distributions to shareholders with tax/deduction calculations
- **Payment** — shareholder investments into funds with approval workflow
- **Transfer** — share transfers between shareholders (buyer/seller) requiring admin approval
- **RentalIncome** — monthly income per property/fund
- **Expense** — fund costs with approval workflow
- **Log** — audit trail for all record changes (tracks old/new values per field)
- **Configuration** — system-wide key-value settings

### Current State

The project has complete domain models but only a placeholder `/weatherforecast` endpoint in [Program.cs](Program.cs). Controllers, services, repositories, authentication, and authorization are not yet implemented.

### Connection String

Configured in [appsettings.json](appsettings.json) targeting `TAZ\SQLEXPRESS`. For local dev, consider moving credentials to `dotnet user-secrets`.
