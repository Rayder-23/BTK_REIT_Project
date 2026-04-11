namespace REIT_Project.Services;

public interface IValidationService
{
    /// <summary>
    /// Checks whether <paramref name="value"/> exists in the comma-separated
    /// configuration string stored under <paramref name="key"/> in the
    /// Configurations table. Look-up and comparison are case-insensitive.
    /// </summary>
    /// <returns>
    /// A tuple of:
    ///   <c>IsValid</c>  — true when the value was found.
    ///   <c>Allowed</c>  — the raw CSV string from the DB (e.g. "sale, gift, inheritance"),
    ///                     or null when the key was not found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration key does not exist in the DB (fail-safe).
    /// </exception>
    Task<(bool IsValid, string? Allowed)> IsValidAsync(string key, string value);
}
