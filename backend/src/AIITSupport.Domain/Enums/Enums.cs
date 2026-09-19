namespace AIITSupport.Domain.Enums;

public enum TicketStatus
{
    New = 0,
    AIProcessing = 1,
    AIAnalyzed = 2,
    AIProcessingFailed = 3,
    PolicyChecked = 4,
    HumanReview = 5,
    AutoApproved = 6,
    Resolved = 7,
    Escalated = 8,
    Rejected = 9
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ApprovalDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

// Well known role names. Roles are still stored in the Roles table,
// this enum is just a convenient constant list used across the app.
public static class RoleNames
{
    public const string Employee = "Employee";
    public const string SupportAgent = "SupportAgent";
    public const string Admin = "Admin";
}
