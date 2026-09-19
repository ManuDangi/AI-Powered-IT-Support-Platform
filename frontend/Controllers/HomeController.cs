using AIITSupport.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var token = User.FindFirst(AccountController.TokenClaimType)?.Value ?? string.Empty;
        var tickets = await _api.GetTicketsAsync(token);

        ViewBag.Total = tickets.Count;
        ViewBag.HumanReview = tickets.Count(t => t.Status == "HumanReview");
        ViewBag.Resolved = tickets.Count(t => t.Status == "Resolved");
        ViewBag.Escalated = tickets.Count(t => t.Status == "Escalated");

        return View(tickets.OrderByDescending(t => t.CreatedAt).Take(5).ToList());
    }

    public IActionResult Error() => View();
}
