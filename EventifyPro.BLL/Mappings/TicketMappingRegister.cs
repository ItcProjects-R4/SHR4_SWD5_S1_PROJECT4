namespace EventifyPro.BLL.Mappings;

public sealed class TicketMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketResponseDto>()
            .Map(dest => dest.TicketTypeName, src => src.TicketType.Name)
            .Map(dest => dest.UsedByName, src => src.Scanner == null ? null : src.Scanner.FullName);
    }
}
