using System.Security.Claims;
using AIITSupport.Application.DTOs;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize] // must be logged in for every action here
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    public TicketsController(ITicketService ticketService) => _ticketService = ticketService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAgentOrAdmin =>
        User.IsInRole(RoleNames.SupportAgent) || User.IsInRole(RoleNames.Admin);

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request)
    {
        var result = await _ticketService.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<TicketResponse>>> GetAll()
    {
        // Employees see only their own tickets; agents/admins see everything.
        var result = await _ticketService.GetForUserAsync(CurrentUserId, IsAgentOrAdmin);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketResponse>> GetById(int id)
    {
        var result = await _ticketService.GetByIdAsync(id, CurrentUserId, IsAgentOrAdmin);
        // Returning 404 (not 403) for someone else's ticket avoids leaking that the ID exists.
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TicketResponse>> Update(int id, UpdateTicketRequest request)
    {
        var result = await _ticketService.UpdateAsync(id, CurrentUserId, IsAgentOrAdmin, request);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
