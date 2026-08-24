using GetCode.Application.Orders;
using GetCode.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Orders;

/// <summary>M06-002: EF order repository; unique-index violations surface as OrderAlreadyExistsException.</summary>
public sealed class OrderRepository(GetCodeDbContext db) : IOrderRepository
{
    public Task<Order?> FindByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Set<Order>().SingleOrDefaultAsync(
            o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        db.Set<Order>().Add(order);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("orders_customer_id_idempotency_key", StringComparison.Ordinal) == true
            || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            db.Entry(order).State = EntityState.Detached;
            throw new OrderAlreadyExistsException();
        }
    }
}
