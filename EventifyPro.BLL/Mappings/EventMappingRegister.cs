using DomainEvent = Eventify.Domain.Entities.Event;

namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class EventMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DomainEvent, EventSummaryDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.OrganizerName, src => src.Organizer != null ? src.Organizer.FullName : string.Empty)
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.MinTicketPrice, src => src.TicketTypes != null && src.TicketTypes.Any() ? src.TicketTypes.Min(t => t.Price) : 0m)
            .Map(dest => dest.MaxTicketPrice, src => src.TicketTypes != null && src.TicketTypes.Any() ? src.TicketTypes.Max(t => t.Price) : 0m)
            .Map(dest => dest.Location, src => src.Location);

        config.NewConfig<DomainEvent, EventDetailDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.OrganizerName, src => src.Organizer != null ? src.Organizer.FullName : string.Empty)
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.TotalBookings, src => src.Bookings != null ? src.Bookings.Count : 0)
            .Map(dest => dest.TotalTicketsSold, src => src.TicketTypes != null ? src.TicketTypes.Sum(ticketType => ticketType.SoldQuantity) : 0)
            .Map(dest => dest.TotalRevenue, src => src.Bookings != null ? src.Bookings.Sum(booking => booking.TotalAmount) : 0)
            .Map(dest => dest.AverageRating, src => src.Reviews != null && src.Reviews.Any()
                ? src.Reviews.Average(review => review.Rating)
                : 0);

        config.NewConfig<EventCreateDto, DomainEvent>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.OrganizerId)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Organizer)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.TicketTypes)
            .Ignore(dest => dest.Bookings)
            .Ignore(dest => dest.Reviews)
            .Ignore(dest => dest.Tickets)
            .Ignore(dest => dest.ScanLogs)
            .Ignore(dest => dest.ActualEventScanLogs)
            .Ignore(dest => dest.WaitingListEntries)
            .Map(dest => dest.Status, _ => EventStatus.PendingReview)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow);

        config.NewConfig<EventUpdateDto, DomainEvent>()
            .Ignore(dest => dest.OrganizerId)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Organizer)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.TicketTypes)
            .Ignore(dest => dest.Bookings)
            .Ignore(dest => dest.Reviews)
            .Ignore(dest => dest.Tickets)
            .Ignore(dest => dest.ScanLogs)
            .Ignore(dest => dest.ActualEventScanLogs)
            .Ignore(dest => dest.WaitingListEntries)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}

#pragma warning restore CS8603
