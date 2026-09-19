using AIITSupport.Application.DTOs;
using AIITSupport.Domain.Enums;
using AIITSupport.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleNames.Admin)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogs;
    public AuditLogsController(IAuditLogRepository auditLogs) => _auditLogs = auditLogs;

    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponse>>> GetRecent([FromQuery] int take = 100)
    {
        var logs = await _auditLogs.GetRecentAsync(Math.Clamp(take, 1, 500));
        var response = logs.Select(l => new AuditLogResponse(
            l.Id, l.UserId, l.TicketId, l.Action, l.Details, l.CreatedAt));
        return Ok(response);
    }
}
