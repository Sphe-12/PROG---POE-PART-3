using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var user = await _api.LoginAsync(model.Email, model.Password);
            StoreSession(user);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return GoToRoleHome(user.Role);
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _api.SendAsync<RegisterResult>(HttpMethod.Post, "/api/auth/register",
                new { model.FirstName, model.LastName, model.Email, model.Password, model.Role, model.PhoneNumber });
            var user = await _api.LoginAsync(model.Email, model.Password);
            StoreSession(user);
            return GoToRoleHome(user.Role);
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutApiAsync();
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void StoreSession(LoginResult user)
    {
        HttpContext.Session.SetInt32("UserId", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("Name", $"{user.FirstName} {user.LastName}");
    }

    private IActionResult GoToRoleHome(string role) =>
        role == "Organizer" ? RedirectToAction("Dashboard", "Organizer") : RedirectToAction("MyEnrolments", "Participant");
}
