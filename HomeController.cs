using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? status, string? search, string? type)
    {
        var all = await _api.GetAsync<List<EventDto>>("/api/events") ?? new();
        var query = all.Where(e => e.EventDate >= DateTime.Today).OrderBy(e => e.EventDate);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.EventDate);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.EventDate);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (e.Location ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.EventDate);
        var vm = new EventListViewModel
        {
            Events = query.ToList(),
            Status = status, Search = search, Type = type,
            AllTypes = all.Select(e => e.EventType).Where(t => !string.IsNullOrEmpty(t)).Cast<string>().Distinct().ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDetailDto>($"/api/events/{id}");
        if (ev is null) return NotFound();
        var categories = await _api.GetAsync<List<CategoryDto>>($"/api/events/{id}/categories") ?? new();
        var enrolled = false;
        if (User.IsInRole("Participant"))
        {
            var mine = await _api.GetAsync<List<EnrolmentDto>>("/api/enrollments/mine") ?? new();
            enrolled = mine.Any(m => m.EventID == id);
        }
        return View(new EventDetailsViewModel { Event = ev, Categories = categories, AlreadyEnrolled = enrolled });
    }

    [Route("Home/EventImage/{id:int}")]
    public async Task<IActionResult> EventImage(int id)
    {
        var img = await _api.GetImageAsync($"/api/events/{id}/banner");
        return img is null ? NotFound() : File(img.Value.Data, img.Value.ContentType);
    }

    public IActionResult Error()
    {
        var msg = HttpContext.Session.GetString("ErrorMessage");
        HttpContext.Session.Remove("ErrorMessage");
        return View(new ErrorViewModel { Message = msg ?? "Something went wrong. Please try again." });
    }
}
