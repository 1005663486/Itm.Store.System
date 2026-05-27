namespace Itm.Store.Mobile.Models;

// --- AUTH ---
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Role);

// --- SEARCH ---
public record SearchResult(int Total, List<TicketDoc> Results);
public record TicketDoc(
    int Id,
    string ArtistName,
    string EventName,
    string City,
    string Venue
);

// --- ORDER ---
public record CreateOrderRequest(int EventId, int Quantity, string City);
public record OrderResponse(
    string Ticket,
    int EventId,
    int Quantity,
    string City,
    decimal UnitPrice,
    decimal Total,
    string Currency,
    string Status,
    string Message
);

// --- PRICE ---
public record PriceApiResponse(string Source, PriceData Data);
public record PriceData(int EventId, decimal TicketPrice, string Currency, string EventName);