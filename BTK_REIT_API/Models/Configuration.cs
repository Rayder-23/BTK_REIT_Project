using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class Configuration
{
    public int ConfigId { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public bool IsActive { get; set; }

    public int? UserId { get; set; }

    public DateTime LastEdited { get; set; }

    public string? Notes { get; set; }

    public virtual AdminUser? User { get; set; }
}
