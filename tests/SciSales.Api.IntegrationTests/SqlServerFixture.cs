using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SciSales.Api.IntegrationTests;

/// <summary>
/// A throwaway SQL Server in a container, with the very same scripts a reviewer
/// would run by hand. Testing stored procedures against an in-memory fake would
/// prove nothing: the procedures are the thing under test.
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Null when the database is up; otherwise why the tests cannot run.</summary>
    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            // Building the container already touches the Docker endpoint, so it
            // belongs inside the try: without a runtime this throws before start.
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

            await _container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception exception)
        {
            // No Docker or Podman on this machine. Saying so beats a wall of red.
            SkipReason = $"No container runtime available for SQL Server: {exception.Message}";
            return;
        }

        var masterConnectionString = _container.GetConnectionString();

        await RunScriptsAsync(masterConnectionString, TestContext.Current.CancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = "SciSalesCatalog",
        }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static async Task RunScriptsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var databaseFolder = Path.Combine(SolutionRoot(), "database");

        var scripts = Directory
            .GetFiles(databaseFolder, "*.sql")
            .OrderBy(path => path, StringComparer.Ordinal);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var script in scripts)
        {
            var content = await File.ReadAllTextAsync(script, cancellationToken);

            foreach (var batch in SplitOnGo(content))
            {
                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 60 };
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    /// <summary>GO is a client-side batch separator, not T-SQL, so it never reaches the server.</summary>
    private static IEnumerable<string> SplitOnGo(string script) => GoSeparator()
        .Split(script)
        .Select(batch => batch.Trim())
        .Where(batch => batch.Length > 0);

    [GeneratedRegex(@"^[ \t]*GO[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();

    private static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SciSales.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("SciSales.sln was not found above the test output folder.");
    }
}

/// <summary>
/// One container for the whole suite. Starting SQL Server per test class would
/// add a minute to every run for no extra confidence.
/// </summary>
[CollectionDefinition(SqlServerTests.Name)]
public sealed class SqlServerTests : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sql-server";
}
