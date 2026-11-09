namespace RaceDay.Web.Models;

public record LoginResult(int UserID, string FirstName, string LastName, string Email, string Role);
public record RegisterResult(int UserID, string FirstName, string LastName, string Email, string Role);
public record ProfileDto(int UserID, string FirstName, string LastName, string Email, string? PhoneNumber, DateTime CreatedAt, bool HasProfilePicture);
public record EventDto(int EventID, string EventName, string? Description, DateTime EventDate, string Location, string Status, string? EventType, decimal? DistanceKM, int OrganizerID, bool HasBanner, int EnrolmentCount = 0);
public record EventDetailDto(int EventID, int OrganizerID, string EventName, string? Description, DateTime EventDate, string Location, string Status, string? EventType, decimal? DistanceKM, string? BannerBlobName, string? BannerContentType);
public record CategoryDto(int CategoryID, int EventID, string CategoryName, decimal DistanceKM, int MinimumAge, int? MaximumAge, decimal EntryFee);
public record EnrolmentDto(int EnrollmentID, int CategoryID, string Category, string Event, int EventID, DateTime EventDate, string EventStatus, string EnrollmentStatus, string? BibNumber);
public record OrganizerEnrolmentDto(int EnrollmentID, int ParticipantID, string Participant, string Category, string EnrollmentStatus, string? BibNumber);
public record MyResultDto(int ResultID, int? FinishTimeSeconds, int? Position, string ResultStatus, string Event, int EventID, DateTime EventDate, string Category, int TotalFinishers);
public record OrganizerEventSummary(int EventID, string EventName, DateTime EventDate, string Location, string Status, string? EventType, decimal? DistanceKM, bool HasBanner, int EnrolmentCount);
