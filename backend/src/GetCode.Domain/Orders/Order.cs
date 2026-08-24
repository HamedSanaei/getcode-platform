namespace GetCode.Domain.Orders;

/// <summary>
/// M06-001: order state names deliberately SEPARATE payment outcomes from
/// fulfillment outcomes — a paid-but-unfulfilled order is a normal, expressible
/// state, and a failed provider reservation never means the money vanished.
/// </summary>
public enum OrderPaymentState
{
    /// <summary>Created; waiting for the customer to pay.</summary>
    AwaitingPayment = 0,
    /// <summary>Gateway authorized funds; capture pending.</summary>
    PaymentAuthorized = 1,
    /// <summary>Payment definitively captured. Fulfillment may start.</summary>
    Paid = 2,
    /// <summary>Payment failed or was rejected by the gateway.</summary>
    PaymentFailed = 3,
    /// <summary>Captured funds returned to the customer (full refund).</summary>
    Refunded = 4,
}

public enum OrderFulfillmentState
{
    /// <summary>No fulfillment activity yet (payment not captured).</summary>
    NotStarted = 0,
    /// <summary>Provider reservation attempt in progress.</summary>
    Reserving = 1,
    /// <summary>Provider accepted the reservation; SMS expected.</summary>
    Reserved = 2,
    /// <summary>Delivered to the customer (SMS revealed / rental active).</summary>
    Completed = 3,
    /// <summary>Fulfillment failed after payment; requires ops action/refund flow.</summary>
    Failed = 4,
}

/// <summary>Deterministic guard failure for illegal transitions.</summary>
public sealed class InvalidOrderTransitionException(string from, string to)
    : InvalidOperationException($"order-transition-forbidden:{from}->{to}")
{
    public string From { get; } = from;
    public string To { get; } = to;
}

/// <summary>
/// M06-001: Order aggregate root. The commercial snapshot taken at creation
/// (price, currency, product identity, pricing-rule version, quote reference)
/// is immutable — later rule changes can never rewrite what the customer owes.
/// All mutations go through explicit transition guards; invalid transitions fail
/// deterministically with <see cref="InvalidOrderTransitionException"/>.
/// </summary>
public sealed class Order
{
    // Explicit, closed transition matrices (from → allowed targets).
    private static readonly Dictionary<OrderPaymentState, OrderPaymentState[]> PaymentTransitions = new()
    {
        [OrderPaymentState.AwaitingPayment] = [OrderPaymentState.PaymentAuthorized, OrderPaymentState.PaymentFailed, OrderPaymentState.AwaitingPayment /* idempotent callback replays */],
        [OrderPaymentState.PaymentAuthorized] = [OrderPaymentState.Paid, OrderPaymentState.PaymentFailed, OrderPaymentState.PaymentAuthorized],
        [OrderPaymentState.Paid] = [OrderPaymentState.Refunded, OrderPaymentState.Paid],
        [OrderPaymentState.PaymentFailed] = [],                       // terminal until new order
        [OrderPaymentState.Refunded] = [],
    };

    private static readonly Dictionary<OrderFulfillmentState, OrderFulfillmentState[]> FulfillmentTransitions = new()
    {
        [OrderFulfillmentState.NotStarted] = [OrderFulfillmentState.Reserving, OrderFulfillmentState.NotStarted],
        [OrderFulfillmentState.Reserving] = [OrderFulfillmentState.Reserved, OrderFulfillmentState.Failed, OrderFulfillmentState.Reserving],
        [OrderFulfillmentState.Reserved] = [OrderFulfillmentState.Completed, OrderFulfillmentState.Failed, OrderFulfillmentState.Reserved],
        [OrderFulfillmentState.Completed] = [OrderFulfillmentState.Completed],  // terminal success
        [OrderFulfillmentState.Failed] = [OrderFulfillmentState.Failed],        // terminal failure (refund via payment side)
    };

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid QuoteId { get; }

    /// <summary>Durable client-supplied idempotency key; unique per customer.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Immutable commercial snapshot (support/audit truth).</summary>
    public decimal Amount { get; }
    public string Currency { get; }
    public string CountryKey { get; }
    public string ServiceKey { get; }
    public string ProductTypeKey { get; }
    public int PricingRuleVersion { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public OrderPaymentState PaymentState { get; private set; } = OrderPaymentState.AwaitingPayment;
    public OrderFulfillmentState FulfillmentState { get; private set; } = OrderFulfillmentState.NotStarted;

    /// <summary>Canonical provider operation reference once reserved (audit link).</summary>
    public string? ProviderOperationId { get; private set; }

    public Order(Guid id, Guid customerId, Guid quoteId, string idempotencyKey, decimal amount, string currency,
        string countryKey, string serviceKey, string productTypeKey, int pricingRuleVersion, DateTimeOffset createdAtUtc)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "order amount must be positive");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CustomerId = customerId;
        QuoteId = quoteId;
        IdempotencyKey = idempotencyKey;
        Amount = amount;
        Currency = currency;
        CountryKey = countryKey;
        ServiceKey = serviceKey;
        ProductTypeKey = productTypeKey;
        PricingRuleVersion = pricingRuleVersion;
        CreatedAtUtc = createdAtUtc;
    }

    // ---- payment transitions ------------------------------------------------

    public void MarkPaymentAuthorized()
    {
        PaymentState = Next(PaymentTransitions, PaymentState, OrderPaymentState.PaymentAuthorized);
    }

    public void MarkPaid()
    {
        // Opening the payment gate is what allows fulfillment transitions.
        PaymentState = Next(PaymentTransitions, PaymentState, OrderPaymentState.Paid);
    }

    public void MarkPaymentFailed()
    {
        PaymentState = Next(PaymentTransitions, PaymentState, OrderPaymentState.PaymentFailed);
    }

    public void MarkRefunded()
    {
        // Refunds are only meaningful for captured money.
        if (PaymentState != OrderPaymentState.Paid && PaymentState != OrderPaymentState.Refunded)
        {
            throw new InvalidOrderTransitionException(PaymentState.ToString(), OrderPaymentState.Refunded.ToString());
        }

        PaymentState = OrderPaymentState.Refunded;
    }

    // ---- fulfillment transitions (gated on captured payment) -----------------

    public void StartFulfillment()
    {
        EnsurePaid();
        FulfillmentState = Next(FulfillmentTransitions, FulfillmentState, OrderFulfillmentState.Reserving);
    }

    public void MarkProviderReserved(string providerOperationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOperationId);
        EnsurePaid();
        FulfillmentState = Next(FulfillmentTransitions, FulfillmentState, OrderFulfillmentState.Reserved);
        ProviderOperationId = providerOperationId;
    }

    public void Complete()
    {
        EnsurePaid();
        FulfillmentState = Next(FulfillmentTransitions, FulfillmentState, OrderFulfillmentState.Completed);
    }

    public void FailFulfillment()
    {
        EnsurePaid();
        FulfillmentState = Next(FulfillmentTransitions, FulfillmentState, OrderFulfillmentState.Failed);
    }

    private void EnsurePaid()
    {
        if (PaymentState != OrderPaymentState.Paid)
        {
            throw new InvalidOrderTransitionException(
                $"fulfillment:{FulfillmentState}", $"payment:{PaymentState}");
        }
    }

    private static TState Next<TState>(
        IReadOnlyDictionary<TState, TState[]> matrix, TState current, TState target)
        where TState : struct, Enum
    {
        if (!matrix[current].Contains(target))
        {
            throw new InvalidOrderTransitionException(current.ToString(), target.ToString());
        }

        return target;
    }
}
