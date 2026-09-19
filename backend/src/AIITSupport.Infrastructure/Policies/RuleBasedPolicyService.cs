using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;

namespace AIITSupport.Infrastructure.Policies;

// This is the core "AI recommendation is not the final business decision" piece.
// Every rule here is plain, testable C# - nothing is decided by the LLM.
// Swap this out for a rules-table-driven engine later without touching callers,
// since everything talks to IPolicyService, not this class directly.
public class RuleBasedPolicyService : IPolicyService
{
    private const double AutoApproveConfidenceThreshold = 0.85;

    // Categories that must always go to a human, regardless of AI confidence.
    private static readonly string[] AlwaysHumanReviewCategories = { "Security", "Access" };

    public Task<PolicyDecision> EvaluateAsync(Ticket ticket, TicketAnalysisResult analysis)
    {
        // Rule 1: low AI confidence is never trusted for auto-action.
        if (analysis.Confidence < AutoApproveConfidenceThreshold)
        {
            return Task.FromResult(new PolicyDecision(
                RequiresHumanReview: true,
                Reason: $"AI confidence {analysis.Confidence:P0} is below the {AutoApproveConfidenceThreshold:P0} auto-approve threshold.",
                SuggestedAction: analysis.Recommendation));
        }

        // Rule 2: security-sensitive or access-related categories always need a human,
        // even with high confidence - these carry real business/security risk.
        if (AlwaysHumanReviewCategories.Contains(analysis.Category, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PolicyDecision(
                RequiresHumanReview: true,
                Reason: $"Category '{analysis.Category}' is always routed to human review by policy.",
                SuggestedAction: analysis.Recommendation));
        }

        // Rule 3: Critical urgency always gets a human set of eyes, so nothing
        // urgent silently auto-resolves.
        if (string.Equals(analysis.Urgency, "Critical", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PolicyDecision(
                RequiresHumanReview: true,
                Reason: "Critical urgency tickets always require human review.",
                SuggestedAction: analysis.Recommendation));
        }

        // Otherwise: high confidence, low-risk category, non-critical urgency -> eligible
        // for automatic processing.
        return Task.FromResult(new PolicyDecision(
            RequiresHumanReview: false,
            Reason: $"High confidence ({analysis.Confidence:P0}) on a low-risk category with non-critical urgency.",
            SuggestedAction: analysis.Recommendation));
    }
}
