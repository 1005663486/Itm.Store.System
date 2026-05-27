using Microsoft.AspNetCore.SignalR.Client;

namespace Itm.Store.Mobile.Services;

/// <summary>
/// Encapsula la conexión SignalR al TicketHub de Order.Api.
/// Cuando el backend publica "TicketReady", dispara el evento TicketReceived para la UI.
/// </summary>
public class SignalRService : IAsyncDisposable
{
    // Ajusta el puerto al de tu Order.Api
    private const string HubUrl = "http://localhost:5110/ticketHub";

    private HubConnection? _connection;

    /// <summary>Evento que se dispara cuando llega una notificación de boleta lista.</summary>
    public event Action<string, string>? TicketReceived;

    // ─── CONEXIÓN ─────────────────────────────────────────────────────────────
    public async Task ConnectAsync()
    {
        if (_connection is not null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
            })
            .WithAutomaticReconnect()
            .Build();

        // Escuchar el evento "TicketReady" que emite Order.Api
        // hub.Clients.All.SendAsync("TicketReady", ticketCode, city)
        _connection.On<string, string>("TicketReady", (ticketCode, city) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                TicketReceived?.Invoke(ticketCode, city));
        });

        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"[SignalR] Reconectando... {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"[SignalR] Reconectado. ID: {connectionId}");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
        Console.WriteLine("[SignalR] Conectado al TicketHub");
    }

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    // ─── DESCONEXIÓN ──────────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}