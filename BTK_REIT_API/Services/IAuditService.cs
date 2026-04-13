namespace REIT_Project.Services;

public interface IAuditService
{
    /// <summary>
    /// Stages a log entry on the DbContext. Does NOT call SaveChangesAsync —
    /// the caller's existing SaveChangesAsync will persist it atomically.
    /// </summary>
    void LogAction(
        int userId,
        string tableName,
        int recordId,
        string action,
        string? oldInfo = null,
        string? notes = null);
}
