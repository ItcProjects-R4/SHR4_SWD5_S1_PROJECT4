namespace EventifyPro.BLL.Mappings;

public sealed class TicketMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketResponseDto>()
            .Map(dest => dest.TicketTypeName, src => src.TicketType != null ? src.TicketType.Name : string.Empty)
            .Map(dest => dest.UsedByName, src => src.Scanner == null ? null : src.Scanner.FullName)
            .Map(dest => dest.EventTitle, src => src.Event != null ? src.Event.Title : string.Empty)
            .Map(dest => dest.EventStartDate, src => src.Event != null ? src.Event.StartDate : DateTime.MinValue)
            .Map(dest => dest.EventEndDate, src => src.Event != null ? src.Event.EndDate : DateTime.MinValue)
            .Map(dest => dest.BookingStatus, src => src.Booking != null ? src.Booking.Status.ToString() : string.Empty);
    }
}
