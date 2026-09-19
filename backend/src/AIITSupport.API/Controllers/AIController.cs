using System.Security.Claims;
using AIITSupport.Application.Exceptions;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.API.Controllers;

[ApiController]
[Route("api/tickets/{id}")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IWorkflowService _workflow;
    private readonly ITicketService _ticketService;

    public AIController(IWorkflowService workflow, ITicketService ticketService)
    {
        _workflow = workflow;
        _ticketService = ticketService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAgentOrAdmin => User.IsInRole(RoleNames.SupportAgent) || User.IsInRole(RoleNames.Admin);

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(int id)
    {
        // Reuse the same ownership rule as ticket viewing: the owner or an
        // agent/admin can trigger analysis, nobody else even learns the ticket exists.
        var ticket = await _ticketService.GetByIdAsync(id, CurrentUserId, IsAgentOrAdmin);
        if (ticket == null) return NotFound();

        try
        {
            var result = await _workflow.AnalyzeAsync(id, CurrentUserId);
            return Ok(result);
        }
        catch (AIProviderException ex)
        {
            // AI is down/unreachable after retries - ticket is already marked
            // AIProcessingFailed and routed for human attention.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (AIResponseInvalidException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
        catch (InvalidTicketStateException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("analysis")]
    public async Task<IActionResult> GetAnalysis(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id, CurrentUserId, IsAgentOrAdmin);
        if (ticket == null) return NotFound();

        var analysis = await _workflow.GetLatestAnalysisAsync(id);
        if (analysis == null) return NotFound(new { message = "No AI analysis yet for this ticket." });

        return Ok(analysis);
    }
}
