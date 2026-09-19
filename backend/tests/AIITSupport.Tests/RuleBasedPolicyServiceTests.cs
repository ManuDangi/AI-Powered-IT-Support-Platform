using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;
using AIITSupport.Infrastructure.Policies;
using Xunit;

namespace AIITSupport.Tests;

public class RuleBasedPolicyServiceTests
{
    private readonly RuleBasedPolicyService _policyService = new();

    [Fact]
    public async Task LowConfidence_ShouldRequireHumanReview()
    {
        var ticket = new Ticket();

        var analysis = new TicketAnalysisResult(
            "VPN",
            "High",
            0.70,
            "VPN issue",
            "Check VPN settings",
            "{}");

        var result = await _policyService.EvaluateAsync(ticket, analysis);

        Assert.True(result.RequiresHumanReview);
        Assert.Contains("below", result.Reason);
    }

    [Fact]
    public async Task SecurityCategory_ShouldRequireHumanReview()
    {
        var ticket = new Ticket();

        var analysis = new TicketAnalysisResult(
            "Security",
            "High",
            0.95,
            "Security issue",
            "Investigate security issue",
            "{}");

        var result = await _policyService.EvaluateAsync(ticket, analysis);

        Assert.True(result.RequiresHumanReview);
        Assert.Contains("always routed to human review", result.Reason);
    }

    [Fact]
    public async Task AccessCategory_ShouldRequireHumanReview()
    {
        var ticket = new Ticket();

        var analysis = new TicketAnalysisResult(
            "Access",
            "High",
            0.95,
            "Access issue",
            "Verify permissions",
            "{}");

        var result = await _policyService.EvaluateAsync(ticket, analysis);

        Assert.True(result.RequiresHumanReview);
    }

    [Fact]
    public async Task CriticalUrgency_ShouldRequireHumanReview()
    {
        var ticket = new Ticket();

        var analysis = new TicketAnalysisResult(
            "Network",
            "Critical",
            0.95,
            "Network outage",
            "Investigate network",
            "{}");

        var result = await _policyService.EvaluateAsync(ticket, analysis);

        Assert.True(result.RequiresHumanReview);
        Assert.Contains("Critical urgency", result.Reason);
    }

    [Fact]
    public async Task HighConfidenceLowRisk_ShouldAllowAutoProcessing()
    {
        var ticket = new Ticket();

        var analysis = new TicketAnalysisResult(
            "VPN",
            "High",
            0.95,
            "VPN issue",
            "Check VPN settings",
            "{}");

        var result = await _policyService.EvaluateAsync(ticket, analysis);

        Assert.False(result.RequiresHumanReview);
        Assert.Contains("High confidence", result.Reason);
    }
}