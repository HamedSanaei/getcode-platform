using GetCode.Application.Orders;
using GetCode.Application.Quotes;
using GetCode.Application.Identity;
using GetCode.Domain.Authorization;
using GetCode.Domain.Orders;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M06-002 verification: duplicate checkout submits — including racing ones —
/// resolve to exactly one durably-persisted order per (customer, idempotency key).
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class CheckoutIdempotencyTests(DatabaseFixture database)
{
    private static async Task<(GetCodeApiFactory Factory, Guid CustomerId)> NewCustomerAsync(DatabaseFixture db)
    {
        var factory = new GetCodeApiFactory(db);
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityService>();
        var email = $"checkout-{Guid.NewGuid():N}@test.example";
        var result = await identity.RegisterAsync(new RegisterUserCommand(email, "Correct-Horse-9!"), TestContext.Current.CancellationToken);
        var customerId = result.UserId;
        return (factory, customerId);
    }

    private static IssuedQuote IssueQuote(GetCodeApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var quotes = scope.ServiceProvider.GetRequiredService<QuoteService>();
        return quotes.Issue(new IssueQuoteRequest("RU", "telegram", "activation", "fake", 100m, "RUB"));
    }

    [Fact]
    public async Task Sequential_duplicate_submits_return_the_same_order()
    {
        var (factory, customerId) = await NewCustomerAsync(database);
        var quote = IssueQuote(factory);

        using var scopeFactoryScope = factory.Services.CreateScope();
        var checkout1 = scopeFactoryScope.ServiceProvider.GetRequiredService<CheckoutService>();
        var checkout2 = factory.Services.CreateScope().ServiceProvider.GetRequiredService<CheckoutService>();

        var first = await checkout1.CreateOrderAsync(customerId, quote.CustomerView.QuoteId, quote.CustomerView.CustomerAmount, "idem-seq-1", TestContext.Current.CancellationToken);
        var replay = await checkout2.CreateOrderAsync(customerId, quote.CustomerView.QuoteId, quote.CustomerView.CustomerAmount, "idem-seq-1", TestContext.Current.CancellationToken);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Order.Id, replay.Order.Id);
        Assert.Equal(127m, first.Order.Amount); // commercial snapshot from the quote
        Assert.Equal(1, first.Order.PricingRuleVersion);
        Assert.Equal(OrderPaymentState.AwaitingPayment, first.Order.PaymentState);
    }

    [Fact]
    public async Task Racing_duplicate_submits_create_exactly_one_order()
    {
        var (factory, customerId) = await NewCustomerAsync(database);
        var quote = IssueQuote(factory);
        var key = $"idem-race-{Guid.NewGuid():N}";

        // Fresh scopes simulate two independent HTTP requests hitting the server.
        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var checkoutA = scopeA.ServiceProvider.GetRequiredService<CheckoutService>();
        var checkoutB = scopeB.ServiceProvider.GetRequiredService<CheckoutService>();

        var results = await Task.WhenAll(
            checkoutA.CreateOrderAsync(customerId, quote.CustomerView.QuoteId, quote.CustomerView.CustomerAmount, key, TestContext.Current.CancellationToken),
            checkoutB.CreateOrderAsync(customerId, quote.CustomerView.QuoteId, quote.CustomerView.CustomerAmount, key, TestContext.Current.CancellationToken));

        Assert.Equal(results[0].Order.Id, results[1].Order.Id);      // same order for both requests
        Assert.Contains(results, r => !r.Replayed);                  // exactly one creator
        Assert.Single(results, r => r.Replayed);

        // And the durable state agrees: a third lookup sees that one order.
        using var verifyScope = factory.Services.CreateScope();
        var repo = verifyScope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var persisted = await repo.FindByIdempotencyKeyAsync(customerId, key, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(results[0].Order.Id, persisted!.Id);
    }
}
