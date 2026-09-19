using AIITSupport.Domain.Entities;
using AIITSupport.Domain.Interfaces;
using AIITSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIITSupport.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdWithRolesAsync(int id) =>
        _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<List<string>> GetRoleNamesAsync(int userId) =>
        await _db.UserRoles.Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

    public async Task AddAsync(User user, string defaultRoleName)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == defaultRoleName)
            ?? throw new InvalidOperationException($"Role '{defaultRoleName}' not found. Did migrations run?");

        _db.Users.Add(user);
        // EF will insert the User first (FK), then the join row, because of
        // the navigation - so we attach the join entity to the tracked user.
        user.UserRoles.Add(new UserRole { User = user, Role = role });
    }


    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;
    public TicketRepository(AppDbContext db) => _db = db;

    public async Task<Ticket> AddAsync(Ticket ticket)
    {
        _db.Tickets.Add(ticket);
        return ticket;
    }

    public Task<List<Ticket>> GetAllAsync() =>
        _db.Tickets.Include(t => t.Category).Include(t => t.CreatedByUser)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();

    public Task<List<Ticket>> GetByUserAsync(int userId) =>
        _db.Tickets.Include(t => t.Category)
            .Where(t => t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();

    public Task<Ticket?> GetByIdAsync(int id) =>
        _db.Tickets.Include(t => t.Category).Include(t => t.CreatedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<TicketCategory?> GetCategoryByNameAsync(string name) =>
        _db.TicketCategories.FirstOrDefaultAsync(c => c.Name == name);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    // ---------- Phase 2 ----------

    public Task<Ticket?> GetByIdWithAnalysesAsync(int id) =>
        _db.Tickets.Include(t => t.Category).Include(t => t.CreatedByUser)
            .Include(t => t.AIAnalyses)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAnalysisAsync(AIAnalysis analysis)
    {
        _db.AIAnalyses.Add(analysis);
        await _db.SaveChangesAsync();
    }

    public Task<AIAnalysis?> GetLatestAnalysisAsync(int ticketId) =>
        _db.AIAnalyses.Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task AddActionAsync(TicketAction action)
    {
        _db.TicketActions.Add(action);
        await _db.SaveChangesAsync();
    }

    public async Task AddApprovalAsync(Approval approval)
    {
        _db.Approvals.Add(approval);
        await _db.SaveChangesAsync();
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;
    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log)
    {
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public Task<List<AuditLog>> GetRecentAsync(int take = 100) =>
        _db.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync();
}



