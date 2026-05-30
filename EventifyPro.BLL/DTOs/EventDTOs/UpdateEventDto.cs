namespace Eventify.BLL.DTOs.EventDtos;

public record UpdateEventDto(
    int Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Location,
    string City
);