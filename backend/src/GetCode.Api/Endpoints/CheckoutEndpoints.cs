using GetCode.Application.Orders;
using GetCode.Application.Quotes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GetCode.Api.Endpoints;

/// <summary>
/// M08-002 groundwork: authenticated checkout API. The client submits its
/// quote id, the amount IT was shown, and a stable idempotency key that must
/// be reused across retries of one submit intent. Duplicate submits (double
/// click, network retry) deterministically resolve to the SAME order. The
/// server revalidates the quote — locally calculated prices are never trusted.
/// </summary>
internal static class CheckoutEndpoints
{
    public static IEndpointConventionBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checkout").WithTags("Checkout").RequireAuthorization();

        group.MapPost("/", async (CheckoutRequest request, CheckoutService checkout, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var customerId))
            {
                return Results.Unauthorized();
            }

            if (request.QuoteId == Guid.Empty || request.ExpectedAmount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["quoteId, expectedAmount and idempotencyKey are required"] });
            }

            try
            {
                var result = await checkout.CreateOrderAsync(
                    customerId, request.QuoteId, request.ExpectedAmount, request.IdempotencyKey.Trim(), ct);
                return Results.Ok(new CheckoutResponse(result.Order.Id, result.Replayed,
                    result.Order.Amount, result.Order.Currency));
            }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("checkout-quote-"))
            {
                return failure.Message switch
                {
                    "checkout-quote-expired" => Results.StatusCode(StatusCodes.Status410Gone),
                    "checkout-quote-not-found" => Results.NotFound(new { error = "quote-not-found" }),
                    _ => Results.Conflict(new { error = "quote-tampered" }),
                };
            }
        });

        return group;
    }
}

public sealed record CheckoutRequest(Guid QuoteId, decimal ExpectedAmount, string IdempotencyKey);
public sealed record CheckoutResponse(Guid OrderId, bool Replayed, decimal Amount, string Currency);
