# BTK_REIT_Shared

.NET 10 class library containing all DTOs (Data Transfer Objects) shared between the API and the UI.

## Purpose

Centralising DTOs here means the API and UI always agree on the shape of requests and responses. A breaking change to a DTO surfaces as a **compile error** in both projects, not a silent runtime mismatch.

## Usage

Both `BTK_REIT_API` and `BTK_REIT_UI` reference this project directly:

```xml
<ProjectReference Include="..\BTK_REIT_Shared\BTK_REIT_Shared.csproj" />
```

Import the namespace where needed:

```csharp
using BTK_REIT_Shared.DTOs;
```

## DTO Inventory

### Read Models (API responses)

| File | Type | Description |
|------|------|-------------|
| `ShareholderDto` | Read | Shareholder profile with KYC fields and status |
| `ShareholderPortfolioItemDto` | Read | Single fund entry in a shareholder's portfolio |
| `PropertyDto` | Read | Property record |
| `PropertyDetailDto` | Read | Property with ownership summary |
| `TrustFundDto` | Read | Trust fund record |
| `FundDetailDto` | Read | Ownership stake (shareholder ↔ fund) |
| `FundSummaryDto` | Read | Aggregated fund health view |
| `TransferDto` | Read | Share transfer record |
| `PaymentDto` | Read | Shareholder investment/payment record |
| `RentalIncomeDto` | Read | Rental income record |
| `DividendDto` | Read | Dividend distribution record |
| `ExpenseDto` | Read | Fund expense record |
| `ShbkaccountDto` | Read | Shareholder bank account |
| `ConfigurationDto` | Read | System configuration entry |
| `LogDto` | Read | Audit log entry |
| `AdminUserDto` | Read | Admin user record |

### Write Models (API request bodies)

| File | Type | Description |
|------|------|-------------|
| `ShareholderCreateDto` | Write | Register a new shareholder |
| `BankAccountCreateDto` | Write | Add a bank account to a shareholder |
| `UpdateShareholderStatusDto` | Write | Activate / deactivate a shareholder |
| `OnboardPropertyDto` | Write | Onboard a property and create its trust fund |
| `TransferInitiateDto` | Write | Start a share transfer |
| `PaymentCompleteDto` | Write | Complete a shareholder payment |
| `PaymentUpdateDto` | Write | Record an additional payment installment |
| `RentRecordDto` | Write | Record a rental income entry |
| `RentPaymentDto` | Write | Mark rental income as received |
| `DistributionDto` | Write | Distribute rental income |
| `DividendCalculateDto` | Write | Trigger dividend calculation for a period |
| `DividendPayoutDto` | Write | Confirm dividend payout |
| `CreateExpenseDto` | Write | Record a new fund expense |
| `SettleExpenseDto` | Write | Mark an expense as settled |
| `DisputeExpenseDto` | Write | File a dispute against an expense |
| `ConfigSetDto` | Write | Upsert a configuration key |
| `ConfigPatchDto` | Write | Append or remove a value from a config list |

## Namespace

All DTOs use the file-scoped namespace:

```csharp
namespace BTK_REIT_Shared.DTOs;
```
