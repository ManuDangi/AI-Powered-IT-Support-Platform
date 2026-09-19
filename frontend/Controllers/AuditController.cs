using AIITSupport.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly ApiClient _api;
    public AuditController(ApiClient api) => _api = api;

    private string Token => User.FindFirst(AccountController.TokenClaimType)?.Value ?? string.Empty;

    public async Task<IActionResult> Index()
    {
        var logs = await _api.GetAuditLogsAsync(Token);
        return View(logs);
    }
}
