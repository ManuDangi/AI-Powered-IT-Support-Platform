using AIITSupport.Web.Models;
using AIITSupport.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.Web.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApiClient _api;
    public TicketsController(ApiClient api) => _api = api;

    private string Token => User.FindFirst(AccountController.TokenClaimType)?.Value ?? string.Empty;
    private bool IsAgentOrAdmin => User.IsInRole("SupportAgent") || User.IsInRole("Admin");

    // Employees see this as "My Tickets"; agents/admins see every ticket -
    // same visibility rule the API itself already enforces, the view label
    // just adapts to who's looking.
    public async Task<IActionResult> Index()
    {
        var tickets = await _api.GetTicketsAsync(Token);
        ViewBag.IsAgentOrAdmin = IsAgentOrAdmin;
        return View(tickets.OrderByDescending(t => t.CreatedAt).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateTicketViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, result, error) = await _api.CreateTicketAsync(Token, model.Title, model.Description);
        if (!success || result == null)
        {
            model.ErrorMessage = error ?? "Could not create ticket.";
            return View(model);
        }

        TempData["Success"] = $"Ticket {result.TicketNumber} created.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _api.GetTicketAsync(Token, id);
        if (ticket == null) return NotFound();

        var analysis = await _api.GetAnalysisAsync(Token, id);

        ViewBag.Analysis = analysis;
        ViewBag.IsAgentOrAdmin = IsAgentOrAdmin;
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(int id)
    {
        var (success, _, error) = await _api.AnalyzeAsync(Token, id);
        TempData[success ? "Success" : "Error"] = success
            ? "AI analysis complete."
            : $"AI analysis failed: {error}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> Process(int id)
    {
        var (success, result, error) = await _api.ProcessAsync(Token, id);
        TempData[success ? "Success" : "Error"] = success
            ? $"Policy decision: {(result!.RequiresHumanReview ? "Human review required" : "Auto-resolved")} - {result.PolicyReason}"
            : $"Policy check failed: {error}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> Approve(int id, string? comments)
    {
        var (success, error) = await _api.ApproveAsync(Token, id, comments);
        TempData[success ? "Success" : "Error"] = success ? "Ticket approved and resolved." : $"Approve failed: {error}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> Reject(int id, string? comments)
    {
        var (success, error) = await _api.RejectAsync(Token, id, comments);
        TempData[success ? "Success" : "Error"] = success ? "Ticket rejected." : $"Reject failed: {error}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Escalate(int id, string reason)
    {
        var (success, error) = await _api.EscalateAsync(Token, id, string.IsNullOrWhiteSpace(reason) ? "Escalated by user." : reason);
        TempData[success ? "Success" : "Error"] = success ? "Ticket escalated." : $"Escalate failed: {error}";
        return RedirectToAction(nameof(Details), new { id });
    }
}
