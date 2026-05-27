namespace Itm.Search.Api.Models;

public class TicketSearchDoc
{
    public int Id { get; set; }

    public string ArtistName { get; set; } = string.Empty;

    public string Venue { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;
}