namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class BookingMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BookingItemRequestDto, BookingItem>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.BookingId)
            .Ignore(dest => dest.UnitPrice)
            .Ignore(dest => dest.Booking)
            .Ignore(dest => dest.TicketType);

        config.NewConfig<BookingItem, BookingItemResponseDto>()
            .Map(dest => dest.TicketTypeName, src => src.TicketType != null ? src.TicketType.Name : string.Empty);

        config.NewConfig<BookingCreateDto, Booking>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.TotalAmount)
            .Ignore(dest => dest.BookingReference)
            .Ignore(dest => dest.CancellationReason)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Event)
            .Ignore(dest => dest.Tickets)
            .Ignore(dest => dest.Payment)
            .Ignore(dest => dest.Refunds)
            .Map(dest => dest.Status, _ => BookingStatus.Pending)
            .Map(dest => dest.BookingDate, _ => DateTime.UtcNow);

        config.NewConfig<Booking, BookingSummaryDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.EventTitle, src => src.Event != null ? src.Event.Title : string.Empty);

        config.NewConfig<Booking, BookingDetailDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.EventTitle, src => src.Event != null ? src.Event.Title : string.Empty)
            .Map(dest => dest.EventStartDate, src => src.Event != null ? src.Event.StartDate : DateTime.MinValue)
            .Map(dest => dest.EventEndDate, src => src.Event != null ? src.Event.EndDate : DateTime.MinValue)
            .Map(dest => dest.EventLocation, src => src.Event != null ? src.Event.Location : string.Empty)
            .Map(dest => dest.EventCity, src => src.Event != null ? src.Event.City : string.Empty)
            .Map(dest => dest.EventDescription, src => src.Event != null ? src.Event.Description : string.Empty)
            .Map(dest => dest.IsEventPassed, src => src.Event != null && src.Event.StartDate < DateTime.UtcNow)
            .Map(dest => dest.IsPaymentConfirmed, src => src.Payment != null && src.Payment.Status == Eventify.Domain.Enums.PaymentStatus.Completed)
            .Map(dest => dest.PaymentDate, src => src.Payment != null && src.Payment.Status == Eventify.Domain.Enums.PaymentStatus.Completed ? src.Payment.PaymentDate : (DateTime?)null)
            .Map(dest => dest.AreTicketsGenerated, src => src.Tickets != null && src.Tickets.Any())
            .Map(dest => dest.TicketsGeneratedDate, src => src.Tickets != null && src.Tickets.Any() ? src.Tickets.First().CreatedAt : (DateTime?)null)
            .Map(dest => dest.IsTicketScanned, src => src.Tickets != null && src.Tickets.Any(t => t.IsUsed))
            .Map(dest => dest.TicketScannedDate, src => src.Tickets != null && src.Tickets.Any(t => t.IsUsed) ? (DateTime?)src.Tickets.Where(t => t.IsUsed).OrderBy(t => t.UsedAt).Select(t => t.UsedAt).FirstOrDefault() : (DateTime?)null);
    }
}

#pragma warning restore CS8603
