namespace EventifyPro.BLL.Mappings;

public sealed class SavedEventMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SavedEvent, SavedEventDto>()
            .Map(dest => dest.EventTitle, src => src.Event.Title)
            .Map(dest => dest.EventLocation, src => src.Event.Location)
            .Map(dest => dest.EventCity, src => src.Event.City)
            .Map(dest => dest.EventImageUrl, src => src.Event.ImageUrl)
            .Map(dest => dest.EventStartDate, src => src.Event.StartDate)
            .Map(dest => dest.EventEndDate, src => src.Event.EndDate)
            .Map(dest => dest.EventCategoryName, src => src.Event.Category.Name)
            .Map(dest => dest.EventOrganizerName, src => src.Event.Organizer.FullName);
    }
}
