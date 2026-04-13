using REIT_Project.Models;

namespace REIT_Project.Services;

public class AuditService : IAuditService
{
    private readonly ReitContext _context;

    // Exact table names as defined in the database schema (13 tables).
    private static readonly HashSet<string> ValidTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AdminUsers",
        "Configurations",
        "Dividend",
        "Expenses",
        "FundDetails",
        "Logs",
        "Payments",
        "Property",
        "RentalIncome",
        "Shareholder",
        "SHBKAccounts",
        "Transfers",
        "TrustFund"
    };

    public AuditService(ReitContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public void LogAction(
        int userId,
        string tableName,
        int recordId,
        string action,
        string? oldInfo = null,
        string? notes = null)
    {
        if (!ValidTableNames.Contains(tableName))
            throw new ArgumentException(
                $"'{tableName}' is not a recognised table name. Audit aborted to prevent constraint violation.",
                nameof(tableName));

        var log = new Log
        {
            UserId = userId,
            TableName = tableName,
            RecordId = recordId,
            ActionDetails = action,
            OldInfo = oldInfo,
            Notes = notes,
            CreationDate = DateTime.UtcNow
        };

        _context.Logs.Add(log);
        // Intentionally NO SaveChangesAsync — the caller owns the transaction.
    }
}
