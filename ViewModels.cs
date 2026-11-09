using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Phone] public string? PhoneNumber { get; set; }
}

public class EventFormViewModel
{
    public int? EventID { get; set; }
    [Required, MaxLength(150)] public string EventName { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required, MaxLength(200)] public string Location { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string? EventType { get; set; }
    [Range(0, 1000)] public decimal? DistanceKM { get; set; }
    public IFormFile? Banner { get; set; }
    public bool HasBanner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    [Required] public string CategoryName { get; set; } = "";
    [Range(0.1, 1000)] public decimal DistanceKM { get; set; } = 5;
    [Range(0, 120)] public int MinimumAge { get; set; }
    [Range(0, 120)] public int? MaximumAge { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}

public class EnrolViewModel { public EventDetailDto Event { get; set; } = new(); public List<CategoryDto> Categories { get; set; } = new(); public int SelectedCategoryId { get; set; } }
public class EventListViewModel { public List<EventDto> Events { get; set; } = new(); public string? Status { get; set; } public string? Search { get; set; } public string? Type { get; set; } public List<string> AllTypes { get; set; } = new(); }
public class EventDetailsViewModel { public EventDetailDto Event { get; set; } = new(); public List<CategoryDto> Categories { get; set; } = new(); public bool AlreadyEnrolled { get; set; } }
public class OrganizerDashboardViewModel { public List<OrganizerEventSummary> Events { get; set; } = new(); public int UpcomingCount { get; set; } public int TotalEnrolments { get; set; } }
public class CategoriesViewModel { public EventDetailDto Event { get; set; } = new(); public List<CategoryDto> Categories { get; set; } = new(); public CategoryFormViewModel Form { get; set; } = new(); }
public class EnrolmentsViewModel { public EventDetailDto Event { get; set; } = new(); public List<OrganizerEnrolmentDto> Enrolments { get; set; } = new(); }
public class ResultsEntryViewModel { public EventDetailDto Event { get; set; } = new(); public List<OrganizerEnrolmentDto> Enrolments { get; set; } = new(); }

public class ProfileViewModel
{
    public string Email { get; set; } = "";
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Phone] public string? PhoneNumber { get; set; }
    public IFormFile? Picture { get; set; }
    public bool HasPicture { get; set; }
}

public class ErrorViewModel { public string? Message { get; set; } }

public static class DisplayFormatter
{
    public static string GetOrdinal(int n)
    {
        if (n <= 0) return n.ToString();
        var suffix = n % 100 is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n + suffix;
    }

    public static string FormatTime(int? seconds)
    {
        if (!seconds.HasValue || seconds.Value <= 0) return "-";
        var t = TimeSpan.FromSeconds(seconds.Value);
        return t.Hours > 0 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }
}
