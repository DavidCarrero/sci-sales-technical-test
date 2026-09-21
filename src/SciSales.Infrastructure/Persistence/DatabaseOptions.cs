using System.ComponentModel.DataAnnotations;

namespace SciSales.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// SQL Server connection string. It is read from configuration, never
    /// hardcoded: user secrets in development, environment variables in Docker.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Seconds a stored procedure may run before the client gives up.</summary>
    [Range(1, 120)]
    public int CommandTimeoutSeconds { get; init; } = 15;
}
