using SciSales.Domain.Common;

namespace SciSales.Domain.Products;

/// <summary>
/// A catalog product. The constructor is private on purpose: a Product can only
/// exist through <see cref="Create"/> or <see cref="FromStorage"/>, so there is
/// no way to hold one that breaks its own rules.
/// </summary>
public sealed class Product
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private Product(int id, string name, string description, Money price, DateTimeOffset createdDate)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        CreatedDate = createdDate;
    }

    /// <summary>Zero until SQL Server assigns the identity value.</summary>
    public int Id { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Money Price { get; private set; }

    public DateTimeOffset CreatedDate { get; }

    public static Result<Product> Create(
        string? name,
        string? description,
        decimal price,
        DateTimeOffset createdDate)
    {
        var fields = ValidateFields(name, description, price);

        if (fields.IsFailure)
        {
            return Result.Failure<Product>(fields.Error);
        }

        var (validName, validDescription, validPrice) = fields.Value;

        return new Product(id: 0, validName, validDescription, validPrice, createdDate);
    }

    /// <summary>Rebuilds a product that is already stored. Skips no rule the database also enforces.</summary>
    public static Product FromStorage(
        int id,
        string name,
        string description,
        decimal price,
        DateTimeOffset createdDate) =>
        new(id, name, description, Money.FromStorage(price, Currency.Usd), createdDate);

    public Result Update(string? name, string? description, decimal price)
    {
        var fields = ValidateFields(name, description, price);

        if (fields.IsFailure)
        {
            return Result.Failure(fields.Error);
        }

        (Name, Description, Price) = fields.Value;

        return Result.Success();
    }

    /// <summary>Called by the persistence adapter once the identity value is known.</summary>
    public void AssignIdentity(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        if (Id != 0)
        {
            throw new InvalidOperationException($"Product {Id} already has an identity.");
        }

        Id = id;
    }

    private static Result<(string Name, string Description, Money Price)> ValidateFields(
        string? name,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<(string, string, Money)>(ProductErrors.NameEmpty);
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > NameMaxLength)
        {
            return Result.Failure<(string, string, Money)>(ProductErrors.NameTooLong);
        }

        var trimmedDescription = (description ?? string.Empty).Trim();

        if (trimmedDescription.Length > DescriptionMaxLength)
        {
            return Result.Failure<(string, string, Money)>(ProductErrors.DescriptionTooLong);
        }

        var money = Money.Create(price, Currency.Usd);

        if (money.IsFailure)
        {
            return Result.Failure<(string, string, Money)>(money.Error);
        }

        return (trimmedName, trimmedDescription, money.Value);
    }
}
