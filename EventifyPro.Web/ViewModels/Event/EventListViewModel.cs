namespace EventifyPro.Web.ViewModels.Event;

public class EventListViewModel
{
    public EventSearchViewModel Filter { get; set; } = new();

    public IReadOnlyList<EventSummaryViewModel> Events { get; set; } = [];

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }
}
