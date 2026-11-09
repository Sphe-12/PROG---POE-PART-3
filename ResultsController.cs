using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api"), Authorize]
public class ResultsController(RaceDayContext db) : ControllerBase
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("enrollments/{enrollmentId:int}/result"), Authorize(Roles = "Organizer")]
    public async Task<IActionResult> Record(int enrollmentId, ResultRequest r)
    {
        var en = await db.EventEnrollments.Include(x => x.Category).ThenInclude(x => x!.Event)
            .SingleOrDefaultAsync(x => x.EnrollmentID == enrollmentId);
        if (en is null) return NotFound();
        if (en.Category!.Event!.OrganizerID != UserId) return Forbid();
        if (r.FinishTimeSeconds is <= 0 || r.Position is <= 0 || r.ResultStatus is not ("Finished" or "DNF" or "Disqualified"))
            return BadRequest("Invalid result.");
        var old = await db.Results.SingleOrDefaultAsync(x => x.EnrollmentID == enrollmentId);
        if (old is null) { old = new RaceResult { EnrollmentID = enrollmentId }; db.Results.Add(old); }
        old.FinishTimeSeconds = r.FinishTimeSeconds; old.Position = r.Position; old.ResultStatus = r.ResultStatus;
        await db.SaveChangesAsync();
        return Ok(old);
    }

    [HttpGet("results/mine"), Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine() =>
        Ok(await db.Results.Where(r => r.Enrollment!.ParticipantID == UserId)
            .Select(r => new {
                r.ResultID, r.FinishTimeSeconds, r.Position, r.ResultStatus,
                Event = r.Enrollment!.Category!.Event!.EventName,
                EventID = r.Enrollment.Category.Event.EventID,
                EventDate = r.Enrollment.Category.Event.EventDate,
                Category = r.Enrollment.Category.CategoryName,
                TotalFinishers = db.Results.Count(x => x.Enrollment!.Category!.EventID == r.Enrollment.Category.Event.EventID
                    && x.ResultStatus == "Finished")
            }).ToListAsync());
}
