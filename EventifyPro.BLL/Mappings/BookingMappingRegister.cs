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
            .Map(dest => dest.TicketTypeName, src => src.TicketType.Name);

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
            .Map(dest => dest.EventTitle, src => src.Event.Title);

        config.NewConfig<Booking, BookingDetailDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.EventTitle, src => src.Event.Title);
    }
}

#pragma warning restore CS8603
