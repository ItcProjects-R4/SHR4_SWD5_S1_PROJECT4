namespace Eventify.BLL.DTOs.EventDtos;

public record CreateEventDto(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Location,
    string City
);