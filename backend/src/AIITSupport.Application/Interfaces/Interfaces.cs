using AIITSupport.Application.DTOs;
using AIITSupport.Domain.Entities;

namespace AIITSupport.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, IEnumerable<string> roles);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public interface ITicketService
{
    Task<TicketResponse> CreateAsync(int userId, CreateTicketRequest request);
    Task<List<TicketResponse>> GetForUserAsync(int userId, bool isAgentOrAdmin);
    Task<TicketResponse?> GetByIdAsync(int ticketId, int userId, bool isAgentOrAdmin);
    Task<TicketResponse?> UpdateAsync(int ticketId, int userId, bool isAgentOrAdmin, UpdateTicketRequest request);
}

// ---------- Phase 2 interfaces (implemented later, kept here so the
// controllers / DI wiring for the next phase are easy to add) ----------

public record TicketAnalysisResult(
    string Category,
    string Urgency,
    double Confidence,
    string Summary,
    string Recommendation,
    string RawResponse);

public interface IAIService
{
    Task<TicketAnalysisResult> AnalyzeTicketAsync(Ticket ticket);
}

public record PolicyDecision(bool RequiresHumanReview, string Reason, string SuggestedAction);

public interface IPolicyService
{
    Task<PolicyDecision> EvaluateAsync(Ticket ticket, TicketAnalysisResult analysis);
}

// ---------- Workflow orchestration (Analyze -> Policy -> Approve/Reject/Escalate) ----------

public interface IWorkflowService
{
    Task<AIAnalysisResponse> AnalyzeAsync(int ticketId, int actingUserId);
    Task<ProcessResponse> ProcessAsync(int ticketId, int actingUserId);
    Task<TicketResponse> ApproveAsync(int ticketId, int reviewerId, string? comments);
    Task<TicketResponse> RejectAsync(int ticketId, int reviewerId, string? comments);
    Task<TicketResponse> EscalateAsync(int ticketId, int actingUserId, string reason);
    Task<AIAnalysisResponse?> GetLatestAnalysisAsync(int ticketId);
}
