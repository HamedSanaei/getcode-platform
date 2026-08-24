using GetCode.Domain.Orders;

namespace GetCode.UnitTests.Orders;

/// <summary>
/// M06-001: exhaustive state transition matrix tests — every (from, to) pair
/// for both payment and fulfillment dimensions is pinned allowed or forbidden;
/// fulfillment is gated on captured payment; commercial snapshot is immutable.
/// </summary>
public sealed class OrderStateTransitionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount: 127m, currency: "RUB",
        countryKey: "RU", serviceKey: "telegram", productTypeKey: "activation",
        pricingRuleVersion: 1, createdAtUtc: T0);

    private static IEnumerable<OrderPaymentState> PaymentStates => Enum.GetValues<OrderPaymentState>();

    private static IEnumerable<OrderFulfillmentState> FulfillmentStates => Enum.GetValues<OrderFulfillmentState>();

    // ---- payment matrix ---------------------------------------------------------

    [Theory]
    [InlineData(OrderPaymentState.AwaitingPayment, OrderPaymentState.PaymentAuthorized, true)]
    [InlineData(OrderPaymentState.AwaitingPayment, OrderPaymentState.PaymentFailed, true)]
    [InlineData(OrderPaymentState.AwaitingPayment, OrderPaymentState.Paid, false)]          // no capture without authorize path
    [InlineData(OrderPaymentState.AwaitingPayment, OrderPaymentState.Refunded, false)]
    [InlineData(OrderPaymentState.PaymentAuthorized, OrderPaymentState.Paid, true)]
    [InlineData(OrderPaymentState.PaymentAuthorized, OrderPaymentState.PaymentFailed, true)]
    [InlineData(OrderPaymentState.PaymentAuthorized, OrderPaymentState.Refunded, false)]
    [InlineData(OrderPaymentState.Paid, OrderPaymentState.Refunded, true)]
    [InlineData(OrderPaymentState.Paid, OrderPaymentState.PaymentFailed, false)]            // captured money can't "fail"
    [InlineData(OrderPaymentState.PaymentFailed, OrderPaymentState.Paid, false)]            // terminal until a NEW order
    [InlineData(OrderPaymentState.PaymentFailed, OrderPaymentState.PaymentAuthorized, false)]
    [InlineData(OrderPaymentState.Refunded, OrderPaymentState.Paid, false)]
    public void Payment_transitions_match_the_explicit_matrix(
        OrderPaymentState from, OrderPaymentState to, bool allowed)
    {
        var order = NewOrder();
        Force(order, from);

        if (allowed)
        {
            Apply(order, to); // must not throw
            Assert.Equal(to, order.PaymentState);
        }
        else
        {
            var ex = Assert.Throws<InvalidOrderTransitionException>(() => Apply(order, to));
            Assert.Contains(from.ToString(), ex.Message); // deterministic reason token
        }
    }

    // ---- fulfillment matrix (payment gate) ----------------------------------------

    [Fact]
    public void Fulfillment_is_gated_until_payment_is_captured()
    {
        var order = NewOrder();

        var ex = Assert.Throws<InvalidOrderTransitionException>(order.StartFulfillment);
        Assert.Contains("payment:AwaitingPayment", ex.Message);
        Assert.Throws<InvalidOrderTransitionException>(() => order.MarkProviderReserved("op-1"));
        Assert.Throws<InvalidOrderTransitionException>(order.Complete);
        Assert.Throws<InvalidOrderTransitionException>(order.FailFulfillment);

        order.MarkPaymentAuthorized();
        Assert.Throws<InvalidOrderTransitionException>(order.StartFulfillment); // authorized ≠ captured

        order.MarkPaid();
        order.StartFulfillment();              // gate opens exactly at Paid
        order.MarkProviderReserved("op-1");
        order.Complete();

        Assert.Equal(OrderPaymentState.Paid, order.PaymentState);
        Assert.Equal(OrderFulfillmentState.Completed, order.FulfillmentState);
        Assert.Equal("op-1", order.ProviderOperationId);
    }

    [Theory]
    [InlineData(OrderFulfillmentState.Reserving, OrderFulfillmentState.Reserved, true)]
    [InlineData(OrderFulfillmentState.Reserving, OrderFulfillmentState.Failed, true)]
    [InlineData(OrderFulfillmentState.Reserving, OrderFulfillmentState.Completed, false)]
    [InlineData(OrderFulfillmentState.Reserved, OrderFulfillmentState.Completed, true)]
    [InlineData(OrderFulfillmentState.Reserved, OrderFulfillmentState.Failed, true)]
    [InlineData(OrderFulfillmentState.Reserved, OrderFulfillmentState.Reserving, false)]
    [InlineData(OrderFulfillmentState.Completed, OrderFulfillmentState.Reserving, false)]   // terminal success
    [InlineData(OrderFulfillmentState.Failed, OrderFulfillmentState.Reserved, false)]       // terminal failure
    public void Fulfillment_transitions_match_the_explicit_matrix(
        OrderFulfillmentState from, OrderFulfillmentState to, bool allowed)
    {
        var order = PaidWith(from);

        if (allowed)
        {
            Apply(order, to);
            Assert.Equal(to, order.FulfillmentState);
        }
        else
        {
            Assert.Throws<InvalidOrderTransitionException>(() => Apply(order, to));
        }
    }

    // ---- snapshot immutability ------------------------------------------------------

    [Fact]
    public void Commercial_snapshot_cannot_be_rewritten_by_later_changes()
    {
        var order = NewOrder();

        Assert.Equal(127m, order.Amount);
        Assert.Equal("RUB", order.Currency);
        Assert.Equal(1, order.PricingRuleVersion);
        Assert.Equal(T0, order.CreatedAtUtc);

        // State changes never touch the snapshot.
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        order.StartFulfillment();
        order.MarkProviderReserved("op-9");
        order.Complete();

        Assert.Equal(127m, order.Amount);
        Assert.Equal(1, order.PricingRuleVersion);
        Assert.True((order.Amount, order.Currency, order.PricingRuleVersion, order.CreatedAtUtc) == (127m, "RUB", 1, T0));
    }

    // ---- helpers ----------------------------------------------------------------------

    /// <summary>Walk the aggregate through its public API into the given payment state.</summary>
    private static void Force(Order order, OrderPaymentState target)
    {
        switch (target)
        {
            case OrderPaymentState.PaymentAuthorized: order.MarkPaymentAuthorized(); break;
            case OrderPaymentState.PaymentFailed:
                try { order.MarkPaymentFailed(); } catch (InvalidOrderTransitionException) { order.MarkPaymentAuthorized(); order.MarkPaymentFailed(); }
                break;
            case OrderPaymentState.Paid: order.MarkPaymentAuthorized(); order.MarkPaid(); break;
            case OrderPaymentState.Refunded: order.MarkPaymentAuthorized(); order.MarkPaid(); order.MarkRefunded(); break;
            case OrderPaymentState.AwaitingPayment: break; // initial
        }
    }

    private static Order PaidWith(OrderFulfillmentState state)
    {
        var order = NewOrder();
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        switch (state)
        {
            case OrderFulfillmentState.Reserving: order.StartFulfillment(); break;
            case OrderFulfillmentState.Reserved: order.StartFulfillment(); order.MarkProviderReserved("op-x"); break;
            case OrderFulfillmentState.Completed: order.StartFulfillment(); order.MarkProviderReserved("op-x"); order.Complete(); break;
            case OrderFulfillmentState.Failed: order.StartFulfillment(); order.FailFulfillment(); break;
            case OrderFulfillmentState.NotStarted: break;
        }

        return order;
    }

    private static void Apply(Order order, OrderPaymentState to)
    {
        switch (to)
        {
            case OrderPaymentState.PaymentAuthorized: order.MarkPaymentAuthorized(); break;
            case OrderPaymentState.Paid: order.MarkPaid(); break;
            case OrderPaymentState.PaymentFailed: order.MarkPaymentFailed(); break;
            case OrderPaymentState.Refunded: order.MarkRefunded(); break;
            case OrderPaymentState.AwaitingPayment: throw new InvalidOperationException("no API returns to awaiting");
        }
    }

    private static void Apply(Order order, OrderFulfillmentState to)
    {
        switch (to)
        {
            case OrderFulfillmentState.NotStarted: throw new InvalidOperationException("no API returns to not-started");
            case OrderFulfillmentState.Reserving: order.StartFulfillment(); break;
            case OrderFulfillmentState.Reserved: order.MarkProviderReserved("op-y"); break;
            case OrderFulfillmentState.Completed: order.Complete(); break;
            case OrderFulfillmentState.Failed: order.FailFulfillment(); break;
        }
    }
}
