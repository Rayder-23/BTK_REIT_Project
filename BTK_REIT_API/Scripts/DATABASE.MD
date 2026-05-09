# Database Setup

## Prerequisites

- .NET 10 SDK
- SQL Server Express with a database named `REIT`
- `dotnet-ef` global tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Connection String

The connection string is stored in **user secrets** and never committed to source control. Run once after cloning:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

A typical SQL Server Express connection string looks like:

```
Server=localhost\SQLEXPRESS;Database=REIT;Trusted_Connection=True;TrustServerCertificate=True;
```

To verify your secrets are set correctly:

```bash
dotnet user-secrets list
```

## Schema

The raw SQL schema is available at [`REIT_Project\BTK_REIT_API\Scripts/REIT_Schema.sql`](./REITdb_schema.sql). It can be run directly in SQL Server Management Studio (SSMS) against an empty `REIT` database as an alternative to EF migrations.

## Migrations

```bash
# Apply all pending migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Drop and rebuild (dev only)
dotnet ef database drop --force && dotnet ef database update
```

## Seed Data

A development seed script is available at [`REIT_Project\BTK_REIT_API\Scripts/REIT_seed.sql`](./REITdb_seed.sql).
It populates all 13 tables with a realistic scenario:

- 3 shareholders (REIT fund, Ahmed Khan, Sara Mahmood)
- 1 property (Clifton Tower, Karachi) with a corresponding fund
- Initial ownership split — REIT 10%, Ahmed 55%, Sara 35%
- A completed transfer — Sara sells 15% to Ahmed (post-transfer: Ahmed 70%, Sara 20%)
- April 2026 rental income, dividend payouts, a payment record, and a maintenance expense
- 2 admin users, bank accounts, audit logs, and system configurations

Run after applying the schema:

```bash
# In SSMS: open REIT_seed.sql, confirm REIT database is selected, press F5
```

> **Note:** This is development data only. Never run the seed script against a production database.