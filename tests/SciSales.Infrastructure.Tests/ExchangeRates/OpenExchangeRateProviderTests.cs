using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SciSales.Domain.Products;
using SciSales.Infrastructure.ExchangeRates;
using Shouldly;

namespace SciSales.Infrastructure.Tests.ExchangeRates;

/// <summary>
/// The provider is the piece most likely to meet a bad day: a third party that
/// answers slowly, answers wrong, or does not answer. Each of those has a test.
/// </summary>
public sealed class OpenExchangeRateProviderTests
{
    private static readonly Currency Cop = Currency.Create("COP").Value;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reads_the_rate_and_the_date_the_provider_published_it()
    {
        var provider = Given("""
            {
              "result": "success",
              "base_code": "USD",
              "time_last_update_unix": 1758412800,
              "rates": { "COP": 3950.25, "EUR": 0.92 }
            }
            """);

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rate.ShouldBe(3950.25m);
        result.Value.From.ShouldBe(Currency.Usd);
        result.Value.To.ShouldBe(Cop);
        result.Value.AsOf.ShouldBe(DateTimeOffset.FromUnixTimeSeconds(1758412800));
    }

    [Fact]
    public async Task Asks_the_provider_for_the_base_currency_of_the_catalog()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            { "result": "success", "time_last_update_unix": 1, "rates": { "COP": 1 } }
            """);

        await Given(handler).GetRateAsync(Currency.Usd, Cop, Ct);

        handler.LastPath.ShouldBe("/v6/latest/USD");
    }

    [Fact]
    public async Task Reports_the_currency_as_unsupported_when_the_provider_does_not_publish_it()
    {
        var provider = Given("""
            { "result": "success", "time_last_update_unix": 1, "rates": { "EUR": 0.92 } }
            """);

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unsupported(Cop));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rejects_a_rate_that_is_not_positive(decimal rate)
    {
        var provider = Given($$"""
            { "result": "success", "time_last_update_unix": 1, "rates": { "COP": {{rate}} } }
            """);

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unsupported(Cop));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Reports_the_provider_as_unavailable_on_an_error_status(HttpStatusCode status)
    {
        var provider = Given(new StubHandler(status, "{}"));

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unavailable);
    }

    [Fact]
    public async Task Reports_the_provider_as_unavailable_when_it_answers_with_an_error_result()
    {
        // The API answers 200 with result "error" when the base code is unknown.
        var provider = Given("""
            { "result": "error", "error-type": "unsupported-code" }
            """);

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unavailable);
    }

    [Fact]
    public async Task Reports_the_provider_as_unavailable_when_the_network_fails()
    {
        var provider = Given(new StubHandler(new HttpRequestException("name resolution failed")));

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unavailable);
        result.Error.Type.ShouldBe(Domain.Common.ErrorType.Unavailable);
    }

    [Fact]
    public async Task Reports_the_provider_as_unavailable_when_the_request_times_out()
    {
        var provider = Given(new StubHandler(new TaskCanceledException("the request timed out")));

        var result = await provider.GetRateAsync(Currency.Usd, Cop, Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unavailable);
    }

    [Fact]
    public async Task Lets_a_cancellation_asked_for_by_the_caller_through()
    {
        var provider = Given(new StubHandler(new TaskCanceledException("cancelled")));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<TaskCanceledException>(
            () => provider.GetRateAsync(Currency.Usd, Cop, cancelled.Token));
    }

    private static OpenExchangeRateProvider Given(string json) =>
        Given(new StubHandler(HttpStatusCode.OK, json));

    private static OpenExchangeRateProvider Given(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://open.er-api.com/") },
            NullLogger<OpenExchangeRateProvider>.Instance);

    /// <summary>Answers whatever the test asked for, and remembers what was requested.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string? _body;
        private readonly Exception? _failure;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public StubHandler(Exception failure) => _failure = failure;

        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.AbsolutePath;

            cancellationToken.ThrowIfCancellationRequested();

            if (_failure is not null)
            {
                return Task.FromException<HttpResponseMessage>(_failure);
            }

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body!, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
