using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/enrollments"), Authorize]
public class EnrollmentsController(RaceDayContext db) : ControllerBase
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enroll(EnrollRequest r)
    {
        var c = await db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.CategoryID == r.CategoryID);
        if (c is null) return NotFound("Category not found.");
        if (c.Event!.Status != "Open") return BadRequest("This event is not open for enrolment.");
        if (await db.EventEnrollments.AnyAsync(x => x.ParticipantID == UserId && x.CategoryID == r.CategoryID))
            return Conflict("Already enrolled in this category.");
        var e = new EventEnrollment { ParticipantID = UserId, CategoryID = r.CategoryID, EnrollmentStatus = "Confirmed" };
        db.EventEnrollments.Add(e); await db.SaveChangesAsync();
        return Ok(new { e.EnrollmentID, e.ParticipantID, e.CategoryID, e.EnrollmentDate, e.EnrollmentStatus });
    }

    [HttpGet("mine"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine() =>
        Ok(await db.EventEnrollments.Where(x => x.ParticipantID == UserId)
            .Select(x => new {
                x.EnrollmentID, x.CategoryID, Category = x.Category!.CategoryName,
                Event = x.Category.Event!.EventName,
                EventID = x.Category.Event.EventID,
                EventDate = x.Category.Event.EventDate,
                EventStatus = x.Category.Event.Status,
                x.EnrollmentStatus, x.BibNumber
            }).ToListAsync());

    [HttpGet("event/{eventId:int}"), Authorize(Roles = "Organizer")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(x => x.EventID == eventId && x.OrganizerID == UserId)) return Forbid();
        return Ok(await db.EventEnrollments.Where(x => x.Category!.EventID == eventId)
            .Select(x => new { x.EnrollmentID, x.ParticipantID, Participant = x.Participant!.FirstName + " " + x.Participant.LastName,
                Category = x.Category!.CategoryName, x.EnrollmentStatus, x.BibNumber }).ToListAsync());
    }
}
