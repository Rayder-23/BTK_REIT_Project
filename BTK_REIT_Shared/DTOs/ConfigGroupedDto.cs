namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Represents all configuration rows for a single key, aggregated for the
/// Configurations management UI. All values are joined into a single CSV string.
/// </summary>
public class ConfigGroupedDto
{
    /// <summary>The config_id of the representative row (first active row for the key).</summary>
    public int ConfigId { get; set; }

    /// <summary>The configuration key (e.g. "banks", "transfer_type", "status_sh").</summary>
    public string Key { get; set; } = null!;

    /// <summary>All values for this key joined as a comma-separated string.</summary>
    public string Value { get; set; } = null!;

    /// <summary>Individual parsed values (split from Value).</summary>
    public List<string> Values { get; set; } = new();

    /// <summary>Whether this configuration row is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional admin notes for this key.</summary>
    public string? Notes { get; set; }

    /// <summary>Last edited timestamp.</summary>
    public DateTime LastEdited { get; set; }

    /// <summary>UserId of the admin who last edited.</summary>
    public int? UserId { get; set; }
}
