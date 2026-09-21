using NSubstitute;
using SciSales.Application.Abstractions;
using SciSales.Application.Products;
using SciSales.Domain.Common;
using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Application.Tests.Products;

public sealed class UpdateAndDeleteProductHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Update_persists_the_new_values_and_returns_them()
    {
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Product.FromStorage(1, "Old", "Old description.", 10m, Now));
        _repository.UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var handler = new UpdateProductHandler(_repository);

        var result = await handler.HandleAsync(new UpdateProductCommand(1, "New", "New description.", 20m), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("New");
        result.Value.Price.ShouldBe(20m);
        await _repository.Received(1).UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_reports_not_found_without_writing_anything()
    {
        _repository.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new UpdateProductHandler(_repository);

        var result = await handler.HandleAsync(new UpdateProductCommand(404, "New", "New description.", 20m), Ct);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_rejects_invalid_values_without_writing_anything()
    {
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Product.FromStorage(1, "Old", "Old description.", 10m, Now));

        var handler = new UpdateProductHandler(_repository);

        var result = await handler.HandleAsync(new UpdateProductCommand(1, "New", "New description.", -5m), Ct);

        result.Error.ShouldBe(ProductErrors.PriceNotPositive);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_passes_the_storage_outcome_straight_through()
    {
        _repository.DeleteAsync(7, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await new DeleteProductHandler(_repository).HandleAsync(7, Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_reports_not_found_for_an_id_that_is_not_there()
    {
        _repository.DeleteAsync(404, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ProductErrors.NotFound(404)));

        var result = await new DeleteProductHandler(_repository).HandleAsync(404, Ct);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
