using AIITSupport.Application.DTOs;
using AIITSupport.Application.Exceptions;
using AIITSupport.Application.Interfaces;
using AIITSupport.Application.Services;
using AIITSupport.Domain.Entities;
using AIITSupport.Domain.Enums;
using AIITSupport.Domain.Interfaces;
using Moq;
using Xunit;

namespace AIITSupport.Tests;

public class WorkflowServiceTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAuditLogRepository> _auditLogs = new();
    private readonly Mock<IAIService> _aiService = new();
    private readonly Mock<IPolicyService> _policyService = new();

    private WorkflowService CreateService()
    {
        return new WorkflowService(
            _tickets.Object,
            _auditLogs.Object,
            _aiService.Object,
            _policyService.Object);
    }

    private static Ticket CreateTicket(
        int id = 1,
        TicketStatus status = TicketStatus.New)
    {
        return new Ticket
        {
            Id = id,
            TicketNumber = $"TCK-{id}",
            Title = "Cannot connect to VPN",
            Description = "The company VPN is not connecting.",
            Status = status
        };
    }

    private static TicketAnalysisResult CreateAnalysis(
        string category = "VPN",
        string urgency = "High",
        double confidence = 0.95)
    {
        return new TicketAnalysisResult(
            category,
            urgency,
            confidence,
            "User cannot connect to VPN.",
            "Check VPN settings and credentials.",
            "{}");
    }


    [Fact]
    public async Task AnalyzeAsync_WhenAIAnalysisSucceeds_ShouldSaveAnalysisAndReturnResult()
    {
        // Arrange
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _aiService
            .Setup(x => x.AnalyzeTicketAsync(ticket))
            .ReturnsAsync(CreateAnalysis());

        _tickets
            .Setup(x => x.AddAnalysisAsync(It.IsAny<AIAnalysis>()))
            .Returns(Task.CompletedTask);

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TicketId);
        Assert.Equal("VPN", result.Category);
        Assert.Equal("High", result.Urgency);
        Assert.Equal(0.95, result.Confidence);

        Assert.Equal(TicketStatus.AIAnalyzed, ticket.Status);

        _tickets.Verify(
            x => x.AddAnalysisAsync(It.Is<AIAnalysis>(a =>
                a.TicketId == 1 &&
                a.Category == "VPN" &&
                a.Urgency == "High" &&
                a.Confidence == 0.95)),
            Times.Once);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.UserId == 10 &&
                a.Action == "AI_ANALYZE")),
            Times.Once);
    }


    [Fact]
    public async Task AnalyzeAsync_WhenAIProviderFails_ShouldMarkTicketAsProcessingFailed()
    {
        // Arrange
        var ticket = CreateTicket();

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _aiService
            .Setup(x => x.AnalyzeTicketAsync(ticket))
            .ThrowsAsync(
                new AIProviderException(
                    "AI provider failed after 3 attempts for ticket 1.",
                    null));

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<AIProviderException>(
            () => service.AnalyzeAsync(1, 10));

        Assert.Equal(
            TicketStatus.AIProcessingFailed,
            ticket.Status);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.UserId == 10 &&
                a.Action == "AI_ANALYZE_FAILED")),
            Times.Once);
    }


    [Fact]
    public async Task ProcessAsync_WhenPolicyRequiresHumanReview_ShouldMoveTicketToHumanReview()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.AIAnalyzed);

        var analysis = new AIAnalysis
        {
            TicketId = 1,
            Category = "Access",
            Urgency = "High",
            Confidence = 0.94,
            Summary = "User cannot access email.",
            Recommendation = "Verify access permissions.",
            RawResponse = "{}"
        };

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _tickets
            .Setup(x => x.GetLatestAnalysisAsync(1))
            .ReturnsAsync(analysis);

        _policyService
            .Setup(x => x.EvaluateAsync(
                ticket,
                It.IsAny<TicketAnalysisResult>()))
            .ReturnsAsync(
                new PolicyDecision(
                    true,
                    "Access category requires human review.",
                    "Verify access permissions."));

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _tickets
            .Setup(x => x.AddActionAsync(It.IsAny<TicketAction>()))
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ProcessAsync(1, 10);

        // Assert
        Assert.Equal(TicketStatus.HumanReview.ToString(), result.Status);
        Assert.True(result.RequiresHumanReview);
        Assert.Equal(TicketStatus.HumanReview, ticket.Status);

        _tickets.Verify(
            x => x.AddActionAsync(It.Is<TicketAction>(a =>
                a.TicketId == 1 &&
                a.Status == "PendingApproval")),
            Times.Once);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.Action == "POLICY_EVALUATE")),
            Times.Once);
    }


    [Fact]
    public async Task ProcessAsync_WhenPolicyAllowsAutoResolution_ShouldResolveTicket()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.AIAnalyzed);

        var analysis = new AIAnalysis
        {
            TicketId = 1,
            Category = "VPN",
            Urgency = "High",
            Confidence = 0.95,
            Summary = "User cannot connect to VPN.",
            Recommendation = "Check VPN settings and credentials.",
            RawResponse = "{}"
        };

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _tickets
            .Setup(x => x.GetLatestAnalysisAsync(1))
            .ReturnsAsync(analysis);

        _policyService
            .Setup(x => x.EvaluateAsync(
                ticket,
                It.IsAny<TicketAnalysisResult>()))
            .ReturnsAsync(
                new PolicyDecision(
                    false,
                    "High confidence on low-risk category.",
                    "Check VPN settings and credentials."));

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _tickets
            .Setup(x => x.AddActionAsync(It.IsAny<TicketAction>()))
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ProcessAsync(1, 10);

        // Assert
        Assert.Equal(TicketStatus.Resolved.ToString(), result.Status);
        Assert.False(result.RequiresHumanReview);
        Assert.Equal(TicketStatus.Resolved, ticket.Status);

        _tickets.Verify(
            x => x.AddActionAsync(It.Is<TicketAction>(a =>
                a.TicketId == 1 &&
                a.Status == "AutoResolved")),
            Times.Once);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.Action == "AUTO_RESOLVE")),
            Times.Once);
    }


    [Fact]
    public async Task ProcessAsync_WhenTicketIsNotAIAnalyzed_ShouldThrowInvalidTicketStateException()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.New);

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidTicketStateException>(
                () => service.ProcessAsync(1, 10));

        Assert.Contains(
            "must be in",
            exception.Message);
    }


    [Fact]
    public async Task ApproveAsync_WhenTicketIsInHumanReview_ShouldResolveTicket()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.HumanReview);

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _tickets
            .Setup(x => x.AddApprovalAsync(It.IsAny<Approval>()))
            .Returns(Task.CompletedTask);

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result =
            await service.ApproveAsync(
                1,
                20,
                "Resolution approved.");

        // Assert
        Assert.Equal(
            TicketStatus.Resolved.ToString(),
            result.Status);

        Assert.Equal(
            TicketStatus.Resolved,
            ticket.Status);

        _tickets.Verify(
            x => x.AddApprovalAsync(It.Is<Approval>(a =>
                a.TicketId == 1 &&
                a.ReviewerId == 20 &&
                a.Decision == ApprovalDecision.Approved &&
                a.Comments == "Resolution approved.")),
            Times.Once);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.UserId == 20 &&
                a.Action == "APPROVE")),
            Times.Once);
    }


    [Fact]
    public async Task RejectAsync_WhenTicketIsInHumanReview_ShouldRejectTicket()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.HumanReview);

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _tickets
            .Setup(x => x.AddApprovalAsync(It.IsAny<Approval>()))
            .Returns(Task.CompletedTask);

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result =
            await service.RejectAsync(
                1,
                20,
                "Needs further investigation.");

        // Assert
        Assert.Equal(
            TicketStatus.Rejected.ToString(),
            result.Status);

        Assert.Equal(
            TicketStatus.Rejected,
            ticket.Status);

        _tickets.Verify(
            x => x.AddApprovalAsync(It.Is<Approval>(a =>
                a.TicketId == 1 &&
                a.ReviewerId == 20 &&
                a.Decision == ApprovalDecision.Rejected &&
                a.Comments == "Needs further investigation.")),
            Times.Once);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.UserId == 20 &&
                a.Action == "REJECT")),
            Times.Once);
    }


    [Fact]
    public async Task EscalateAsync_WhenTicketIsActive_ShouldEscalateTicket()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.HumanReview);

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        _tickets
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogs
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result =
            await service.EscalateAsync(
                1,
                10,
                "Requires senior support intervention.");

        // Assert
        Assert.Equal(
            TicketStatus.Escalated.ToString(),
            result.Status);

        Assert.Equal(
            TicketStatus.Escalated,
            ticket.Status);

        _auditLogs.Verify(
            x => x.AddAsync(It.Is<AuditLog>(a =>
                a.TicketId == 1 &&
                a.UserId == 10 &&
                a.Action == "ESCALATE" &&
                a.Details == "Requires senior support intervention.")),
            Times.Once);
    }


    [Fact]
    public async Task EscalateAsync_WhenTicketIsAlreadyResolved_ShouldThrowInvalidTicketStateException()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            TicketStatus.Resolved);

        _tickets
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ticket);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidTicketStateException>(
            () => service.EscalateAsync(
                1,
                10,
                "Escalation attempted."));
    }
}