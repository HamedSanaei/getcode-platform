using GetCode.Application.Reconciliation;

namespace GetCode.UnitTests.Reconciliation;

/// <summary>
/// M07-005: rule-based detection of stuck/ambiguous records, secret-free
/// evidence, and admin resolution through application commands only.
/// </summary>
public sealed class ManualReviewTests
{
    private sealed class MemoryStore : IManualReviewStore
    {
        public readonly List<ReviewCase> Cases = [];

        public Task<ReviewCase> OpenAsync(ReviewCase reviewCase, CancellationToken ct)
        {
            var existing = Cases.FirstOrDefault(c =>
                c.Status == ReviewStatus.Open &&
                c.SubjectType == reviewCase.SubjectType &&
                c.SubjectId == reviewCase.SubjectId &&
                c.ReasonToken == reviewCase.ReasonToken);
            if (existing is not null)
            {
                return Task.FromResult(existing); // dedupe: one open case per subject+reason
            }

            Cases.Add(reviewCase);
            return Task.FromResult(reviewCase);
        }

        public Task<IReadOnlyList<ReviewCase>> ListOpenAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReviewCase>>(Cases.Where(c => c.Status == ReviewStatus.Open).ToList());

        public async Task<ReviewCase> ResolveAsync(Guid caseId, string adminUserId, string note, CancellationToken ct)
        {
            var index = Cases.FindIndex(c => c.Id == caseId);
            if (index < 0)
            {
                throw new InvalidOperationException("review-case-missing");
            }

            if (Cases[index].Status == ReviewStatus.Resolved)
            {
                throw new InvalidOperationException("review-case-already-resolved"); // audit-safe double resolve guard
            }

            var resolved = Cases[index] with
            {
                Status = ReviewStatus.Resolved,
                ResolvedByAdminId = adminUserId,
                ResolutionNote = note,
                ResolvedAtUtc = DateTimeOffset.UtcNow,
            };
            Cases[index] = resolved;
            await Task.Yield();
            return resolved;
        }
    }

    [Fact]
    public void Rules_flag_unknown_payments_deadletters_and_silent_expirations()
    {
        var jobA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var findings = ReconciliationRuleEngine.Evaluate(new ReconciliationFacts(
            PaymentVerificationAmbiguous: true,
            DeadLetteredFulfillmentJobIds: [jobA],
            ReservationsExpiredWithoutMessage: [(orderB, "op-123")]));

        Assert.Equal(3, findings.Count);
        Assert.Contains(findings, f => f.ReasonToken == ReviewReasons.PaymentAmbiguous && f.SubjectType == "payment");
        Assert.Contains(findings, f => f.ReasonToken == ReviewReasons.FulfillmentDeadLetter && f.SubjectId == jobA);
        Assert.Contains(findings, f => f.ReasonToken == ReviewReasons.ReservationExpiredSilently && f.SubjectId == orderB);
    }

    [Fact]
    public void Healthy_systems_produce_zero_findings()
    {
        var findings = ReconciliationRuleEngine.Evaluate(new ReconciliationFacts(false, [], []));
        Assert.Empty(findings);
    }

    [Fact]
    public async Task Sweep_opens_cases_with_secret_free_evidence()
    {
        var store = new MemoryStore();
        var service = new ManualReviewService(store);
        var jobId = Guid.NewGuid();

        await service.SweepAsync(new ReconciliationFacts(
            PaymentVerificationAmbiguous: true,
            DeadLetteredFulfillmentJobIds: [jobId],
            ReservationsExpiredWithoutMessage: []), TestContext.Current.CancellationToken);

        var open = await service.ListOpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, open.Count);
        foreach (var reviewCase in open)
        {
            // Evidence tokens only — never raw payloads, bodies or credentials.
            Assert.DoesNotContain("sms", reviewCase.Evidence, StringComparison.OrdinalIgnoreCase);
            Assert.Matches(@"^[\w;=\-]+$", reviewCase.Evidence); // token grammar
        }

        var deadLetterCase = open.Single(c => c.ReasonToken == ReviewReasons.FulfillmentDeadLetter);
        Assert.Equal(jobId, deadLetterCase.SubjectId);
    }

    [Fact]
    public async Task Repeated_sweeps_dedupe_to_one_open_case_per_subject_reason()
    {
        var store = new MemoryStore();
        var service = new ManualReviewService(store);
        var facts = new ReconciliationFacts(true, [], []);

        await service.SweepAsync(facts, TestContext.Current.CancellationToken);
        await service.SweepAsync(facts, TestContext.Current.CancellationToken);

        Assert.Single(await service.ListOpenAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Admin_resolution_is_an_audited_application_command()
    {
        var store = new MemoryStore();
        var service = new ManualReviewService(store);
        await service.SweepAsync(
            new ReconciliationFacts(true, [], []), TestContext.Current.CancellationToken);
        var reviewCase = (await service.ListOpenAsync(TestContext.Current.CancellationToken)).Single();

        var resolved = await service.ResolveAsync(
            reviewCase.Id, "admin-42", "verified with provider dashboard; refunded manually",
            TestContext.Current.CancellationToken);

        Assert.Equal(ReviewStatus.Resolved, resolved.Status);
        Assert.Equal("admin-42", resolved.ResolvedByAdminId);       // actor recorded
        Assert.NotNull(resolved.ResolvedAtUtc);                     // when recorded
        Assert.Contains("refunded", resolved.ResolutionNote);       // decision trail

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveAsync(reviewCase.Id, "admin-7", "second try",
                TestContext.Current.CancellationToken));            // no silent re-resolution
        Assert.Equal("admin-42", store.Cases.Single().ResolvedByAdminId); // first resolution stands
    }
}
