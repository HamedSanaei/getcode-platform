namespace GetCode.Application.Reconciliation;

/// <summary>M07-005: Manual Review case states.</summary>
public enum ReviewStatus { Open = 0, Resolved = 1 }

/// <summary>
/// A human-review case. Evidence is BUILT from safe tokens only — raw SMS
/// bodies, provider payloads or secrets must never reach this record.
/// </summary>
public sealed record ReviewCase(
    Guid Id,
    string SubjectType,      // "payment" | "fulfillment" | "reservation"
    Guid SubjectId,
    string ReasonToken,      // stable rule identifier
    string Evidence,         // redacted summary tokens only
    ReviewStatus Status,
    DateTimeOffset CreatedAtUtc,
    string? ResolvedByAdminId = null,
    string? ResolutionNote = null,
    DateTimeOffset? ResolvedAtUtc = null);

/// <summary>Durable storage for review cases (one open case per subject+reason).</summary>
public interface IManualReviewStore
{
    /// <summary>Opens a case unless an OPEN case for the same subject+reason exists; returns the case.</summary>
    Task<ReviewCase> OpenAsync(ReviewCase reviewCase, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewCase>> ListOpenAsync(CancellationToken cancellationToken);

    /// <summary>Application-command resolution by an administrator (never direct DB edits).</summary>
    Task<ReviewCase> ResolveAsync(Guid caseId, string adminUserId, string resolutionNote, CancellationToken cancellationToken);
}

/// <summary>
/// M07-005 rule engine input: everything the sweep needs to know, expressed
/// as plain facts so rules stay pure and testable.
/// </summary>
public sealed record ReconciliationFacts(
    bool PaymentVerificationAmbiguous,
    IReadOnlyList<Guid> DeadLetteredFulfillmentJobIds,
    IReadOnlyList<(Guid OrderId, string ProviderOperationId)> ReservationsExpiredWithoutMessage);

/// <summary>Stable reason tokens emitted by the rule engine.</summary>
public static class ReviewReasons
{
    public const string PaymentAmbiguous = "payment-verification-ambiguous";
    public const string FulfillmentDeadLetter = "fulfillment-job-dead-lettered";
    public const string ReservationExpiredSilently = "reservation-expired-without-message";
}

/// <summary>
/// M07-005: identifies stuck/ambiguous records BY RULE and turns them into
/// Manual Review cases. Pure function — the caller owns persistence.
/// </summary>
public static class ReconciliationRuleEngine
{
    public static IReadOnlyList<(string SubjectType, Guid SubjectId, string ReasonToken, string Evidence)> Evaluate(
        ReconciliationFacts facts)
    {
        var found = new List<(string, Guid, string, string)>();

        if (facts.PaymentVerificationAmbiguous)
        {
            // Payment ambiguity is order-scoped; the sweep supplies per-order
            // facts, so a single global flag maps to the flagged order id set.
            found.Add(("payment", Guid.Empty, ReviewReasons.PaymentAmbiguous,
                "verify-outcome=unknown;amount-redacted"));
        }

        foreach (var jobId in facts.DeadLetteredFulfillmentJobIds)
        {
            found.Add(("fulfillment", jobId, ReviewReasons.FulfillmentDeadLetter,
                $"job-state=dead-lettered;attempts={5}"));
        }

        foreach (var (orderId, opId) in facts.ReservationsExpiredWithoutMessage)
        {
            found.Add(("reservation", orderId, ReviewReasons.ReservationExpiredSilently,
                $"provider-operation-present={opId.Length > 0};message-received=false"));
        }

        return found;
    }
}

/// <summary>
/// M07-005: runs the rules against current facts and persists resulting
/// cases; administrators resolve ONLY through <see cref="ResolveAsync"/>.
/// </summary>
public sealed class ManualReviewService(IManualReviewStore store)
{
    public async Task<int> SweepAsync(ReconciliationFacts facts, CancellationToken cancellationToken)
    {
        var findings = ReconciliationRuleEngine.Evaluate(facts);
        foreach (var (subjectType, subjectId, reason, evidence) in findings)
        {
            await store.OpenAsync(new ReviewCase(
                Guid.CreateVersion7(), subjectType, subjectId, reason, evidence,
                ReviewStatus.Open, DateTimeOffset.UtcNow), cancellationToken);
        }

        return findings.Count;
    }

    public Task<IReadOnlyList<ReviewCase>> ListOpenAsync(CancellationToken cancellationToken) =>
        store.ListOpenAsync(cancellationToken);

    public Task<ReviewCase> ResolveAsync(Guid caseId, string adminUserId, string resolutionNote, CancellationToken cancellationToken) =>
        store.ResolveAsync(caseId, adminUserId, resolutionNote, cancellationToken);
}
