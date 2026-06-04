using EventifyPro.BLL.DTOs.TicketType;
using Mapster;

namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class TicketTypeMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TicketType, TicketTypeResponseDto>();

        config.NewConfig<TicketTypeCreateDto, TicketType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.SoldQuantity)
            .Ignore(dest => dest.RowVersion)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Event)
            .Ignore(dest => dest.BookingItems)
            .Ignore(dest => dest.Tickets)
            .Ignore(dest => dest.WaitingListEntries)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow);

        config.NewConfig<TicketTypeUpdateDto, TicketType>()
            .Ignore(dest => dest.EventId)
            .Ignore(dest => dest.SoldQuantity)
            .Ignore(dest => dest.RowVersion)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Event)
            .Ignore(dest => dest.BookingItems)
            .Ignore(dest => dest.Tickets)
            .Ignore(dest => dest.WaitingListEntries)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}

#pragma warning restore CS8603
