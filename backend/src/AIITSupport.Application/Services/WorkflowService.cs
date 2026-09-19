using AIITSupport.Application.DTOs;
using AIITSupport.Application.Exceptions;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;
using AIITSupport.Domain.Enums;
using AIITSupport.Domain.Interfaces;

namespace AIITSupport.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly ITicketRepository _tickets;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IAIService _aiService;
    private readonly IPolicyService _policyService;

    public WorkflowService(
        ITicketRepository tickets,
        IAuditLogRepository auditLogs,
        IAIService aiService,
        IPolicyService policyService)
    {
        _tickets = tickets;
        _auditLogs = auditLogs;
        _aiService = aiService;
        _policyService = policyService;
    }

    public async Task<AIAnalysisResponse> AnalyzeAsync(int ticketId, int actingUserId)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId)
            ?? throw new InvalidTicketStateException($"Ticket {ticketId} not found.");

        ticket.Status = TicketStatus.AIProcessing;
        await _tickets.SaveChangesAsync();

        try
        {
            var result = await _aiService.AnalyzeTicketAsync(ticket);

            // Sync AI category to the Ticket.CategoryId
            var category = await _tickets.GetCategoryByNameAsync(result.Category);

            Console.WriteLine($"[SYNC DEBUG] AI Category = '{result.Category}'");
            Console.WriteLine($"[SYNC DEBUG] DB Category Found = '{category?.Name}'");
            Console.WriteLine($"[SYNC DEBUG] DB Category Id = '{category?.Id}'");

            if (category != null)
            {
                ticket.CategoryId = category.Id;
                ticket.Category = category;
            }

            // Sync AI urgency to Ticket.Priority
            if (Enum.TryParse<TicketPriority>(result.Urgency, true, out var priority))
            {
                ticket.Priority = priority;
                Console.WriteLine($"[SYNC DEBUG] Priority parsed = '{priority}'");
            }
            else
            {
                Console.WriteLine($"[SYNC DEBUG] Priority parsing FAILED for '{result.Urgency}'");
            }

            Console.WriteLine($"[SYNC DEBUG] Ticket.CategoryId = '{ticket.CategoryId}'");
            Console.WriteLine($"[SYNC DEBUG] Ticket.Priority = '{ticket.Priority}'");

            // Save AI analysis history
            var analysis = new AIAnalysis
            {
                TicketId = ticket.Id,
                Category = result.Category,
                Urgency = result.Urgency,
                Confidence = result.Confidence,
                Summary = result.Summary,
                Recommendation = result.Recommendation,
                RawResponse = result.RawResponse
            };

            await _tickets.AddAnalysisAsync(analysis);

            // Mark ticket as successfully analyzed
            ticket.Status = TicketStatus.AIAnalyzed;
            await _tickets.SaveChangesAsync();

            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = actingUserId,
                TicketId = ticket.Id,
                Action = "AI_ANALYZE",
                Details = $"category={result.Category}, urgency={result.Urgency}, confidence={result.Confidence:F2}"
            });

            return new AIAnalysisResponse(
                ticket.Id,
                result.Category,
                result.Urgency,
                result.Confidence,
                result.Summary,
                result.Recommendation,
                analysis.CreatedAt);
        }
        catch (Exception ex) when (ex is AIProviderException or AIResponseInvalidException)
        {
            ticket.Status = TicketStatus.AIProcessingFailed;
            await _tickets.SaveChangesAsync();

            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = actingUserId,
                TicketId = ticket.Id,
                Action = "AI_ANALYZE_FAILED",
                Details = ex.Message
            });

            throw;
        }
    }
    public async Task<ProcessResponse> ProcessAsync(int ticketId, int actingUserId)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId)
            ?? throw new InvalidTicketStateException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.AIAnalyzed)
            throw new InvalidTicketStateException(
                $"Ticket {ticketId} must be in '{TicketStatus.AIAnalyzed}' status before processing (currently '{ticket.Status}').");

        var latestAnalysis = await _tickets.GetLatestAnalysisAsync(ticketId)
            ?? throw new InvalidTicketStateException($"No AI analysis found for ticket {ticketId}. Run /analyze first.");

        var analysisResult = new TicketAnalysisResult(
            latestAnalysis.Category, latestAnalysis.Urgency, latestAnalysis.Confidence,
            latestAnalysis.Summary, latestAnalysis.Recommendation, latestAnalysis.RawResponse);

        var decision = await _policyService.EvaluateAsync(ticket, analysisResult);

        ticket.Status = TicketStatus.PolicyChecked;
        await _tickets.SaveChangesAsync();

        await _auditLogs.AddAsync(new AuditLog
        {
            UserId = actingUserId,
            TicketId = ticket.Id,
            Action = "POLICY_EVALUATE",
            Details = $"requiresHumanReview={decision.RequiresHumanReview}; reason={decision.Reason}"
        });

        if (decision.RequiresHumanReview)
        {
            ticket.Status = TicketStatus.HumanReview;
            await _tickets.SaveChangesAsync();

            await _tickets.AddActionAsync(new TicketAction
            {
                TicketId = ticket.Id,
                ActionType = decision.SuggestedAction,
                Status = "PendingApproval"
            });
        }
        else
        {
            // Low-risk + high confidence + non-critical: safe to resolve automatically.
            // The suggested action is still recorded for the audit trail.
            ticket.Status = TicketStatus.Resolved;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _tickets.SaveChangesAsync();

            await _tickets.AddActionAsync(new TicketAction
            {
                TicketId = ticket.Id,
                ActionType = decision.SuggestedAction,
                Status = "AutoResolved"
            });

            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = actingUserId,
                TicketId = ticket.Id,
                Action = "AUTO_RESOLVE",
                Details = decision.SuggestedAction
            });
        }

        return new ProcessResponse(ticket.Id, ticket.Status.ToString(),
            decision.RequiresHumanReview, decision.Reason, decision.SuggestedAction);
    }

    public async Task<TicketResponse> ApproveAsync(int ticketId, int reviewerId, string? comments)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId)
            ?? throw new InvalidTicketStateException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.HumanReview)
            throw new InvalidTicketStateException(
                $"Ticket {ticketId} is not awaiting human review (currently '{ticket.Status}').");

        await _tickets.AddApprovalAsync(new Approval
        {
            TicketId = ticket.Id,
            ReviewerId = reviewerId,
            Decision = ApprovalDecision.Approved,
            Comments = comments
        });

        ticket.Status = TicketStatus.Resolved;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _tickets.SaveChangesAsync();

        await _auditLogs.AddAsync(new AuditLog
        {
            UserId = reviewerId,
            TicketId = ticket.Id,
            Action = "APPROVE",
            Details = comments ?? "(no comments)"
        });

        return MapToResponse(ticket);
    }

    public async Task<TicketResponse> RejectAsync(int ticketId, int reviewerId, string? comments)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId)
            ?? throw new InvalidTicketStateException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.HumanReview)
            throw new InvalidTicketStateException(
                $"Ticket {ticketId} is not awaiting human review (currently '{ticket.Status}').");

        await _tickets.AddApprovalAsync(new Approval
        {
            TicketId = ticket.Id,
            ReviewerId = reviewerId,
            Decision = ApprovalDecision.Rejected,
            Comments = comments
        });

        ticket.Status = TicketStatus.Rejected;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _tickets.SaveChangesAsync();

        await _auditLogs.AddAsync(new AuditLog
        {
            UserId = reviewerId,
            TicketId = ticket.Id,
            Action = "REJECT",
            Details = comments ?? "(no comments)"
        });

        return MapToResponse(ticket);
    }

    public async Task<TicketResponse> EscalateAsync(int ticketId, int actingUserId, string reason)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId)
            ?? throw new InvalidTicketStateException($"Ticket {ticketId} not found.");

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Rejected or TicketStatus.Escalated)
            throw new InvalidTicketStateException(
                $"Ticket {ticketId} is already in a terminal or escalated state ('{ticket.Status}').");

        ticket.Status = TicketStatus.Escalated;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _tickets.SaveChangesAsync();

        await _auditLogs.AddAsync(new AuditLog
        {
            UserId = actingUserId,
            TicketId = ticket.Id,
            Action = "ESCALATE",
            Details = reason
        });

        return MapToResponse(ticket);
    }

    public async Task<AIAnalysisResponse?> GetLatestAnalysisAsync(int ticketId)
    {
        var analysis = await _tickets.GetLatestAnalysisAsync(ticketId);
        if (analysis == null) return null;

        return new AIAnalysisResponse(ticketId, analysis.Category, analysis.Urgency,
            analysis.Confidence, analysis.Summary, analysis.Recommendation, analysis.CreatedAt);
    }

    private static TicketResponse MapToResponse(Ticket t) => new(
        t.Id, t.TicketNumber, t.Title, t.Description, t.Status.ToString(),
        t.Priority.ToString(), t.Category?.Name, t.CreatedAt);
}

