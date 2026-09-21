using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Infrastructure.Persistence;

/// <summary>
/// The SQL Server adapter. Every call is a stored procedure invoked with typed
/// parameters: no string concatenation, no ad-hoc SQL, nothing for an injection
/// to attach itself to.
/// </summary>
internal sealed class ProductRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options,
    ILogger<ProductRepository> logger) : IProductRepository
{
    private const int StatusSuccess = 0;
    private const int StatusConflict = 1;
    private const int StatusNotFound = 2;
    private const string ReturnValue = "@ReturnValue";

    private readonly int _commandTimeout = options.Value.CommandTimeoutSeconds;

    public async Task<Result<int>> CreateAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var parameters = new DynamicParameters();
        parameters.Add("@Name", product.Name, DbType.String, size: Product.NameMaxLength);
        parameters.Add("@Description", product.Description, DbType.String, size: Product.DescriptionMaxLength);
        parameters.Add("@Price", product.Price.Amount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@CreatedDate", product.CreatedDate.UtcDateTime, DbType.DateTime2);
        parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add(ReturnValue, dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(Command("products.PRC_Create", parameters, cancellationToken));

        var status = parameters.Get<int>(ReturnValue);

        if (status == StatusConflict)
        {
            InfrastructureLog.DuplicateProductName(logger, product.Name);
            return Result.Failure<int>(ProductErrors.DuplicateName);
        }

        return parameters.Get<int>("@Id");
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        await using var connection = connectionFactory.Create();

        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            Command("products.PRC_GetById", parameters, cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Product>> ListAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Skip", skip, DbType.Int32);
        parameters.Add("@Take", take, DbType.Int32);

        await using var connection = connectionFactory.Create();

        var rows = await connection.QueryAsync<ProductRow>(
            Command("products.PRC_GetAll", parameters, cancellationToken));

        return [.. rows.Select(row => row.ToDomain())];
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();

        var total = await connection.ExecuteScalarAsync<long>(
            Command("products.PRC_Count", parameters: null, cancellationToken));

        return (int)total;
    }

    public async Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", product.Id, DbType.Int32);
        parameters.Add("@Name", product.Name, DbType.String, size: Product.NameMaxLength);
        parameters.Add("@Description", product.Description, DbType.String, size: Product.DescriptionMaxLength);
        parameters.Add("@Price", product.Price.Amount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add(ReturnValue, dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(Command("products.PRC_Update", parameters, cancellationToken));

        return parameters.Get<int>(ReturnValue) switch
        {
            StatusSuccess => Result.Success(),
            StatusConflict => Result.Failure(ProductErrors.DuplicateName),
            StatusNotFound => Result.Failure(ProductErrors.NotFound(product.Id)),
            var unexpected => throw new InvalidOperationException(
                $"products.PRC_Update returned an unknown status: {unexpected}."),
        };
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);
        parameters.Add(ReturnValue, dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(Command("products.PRC_Delete", parameters, cancellationToken));

        return parameters.Get<int>(ReturnValue) == StatusNotFound
            ? Result.Failure(ProductErrors.NotFound(id))
            : Result.Success();
    }

    private CommandDefinition Command(string procedure, DynamicParameters? parameters, CancellationToken cancellationToken) =>
        new(
            procedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            commandTimeout: _commandTimeout,
            cancellationToken: cancellationToken);
}
