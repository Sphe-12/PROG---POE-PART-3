using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api"), Authorize]
public class UsersController(RaceDayContext db, IBlobStorage blobs) : ControllerBase
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    const long MaxPictureBytes = 5 * 1024 * 1024;

    [HttpGet("profile/me")]
    public async Task<IActionResult> Get() =>
        Ok(await db.Users.Where(x => x.UserID == UserId)
            .Select(x => new { x.UserID, x.FirstName, x.LastName, x.Email, x.PhoneNumber, x.CreatedAt,
                HasProfilePicture = x.ProfilePictureBlobName != null }).SingleAsync());

    [HttpPut("profile/me")]
    public async Task<IActionResult> Update(ProfileUpdateRequest r)
    {
        var u = await db.Users.FindAsync(UserId);
        if (u is null) return NotFound();
        u.FirstName = r.FirstName.Trim(); u.LastName = r.LastName.Trim(); u.PhoneNumber = r.PhoneNumber;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("profile/me/picture")]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        var u = await db.Users.FindAsync(UserId);
        if (u is null) return NotFound();
        if (file is null || file.Length == 0) return BadRequest("Please choose an image file.");
        if (file.Length > MaxPictureBytes) return BadRequest("Image must be 5 MB or smaller.");
        var contentType = file.ContentType.ToLowerInvariant();
        if (!contentType.StartsWith("image/")) return BadRequest("Only image files are allowed.");
        var blobName = $"{UserId}-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        if (u.ProfilePictureBlobName is not null)
            await blobs.DeleteAsync(BlobContainers.ProfilePictures, u.ProfilePictureBlobName);
        await blobs.UploadAsync(BlobContainers.ProfilePictures, blobName, file.OpenReadStream(), contentType);
        u.ProfilePictureBlobName = blobName; u.ProfilePictureContentType = contentType;
        await db.SaveChangesAsync();
        return Ok(new { u.UserID, u.ProfilePictureBlobName });
    }

    [HttpGet("users/{userId:int}/picture"), AllowAnonymous]
    public async Task<IActionResult> Picture(int userId)
    {
        var u = await db.Users.FindAsync(userId);
        if (u?.ProfilePictureBlobName is null) return NotFound();
        var blob = await blobs.DownloadAsync(BlobContainers.ProfilePictures, u.ProfilePictureBlobName);
        return blob is null ? NotFound() : File(blob.Data, blob.ContentType);
    }
}
