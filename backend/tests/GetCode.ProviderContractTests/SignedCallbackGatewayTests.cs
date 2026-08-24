using GetCode.Application.Payments;
using GetCode.Domain.Orders;
using GetCode.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace GetCode.ProviderContractTests;

/// <summary>
/// M06-004 verification: signature authenticity, amount/order integrity,
/// duplicate idempotency and replay/forgery rejection for the signed-callback
/// gateway, wired through the same normalized handling service production uses.
/// </summary>
public sealed class SignedCallbackGatewayTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class InMemoryOrderStore : GetCode.Application.Orders.IOrderRepository
    {
        public readonly Dictionary<Guid, Order> Orders = new();

        public Task<Order?> FindByIdempotencyKeyAsync(Guid customerId, string key, CancellationToken ct) =>
            Task.FromResult(Orders.Values.FirstOrDefault(o => o.CustomerId == customerId && o.IdempotencyKey == key));

        public Task AddAsync(Order order, CancellationToken ct)
        {
            Orders[order.Id] = order;
            return Task.CompletedTask;
        }

        public Task<Order?> FindByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Orders.TryGetValue(id, out var order) ? order : null);

        public Task UpdateAsync(Order order, CancellationToken ct) => Task.CompletedTask;
    }

    private static (SignedRedirectGateway Gateway, InMemoryOrderStore Store, Order Order) CreateWorld()
    {
        var gateway = new SignedRedirectGateway(Options.Create(new SignedRedirectGatewayOptions
        {
            Enabled = true,
            CallbackSecret = "test-only-secret-not-a-production-value",
        }));
        var store = new InMemoryOrderStore();
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "idem-cb", 127m, "RUB",
            "RU", "telegram", "activation", 1, T0);
        store.Orders[order.Id] = order;
        return (gateway, store, order);
    }

    private static async Task<string> CreateIntentReferenceAsync(SignedRedirectGateway gateway, Order order)
    {
        var intent = await gateway.CreateIntentAsync(
            new PaymentIntentRequest(order.Id, order.Amount, order.Currency, order.IdempotencyKey, null),
            TestContext.Current.CancellationToken);
        return intent.GatewayReference!;
    }

    private static PaymentCallbackService NewService(SignedRedirectGateway gateway, InMemoryOrderStore store) =>
        new(gateway, store);

    [Fact]
    public async Task Valid_signature_with_matching_terms_applies_payment_once()
    {
        var (gateway, store, order) = CreateWorld();
        var reference = await CreateIntentReferenceAsync(gateway, order);
        var signature = SignedRedirectGateway.Sign(
            System.Text.Encoding.UTF8.GetBytes("test-only-secret-not-a-production-value"), reference, 127m, "RUB");
        var service = NewService(gateway, store);

        var result = await service.HandleCallbackAsync(reference, 127m, "RUB", signature, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Applied, result.Outcome);
        Assert.Equal(OrderPaymentState.Paid, order.PaymentState);
    }

    [Fact]
    public async Task Forged_signature_is_rejected_and_order_stays_unpaid()
    {
        var (gateway, store, order) = CreateWorld();
        var reference = await CreateIntentReferenceAsync(gateway, order);
        var service = NewService(gateway, store);

        var forged = await service.HandleCallbackAsync(reference, 127m, "RUB", "deadbeef-deadbeef", TestContext.Current.CancellationToken);
        var missing = await service.HandleCallbackAsync(reference, 127m, "RUB", null, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Rejected, forged.Outcome);
        Assert.Equal("invalid-signature", forged.ReasonToken);
        Assert.Equal(PaymentCallbackService.Handling.Rejected, missing.Outcome);
        Assert.Equal(OrderPaymentState.AwaitingPayment, order.PaymentState);
    }

    [Fact]
    public async Task Amount_mismatch_is_rejected_even_with_valid_signature_over_other_terms()
    {
        var (gateway, store, order) = CreateWorld();
        var reference = await CreateIntentReferenceAsync(gateway, order);
        // Signature computed over the TAMPERED terms cannot match the intent's.
        var tamperedSignature = SignedRedirectGateway.Sign(
            System.Text.Encoding.UTF8.GetBytes("test-only-secret-not-a-production-value"), reference, 1m, "RUB");
        var service = NewService(gateway, store);

        var result = await service.HandleCallbackAsync(reference, 1m, "RUB", tamperedSignature, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Rejected, result.Outcome);
        Assert.Equal("amount-mismatch", result.ReasonToken); // commercial integrity checked before signature
        Assert.Equal(OrderPaymentState.AwaitingPayment, order.PaymentState);
    }

    [Fact]
    public async Task Unknown_reference_is_rejected_without_order_lookup()
    {
        var (gateway, store, order) = CreateWorld();
        var service = NewService(gateway, store);

        var result = await service.HandleCallbackAsync("sg-does-not-exist", 127m, "RUB", "ff", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Rejected, result.Outcome);
        Assert.Equal("unknown-reference", result.ReasonToken);
        Assert.Equal(OrderPaymentState.AwaitingPayment, order.PaymentState);
    }

    [Fact]
    public async Task Duplicate_callback_is_idempotent_never_double_applies()
    {
        var (gateway, store, order) = CreateWorld();
        var reference = await CreateIntentReferenceAsync(gateway, order);
        var signature = SignedRedirectGateway.Sign(
            System.Text.Encoding.UTF8.GetBytes("test-only-secret-not-a-production-value"), reference, 127m, "RUB");
        var service = NewService(gateway, store);

        var first = await service.HandleCallbackAsync(reference, 127m, "RUB", signature, TestContext.Current.CancellationToken);
        var second = await service.HandleCallbackAsync(reference, 127m, "RUB", signature, TestContext.Current.CancellationToken);
        var third = await service.HandleCallbackAsync(reference, 127m, "RUB", signature, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Applied, first.Outcome);
        Assert.Equal(PaymentCallbackService.Handling.AlreadyApplied, second.Outcome);
        Assert.Equal(PaymentCallbackService.Handling.AlreadyApplied, third.Outcome);
        Assert.Equal(OrderPaymentState.Paid, order.PaymentState); // still exactly paid
    }
}
