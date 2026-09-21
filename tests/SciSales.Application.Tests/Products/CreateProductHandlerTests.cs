using NSubstitute;
using SciSales.Application.Abstractions;
using SciSales.Application.Products;
using SciSales.Domain.Common;
using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Application.Tests.Products;

public sealed class CreateProductHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly FakeTimeProvider _clock = new(Now);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stamps_the_product_with_the_current_time_and_returns_the_new_id()
    {
        _repository.CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(99));

        var handler = new CreateProductHandler(_repository, _clock);

        var result = await handler.HandleAsync(new CreateProductCommand("Mouse", "A mouse.", 24.99m), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(99);
        result.Value.CreatedDate.ShouldBe(Now);
        result.Value.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task Never_touches_storage_when_the_command_is_invalid()
    {
        var handler = new CreateProductHandler(_repository, _clock);

        var result = await handler.HandleAsync(new CreateProductCommand(Name: "", "A mouse.", 24.99m), Ct);

        result.Error.ShouldBe(ProductErrors.NameEmpty);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Surfaces_a_duplicate_name_reported_by_storage()
    {
        _repository.CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<int>(ProductErrors.DuplicateName));

        var handler = new CreateProductHandler(_repository, _clock);

        var result = await handler.HandleAsync(new CreateProductCommand("Mouse", "A mouse.", 24.99m), Ct);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
