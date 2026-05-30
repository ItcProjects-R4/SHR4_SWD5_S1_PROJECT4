namespace Eventify.BLL.DTOs.EventDTOs;

public record EventListDto
(
    int Id,
    string Title,
    DateTime StartDate,
    string City,
    string? ImageUrl
);