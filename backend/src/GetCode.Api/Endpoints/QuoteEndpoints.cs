using GetCode.Application.Quotes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AppQuotes = GetCode.Application.Quotes;

namespace GetCode.Api.Endpoints;

/// <summary>
/// M05-002: public quote API. Customers receive only the customer view — the
/// provider-cost trace never crosses this boundary. Checkout revalidation
/// distinguishes unknown / expired / tampered explicitly.
/// </summary>
internal static class QuoteEndpoints
{
    public static IEndpointConventionBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes").WithTags("Quotes").AllowAnonymous();

        group.MapPost("/", (AppQuotes.IssueQuoteRequest request, AppQuotes.QuoteService quotes) =>
            {
                try
                {
                    var issued = quotes.Issue(request);
                    return Results.Created($"/api/quotes/{issued.CustomerView.QuoteId}", new QuoteResponse(
                        issued.CustomerView.QuoteId, issued.CustomerView.CountryKey, issued.CustomerView.ServiceKey,
                        issued.CustomerView.CustomerAmount, issued.CustomerView.Currency,
                        issued.CustomerView.IssuedAtUtc, issued.CustomerView.ExpiresAtUtc));
                }
                catch (ArgumentException)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["invalid quote input"] });
                }
            })
            .Produces<QuoteResponse>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}", (Guid id, decimal? expectedAmount, AppQuotes.QuoteService quotes) =>
            {
                if (expectedAmount is null)
                {
                    return Results.BadRequest(new { error = "expected-amount-required" });
                }

                var (result, snapshot) = quotes.ValidateForCheckout(id, expectedAmount.Value);
                return result switch
                {
                    QuoteValidation.Valid => Results.Ok(new QuoteResponse(
                        snapshot!.QuoteId, snapshot.CountryKey, snapshot.ServiceKey, snapshot.CustomerAmount,
                        snapshot.Currency, snapshot.IssuedAtUtc, snapshot.ExpiresAtUtc)),
                    QuoteValidation.NotFound => Results.NotFound(new { error = "quote-not-found" }),
                    QuoteValidation.Expired => Results.StatusCode(StatusCodes.Status410Gone),
                    _ => Results.Conflict(new { error = "quote-tampered" }),
                };
            });

        return group;
    }
}

public sealed record QuoteResponse(Guid QuoteId, string CountryKey, string ServiceKey, decimal Amount, string Currency, DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc);
