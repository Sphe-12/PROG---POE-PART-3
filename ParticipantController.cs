using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;
    public ParticipantController(ApiClient api) => _api = api;

    public async Task<IActionResult> MyEnrolments()
    {
        var mine = await _api.GetAsync<List<EnrolmentDto>>("/api/enrollments/mine") ?? new();
        return View(mine.OrderByDescending(m => m.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Enrol(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        if (ev.Status != "Open")
        {
            TempData["Error"] = "This event is not open for enrolment.";
            return RedirectToAction("Details", "Home", new { id });
        }
        var categories = await _api.GetAsync<List<CategoryDto>>($"/api/events/{id}/categories") ?? new();
        return View(new EnrolViewModel { Event = ev, Categories = categories });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(EnrolViewModel model)
    {
        if (model.SelectedCategoryId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please choose a category.");
            return await Reload(model);
        }
        try
        {
            await _api.SendAsync<object>(HttpMethod.Post, "/api/enrollments", new { CategoryID = model.SelectedCategoryId });
            TempData["Success"] = "You are enrolled! See you on race day.";
            return RedirectToAction(nameof(MyEnrolments));
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return await Reload(model); }
    }

    private async Task<IActionResult> Reload(EnrolViewModel model)
    {
        model.Event = (await _api.GetAsync<EventDetailDto>($"/api/events/{model.Event.EventID}")) ?? model.Event;
        model.Categories = await _api.GetAsync<List<CategoryDto>>($"/api/events/{model.Event.EventID}/categories") ?? new();
        return View(model);
    }

    public async Task<IActionResult> MyResults()
    {
        var results = await _api.GetAsync<List<MyResultDto>>("/api/results/mine") ?? new();
        return View(results.OrderByDescending(r => r.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var p = await _api.GetAsync<ProfileDto>("/api/profile/me");
        if (p is null) return RedirectToAction("Login", "Account");
        return View(new ProfileViewModel
        {
            Email = p.Email, FirstName = p.FirstName, LastName = p.LastName,
            PhoneNumber = p.PhoneNumber, HasPicture = p.HasProfilePicture
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _api.SendAsync(HttpMethod.Put, "/api/profile/me",
                new { model.FirstName, model.LastName, model.PhoneNumber });
            if (model.Picture is { Length: > 0 })
                await _api.PostFileAsync<object>("/api/profile/me/picture", model.Picture);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet]
    public async Task<IActionResult> ProfileImage()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return NotFound();
        var img = await _api.GetImageAsync($"/api/users/{uid}/picture");
        return img is null ? NotFound() : File(img.Value.Data, img.Value.ContentType);
    }
}
