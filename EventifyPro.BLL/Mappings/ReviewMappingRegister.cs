namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class ReviewMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ReviewCreateDto, Review>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.IsHidden)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Event)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow);

        config.NewConfig<Review, ReviewResponseDto>()
            .Map(dest => dest.UserName, src => src.User != null ? src.User.FullName : string.Empty)
            .Map(dest => dest.EventTitle, src => src.Event != null ? src.Event.Title : string.Empty)
            .Map(dest => dest.OrganizerId, src => src.Event != null ? src.Event.OrganizerId : string.Empty);
    }
}

#pragma warning restore CS8603
