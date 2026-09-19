using System.Security.Claims;
using AIITSupport.Web.Models;
using AIITSupport.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AIITSupport.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) => _api = api;

    // The JWT from the API is stored under this claim type inside the
    // browser's auth cookie, so every later request can pull it back out
    // and attach it as "Authorization: Bearer ..." when calling the API.
    public const string TokenClaimType = "api_token";

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, result, error) = await _api.LoginAsync(model.Email, model.Password);
        if (!success || result == null)
        {
            model.ErrorMessage = error ?? "Invalid email or password.";
            return View(model);
        }

        await SignInAsync(result.Name, result.Email, result.Token, result.Roles);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, result, error) = await _api.RegisterAsync(model.Name, model.Email, model.Password);
        if (!success || result == null)
        {
            model.ErrorMessage = error ?? "Could not register. Try a different email.";
            return View(model);
        }

        // New self-registrations are always Employee (same rule as the API) -
        // sign them straight in with the token the API just issued.
        await SignInAsync(result.Name, result.Email, result.Token, result.Roles);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(string name, string email, string token, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(TokenClaimType, token)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
