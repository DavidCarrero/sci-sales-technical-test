using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace SciSales.Infrastructure.Persistence;

internal interface ISqlConnectionFactory
{
    DbConnection Create();
}

/// <summary>
/// Hands out closed connections. ADO.NET pools them underneath, so the caller
/// opening and disposing one per operation is the cheap, correct thing to do.
/// </summary>
internal sealed class SqlConnectionFactory(IOptions<DatabaseOptions> options) : ISqlConnectionFactory
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public DbConnection Create() => new SqlConnection(_connectionString);
}
