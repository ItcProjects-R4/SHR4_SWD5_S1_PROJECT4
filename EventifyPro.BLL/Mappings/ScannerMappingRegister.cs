namespace EventifyPro.BLL.Mappings;

public sealed class ScannerMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ScanLog, ScanLogResponseDto>()
            .Map(dest => dest.ScannerName, src => src.Scanner != null ? src.Scanner.FullName : string.Empty)
            .Map(dest => dest.ScanResult, src => src.Result.ToString())
            .Map(dest => dest.RawQRData, src => src.RawQRCode)
            .Map(dest => dest.EventTitle, src => src.ScanEvent != null ? src.ScanEvent.Title : string.Empty)
            .Map(dest => dest.AttendeeName, src => src.Ticket != null && src.Ticket.Booking != null && src.Ticket.Booking.User != null ? src.Ticket.Booking.User.FullName : string.Empty)
            .Map(dest => dest.Notes, src => src.Notes);
    }
}
