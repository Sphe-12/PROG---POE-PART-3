using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Organizer")]
public class OrganizerController : Controller
{
    private readonly ApiClient _api;
    public OrganizerController(ApiClient api) => _api = api;

    public async Task<IActionResult> Dashboard()
    {
        var mine = await _api.GetAsync<List<OrganizerEventSummary>>("/api/events/mine") ?? new();
        var ordered = mine.OrderBy(e => e.EventDate).ToList();
        return View(new OrganizerDashboardViewModel
        {
            Events = ordered,
            UpcomingCount = ordered.Count(e => e.EventDate >= DateTime.Today),
            TotalEnrolments = ordered.Sum(e => e.EnrolmentCount)
        });
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);
        try
        {
            var created = await _api.SendAsync<EventDetailDto>(HttpMethod.Post, "/api/events", BuildRequest(model));
            if (model.Banner is { Length: > 0 } && created is not null)
                await _api.PostFileAsync<object>($"/api/events/{created.EventID}/banner", model.Banner);
            TempData["Success"] = "Event created.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("EventForm", model); }
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        return View("EventForm", new EventFormViewModel
        {
            EventID = ev.EventID, EventName = ev.EventName, Description = ev.Description, EventDate = ev.EventDate,
            Location = ev.Location, Status = ev.Status, EventType = ev.EventType, DistanceKM = ev.DistanceKM,
            HasBanner = ev.BannerBlobName is not null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);
        try
        {
            await _api.SendAsync(HttpMethod.Put, $"/api/events/{model.EventID}", BuildRequest(model));
            if (model.Banner is { Length: > 0 })
                await _api.PostFileAsync<object>($"/api/events/{model.EventID}/banner", model.Banner);
            TempData["Success"] = "Event updated.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (ApiException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("EventForm", model); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        try { await _api.SendAsync(HttpMethod.Delete, $"/api/events/{id}"); TempData["Success"] = "Event deleted."; }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Dashboard));
    }

    private static object BuildRequest(EventFormViewModel m) =>
        new { m.EventName, m.Description, m.EventDate, m.Location, m.Status, m.EventType, m.DistanceKM };

    public async Task<IActionResult> Categories(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        var categories = await _api.GetAsync<List<CategoryDto>>($"/api/events/{id}/categories") ?? new();
        return View(new CategoriesViewModel { Event = ev, Categories = categories, Form = new CategoryFormViewModel { EventId = id } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel form)
    {
        try
        {
            await _api.SendAsync<object>(HttpMethod.Post, $"/api/events/{form.EventId}/categories",
                new { form.CategoryName, form.DistanceKM, form.MinimumAge, form.MaximumAge, form.EntryFee });
            TempData["Success"] = "Category added.";
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Categories), new { id = form.EventId });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        var list = await _api.GetAsync<List<OrganizerEnrolmentDto>>($"/api/enrollments/event/{id}") ?? new();
        return View(new EnrolmentsViewModel { Event = ev, Enrolments = list });
    }

    public async Task<IActionResult> Results(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        var list = await _api.GetAsync<List<OrganizerEnrolmentDto>>($"/api/enrollments/event/{id}") ?? new();
        return View(new ResultsEntryViewModel { Event = ev, Enrolments = list });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(int eventId, int enrollmentId, int? finishTimeSeconds, int? position, string resultStatus)
    {
        try
        {
            await _api.SendAsync<object>(HttpMethod.Post, $"/api/enrollments/{enrollmentId}/result",
                new { finishTimeSeconds, position, resultStatus });
            TempData["Success"] = "Result saved.";
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Results), new { id = eventId });
    }
}
