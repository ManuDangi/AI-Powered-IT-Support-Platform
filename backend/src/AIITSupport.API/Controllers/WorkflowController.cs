using System.Security.Claims;
using AIITSupport.Application.DTOs;
using AIITSupport.Application.Exceptions;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.API.Controllers;

[ApiController]
[Route("api/tickets/{id}")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflow;
    private readonly ITicketService _ticketService;

    public WorkflowController(IWorkflowService workflow, ITicketService ticketService)
    {
        _workflow = workflow;
        _ticketService = ticketService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAgentOrAdmin => User.IsInRole(RoleNames.SupportAgent) || User.IsInRole(RoleNames.Admin);

    // Running the policy engine is an internal workflow step, not something
    // an employee should trigger on their own ticket - agents/admins only.
    [HttpPost("process")]
    [Authorize(Roles = $"{RoleNames.SupportAgent},{RoleNames.Admin}")]
    public async Task<IActionResult> Process(int id)
    {
        try
        {
            var result = await _workflow.ProcessAsync(id, CurrentUserId);
            return Ok(result);
        }
        catch (InvalidTicketStateException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("approve")]
    [Authorize(Roles = $"{RoleNames.SupportAgent},{RoleNames.Admin}")]
    public async Task<IActionResult> Approve(int id, ApprovalRequest request)
    {
        try
        {
            var result = await _workflow.ApproveAsync(id, CurrentUserId, request.Comments);
            return Ok(result);
        }
        catch (InvalidTicketStateException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reject")]
    [Authorize(Roles = $"{RoleNames.SupportAgent},{RoleNames.Admin}")]
    public async Task<IActionResult> Reject(int id, RejectRequest request)
    {
        try
        {
            var result = await _workflow.RejectAsync(id, CurrentUserId, request.Comments);
            return Ok(result);
        }
        catch (InvalidTicketStateException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Escalate is allowed for the ticket owner too - e.g. an employee whose
    // ticket has been sitting untouched can push it up manually.
    [HttpPost("escalate")]
    public async Task<IActionResult> Escalate(int id, EscalateRequest request)
    {
        var ticket = await _ticketService.GetByIdAsync(id, CurrentUserId, IsAgentOrAdmin);
        if (ticket == null) return NotFound();

        try
        {
            var result = await _workflow.EscalateAsync(id, CurrentUserId, request.Reason);
            return Ok(result);
        }
        catch (InvalidTicketStateException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
