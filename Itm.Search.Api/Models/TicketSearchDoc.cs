namespace Itm.Search.Api.Models;

public record TicketSearchDoc(
    string Id, // En Elastic los Ids suelen ser strings
    string ArtistName,
    string Venue,
    DateTime EventDate,
    decimal Price);