namespace Eventify.BLL.DTOs.EventDTOs;

public record EventDetailsDto
(
    int Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Location,
    string City,
    string? ImageUrl,
    byte Status,
    int? MaxCapacity,
    string OrganizerName,
    string CategoryName
);