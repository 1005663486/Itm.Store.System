using Microsoft.AspNetCore.SignalR;

namespace Itm.Order.Api.Hubs;

public class TicketHub : Hub
{
    public async Task SendTicketReady(
        string ticketCode,
        string city)
    {
        await Clients.All.SendAsync(
            "TicketReady",
            ticketCode,
            city
        );
    }
}