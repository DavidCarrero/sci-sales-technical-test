using System.Net;
using System.Net.Http.Json;
using SciSales.Application.Products;
using Shouldly;

namespace SciSales.Api.IntegrationTests;

[Collection(SqlServerTests.Name)]
public sealed class ProductsEndpointsTests(SqlServerFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_product_survives_a_full_round_trip_through_the_stored_procedures()
    {
        using var client = CreateClient();
        var name = UniqueName();

        var created = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductCommand(name, "Created by the integration test.", 24.99m),
            Ct);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>(Ct);
        product.ShouldNotBeNull();
        product.Id.ShouldBeGreaterThan(0);
        product.Price.ShouldBe(24.99m);

        var read = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{product.Id}", Ct);
        read.ShouldNotBeNull();
        read.Name.ShouldBe(name);

        var updated = await client.PutAsJsonAsync(
            $"/api/products/{product.Id}",
            new { Name = name + " v2", Description = "Updated.", Price = 31.50m },
            Ct);

        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterUpdate = await updated.Content.ReadFromJsonAsync<ProductResponse>(Ct);
        afterUpdate!.Name.ShouldBe(name + " v2");
        afterUpdate.Price.ShouldBe(31.50m);

        var deleted = await client.DeleteAsync($"/api/products/{product.Id}", Ct);
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var gone = await client.GetAsync($"/api/products/{product.Id}", Ct);
        gone.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_list_endpoint_reports_the_page_it_served()
    {
        using var client = CreateClient();

        var page = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            "/api/products?page=1&pageSize=5", Ct);

        page.ShouldNotBeNull();
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(5);
        page.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task A_second_product_with_the_same_name_is_rejected_with_409()
    {
        using var client = CreateClient();
        var name = UniqueName();
        var command = new CreateProductCommand(name, "First one.", 10m);

        var first = await client.PostAsJsonAsync("/api/products", command, Ct);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/products", command, Ct);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "Blank name.", 10)]
    [InlineData("Valid name", "Zero price.", 0)]
    [InlineData("Valid name", "Negative price.", -1)]
    public async Task Invalid_input_is_rejected_with_400_and_never_reaches_the_database(
        string name,
        string description,
        decimal price)
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductCommand(name, description, price),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Updating_a_product_that_is_not_there_answers_404()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/products/999999",
            new { Name = "Ghost", Description = "Nothing.", Price = 1m },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_price_endpoint_converts_with_the_rate_the_provider_returned()
    {
        using var client = CreateClient();

        var created = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductCommand(UniqueName(), "Priced in another currency.", 100m),
            Ct);

        var product = await created.Content.ReadFromJsonAsync<ProductResponse>(Ct);

        var priced = await client.GetFromJsonAsync<ProductPriceResponse>(
            $"/api/products/{product!.Id}/price?currency=COP", Ct);

        priced.ShouldNotBeNull();
        priced.BaseCurrency.ShouldBe("USD");
        priced.TargetCurrency.ShouldBe("COP");
        priced.Rate.ShouldBe(ProductsApiFactory.StubRate);
        priced.ConvertedPrice.ShouldBe(decimal.Round(100m * ProductsApiFactory.StubRate, 2));
    }

    [Fact]
    public async Task A_currency_that_is_not_three_letters_is_rejected_with_400()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/products/1/price?currency=PESOS", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateClient()
    {
        Assert.SkipWhen(database.SkipReason is not null, database.SkipReason ?? string.Empty);

        var factory = new ProductsApiFactory(database.ConnectionString);

        return factory.CreateClient();
    }

    private static string UniqueName() => $"Test product {Guid.NewGuid():N}"[..40];
}
