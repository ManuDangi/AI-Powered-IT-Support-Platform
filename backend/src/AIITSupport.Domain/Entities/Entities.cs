using AIITSupport.Domain.Enums;

namespace AIITSupport.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Employee / SupportAgent / Admin

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class TicketCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // VPN, Password, Laptop, Email, Software, Network, Access, Security

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? CategoryId { get; set; }
    public TicketCategory? Category { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
    public ICollection<TicketAction> Actions { get; set; } = new List<TicketAction>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}

public class TicketMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AIAnalysis
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string Category { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty; // raw JSON from the AI provider, for audit/debug

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Policy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<PolicyRule> Rules { get; set; } = new List<PolicyRule>();
}

public class PolicyRule
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public Policy Policy { get; set; } = null!;

    public string Condition { get; set; } = string.Empty; // simple stored condition, evaluated in C# code
    public string Action { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
}

public class TicketAction
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Approval
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
