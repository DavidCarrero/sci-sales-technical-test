using NSubstitute;
using SciSales.Application.Abstractions;
using SciSales.Application.Products;
using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Application.Tests.Products;

public sealed class ListProductsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Asks_storage_for_the_right_slice_and_reports_the_page_metadata()
    {
        _repository.CountAsync(Arg.Any<CancellationToken>()).Returns(45);
        _repository.ListAsync(20, 20, Arg.Any<CancellationToken>()).Returns([SomeProduct()]);

        var response = await new ListProductsHandler(_repository).HandleAsync(page: 2, pageSize: 20, Ct);

        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(20);
        response.TotalItems.ShouldBe(45);
        response.TotalPages.ShouldBe(3);
        response.HasNextPage.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task Clamps_a_page_number_below_one(int requested, int expected)
    {
        var response = await new ListProductsHandler(_repository)
            .HandleAsync(page: requested, cancellationToken: Ct);

        response.Page.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5_000, ListProductsHandler.MaxPageSize)]
    public async Task Clamps_the_page_size_to_the_allowed_range(int requested, int expected)
    {
        var response = await new ListProductsHandler(_repository)
            .HandleAsync(pageSize: requested, cancellationToken: Ct);

        response.PageSize.ShouldBe(expected);
        await _repository.Received(1).ListAsync(0, expected, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_no_next_page_on_the_last_one()
    {
        _repository.CountAsync(Arg.Any<CancellationToken>()).Returns(2);
        _repository.ListAsync(0, 20, Arg.Any<CancellationToken>()).Returns([SomeProduct(), SomeProduct()]);

        var response = await new ListProductsHandler(_repository).HandleAsync(cancellationToken: Ct);

        response.TotalPages.ShouldBe(1);
        response.HasNextPage.ShouldBeFalse();
    }

    private static Product SomeProduct() => Product.FromStorage(1, "Monitor", "QHD panel.", 329m, Now);
}
