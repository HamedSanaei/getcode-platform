using GetCode.Domain.Orders;

namespace GetCode.Application.Orders;

/// <summary>M06-002: order persistence port. Implementations must guarantee the (CustomerId, IdempotencyKey) uniqueness durably.</summary>
public interface IOrderRepository
{
    /// <summary>Find an existing order by durable idempotency scope, if any.</summary>
    Task<Order?> FindByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>M06-004: find by order id (callback handling).</summary>
    Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Persist a new order. Throws a unique-constraint failure when the idempotency scope already exists.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken);

    /// <summary>M06-004: persist state transitions of an existing order.</summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken);
}

/// <summary>
/// M06-002: idempotent checkout — validates the quote, then creates the order
/// with the customer-scoped idempotency key enforced by the database.
/// <para>
/// Duplicate submits (including racing ones) resolve to the SAME order. No
/// external provider/payment call happens inside the persistence transaction:
/// this use case only creates the order; fulfillment is a later, separately
/// compensated flow (M07).
/// </para>
/// </summary>
public sealed class CheckoutService(IOrderRepository orders, Quotes.QuoteService quotes)
{
    public sealed record CheckoutResult(Order Order, bool Replayed);

    public async Task<CheckoutResult> CreateOrderAsync(
        Guid customerId, Guid quoteId, decimal presentedAmount, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        // Replay short-circuit (best effort; the unique index is the authority).
        var existing = await orders.FindByIdempotencyKeyAsync(customerId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return new CheckoutResult(existing, Replayed: true);
        }

        var (validation, snapshot) = quotes.ValidateForCheckout(quoteId, presentedAmount, cancellationToken);
        if (validation == Quotes.QuoteValidation.NotFound || snapshot is null)
        {
            throw new InvalidOperationException("checkout-quote-not-found");
        }

        if (validation == Quotes.QuoteValidation.Expired)
        {
            throw new InvalidOperationException("checkout-quote-expired");
        }

        if (validation == Quotes.QuoteValidation.Tampered)
        {
            throw new InvalidOperationException("checkout-quote-tampered");
        }

        var order = new Order(
            Guid.Empty, customerId, quoteId, idempotencyKey,
            snapshot.CustomerAmount, snapshot.Currency,
            snapshot.CountryKey, snapshot.ServiceKey, snapshot.ProductTypeKey,
            snapshot.PricingRuleVersion, DateTimeOffset.UtcNow);

        try
        {
            await orders.AddAsync(order, cancellationToken);
            return new CheckoutResult(order, Replayed: false);
        }
        catch (OrderAlreadyExistsException)
        {
            // Concurrent duplicate: the winner's row is the truth.
            var winner = await orders.FindByIdempotencyKeyAsync(customerId, idempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("checkout-idempotent-replay-missing-order");
            return new CheckoutResult(winner, Replayed: true);
        }
    }
}

/// <summary>Maps to a unique-constraint violation on the idempotency index.</summary>
public sealed class OrderAlreadyExistsException() : Exception("order-idempotency-conflict");
