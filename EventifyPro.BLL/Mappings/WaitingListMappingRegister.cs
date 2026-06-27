namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class WaitingListMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<WaitingListJoinDto, WaitingList>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.NotifiedAt)
            .Ignore(dest => dest.ExpiresAt)
            .Ignore(dest => dest.Event)
            .Ignore(dest => dest.TicketType)
            .Ignore(dest => dest.User)
            .Map(dest => dest.Status, _ => WaitingListStatus.Waiting)
            .Map(dest => dest.JoinedAt, _ => DateTime.UtcNow);

        config.NewConfig<WaitingList, WaitingListResponseDto>()
            .Map(dest => dest.EventTitle, src => src.Event != null ? src.Event.Title : string.Empty)
            .Map(dest => dest.TicketTypeName, src => src.TicketType != null ? src.TicketType.Name : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CreatedAt, src => src.JoinedAt);
    }
}

#pragma warning restore CS8603
