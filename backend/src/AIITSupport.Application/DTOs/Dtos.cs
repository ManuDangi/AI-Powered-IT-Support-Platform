namespace AIITSupport.Application.DTOs;

// ---------- Auth ----------
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email, List<string> Roles);

// ---------- Tickets ----------
public record CreateTicketRequest(string Title, string Description);

public record TicketResponse(
    int Id,
    string TicketNumber,
    string Title,
    string Description,
    string Status,
    string Priority,
    string? Category,
    DateTime CreatedAt);

public record UpdateTicketRequest(string? Title, string? Description, string? Status);

// ---------- Phase 2: AI + Policy + Workflow ----------

public record AIAnalysisResponse(
    int TicketId,
    string Category,
    string Urgency,
    double Confidence,
    string Summary,
    string Recommendation,
    DateTime CreatedAt);

public record ProcessResponse(
    int TicketId,
    string Status,
    bool RequiresHumanReview,
    string PolicyReason,
    string SuggestedAction);

public record ApprovalRequest(string? Comments);
public record RejectRequest(string? Comments);
public record EscalateRequest(string Reason);

public record AuditLogResponse(
    int Id,
    int? UserId,
    int? TicketId,
    string Action,
    string Details,
    DateTime CreatedAt);
