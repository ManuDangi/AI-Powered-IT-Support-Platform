using AIITSupport.Domain.Entities;

namespace AIITSupport.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdWithRolesAsync(int id);
    Task<List<string>> GetRoleNamesAsync(int userId);
    Task AddAsync(User user, string defaultRoleName);
    Task SaveChangesAsync();
}

public interface ITicketRepository
{
    Task<Ticket> AddAsync(Ticket ticket);
    Task<List<Ticket>> GetAllAsync();
    Task<List<Ticket>> GetByUserAsync(int userId);
    Task<Ticket?> GetByIdAsync(int id);
        Task<TicketCategory?> GetCategoryByNameAsync(string name);
Task SaveChangesAsync();

    // ---------- Phase 2: AI analysis / policy / approvals ----------
    Task<Ticket?> GetByIdWithAnalysesAsync(int id);
    Task AddAnalysisAsync(AIAnalysis analysis);
    Task<AIAnalysis?> GetLatestAnalysisAsync(int ticketId);
    Task AddActionAsync(TicketAction action);
    Task AddApprovalAsync(Approval approval);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetRecentAsync(int take = 100);
}



