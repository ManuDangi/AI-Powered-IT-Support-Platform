using AIITSupport.Application.DTOs;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;
using AIITSupport.Domain.Enums;
using AIITSupport.Domain.Interfaces;

namespace AIITSupport.Application.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;

    public TicketService(ITicketRepository tickets)
    {
        _tickets = tickets;
    }

    public async Task<TicketResponse> CreateAsync(int userId, CreateTicketRequest request)
    {
        var ticket = new Ticket
        {
            TicketNumber = $"TCK-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Title = request.Title,
            Description = request.Description,
            CreatedByUserId = userId,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium
        };

        var created = await _tickets.AddAsync(ticket);
        await _tickets.SaveChangesAsync();
        return Map(created);
    }

    public async Task<List<TicketResponse>> GetForUserAsync(int userId, bool isAgentOrAdmin)
    {
        var list = isAgentOrAdmin
            ? await _tickets.GetAllAsync()
            : await _tickets.GetByUserAsync(userId);

        return list.Select(Map).ToList();
    }

    public async Task<TicketResponse?> GetByIdAsync(int ticketId, int userId, bool isAgentOrAdmin)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId);
        if (ticket == null) return null;

        // Ownership check: an Employee may only view their own ticket.
        if (!isAgentOrAdmin && ticket.CreatedByUserId != userId)
            return null;

        return Map(ticket);
    }

    public async Task<TicketResponse?> UpdateAsync(int ticketId, int userId, bool isAgentOrAdmin, UpdateTicketRequest request)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId);
        if (ticket == null) return null;

        if (!isAgentOrAdmin && ticket.CreatedByUserId != userId)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Title)) ticket.Title = request.Title;
        if (!string.IsNullOrWhiteSpace(request.Description)) ticket.Description = request.Description;

        // Only agents/admins are allowed to change status directly.
        if (isAgentOrAdmin && !string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TicketStatus>(request.Status, true, out var newStatus))
        {
            ticket.Status = newStatus;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await _tickets.SaveChangesAsync();
        return Map(ticket);
    }

    private static TicketResponse Map(Ticket t) => new(
        t.Id,
        t.TicketNumber,
        t.Title,
        t.Description,
        t.Status.ToString(),
        t.Priority.ToString(),
        t.Category?.Name,
        t.CreatedAt);
}
