using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using REIT_Project.Models;

namespace REIT_Project.Services;

public class ValidationService : IValidationService
{
    // Cache entries live for 5 minutes. Callers can rely on stale values being
    // refreshed within this window after a Configuration row is updated via the
    // Config API.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly ReitContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        ReitContext context,
        IMemoryCache cache,
        ILogger<ValidationService> logger)
    {
        _context = context;
        _cache   = cache;
        _logger  = logger;
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? Allowed)> IsValidAsync(string key, string value)
    {
        string cacheKey = $"config_validation:{key.ToLower().Trim()}";

        // Try the in-process cache first.
        if (!_cache.TryGetValue(cacheKey, out string? csvValue))
        {
            // Cache miss — hit the DB.
            var config = await _context.Configurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Key == key &&
                    c.IsActive);

            if (config == null)
            {
                // Key is missing from the Configurations table — fail safe.
                _logger.LogCritical(
                    "Validation key '{Key}' not found in Configurations table. " +
                    "Request rejected. Add the key to the Configurations table to resolve this.",
                    key);

                throw new InvalidOperationException(
                    $"Configuration key '{key}' is missing from the Configurations table. " +
                    "Contact an administrator.");
            }

            csvValue = config.Value;

            _cache.Set(cacheKey, csvValue, CacheDuration);
        }

        // Split on comma, trim whitespace, compare case-insensitively.
        // This correctly handles values such as "bank-transfer" and "mgmt-fee".
        bool isValid = csvValue!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Any(v => string.Equals(v, value.Trim(), StringComparison.OrdinalIgnoreCase));

        return (isValid, csvValue);
    }
}
