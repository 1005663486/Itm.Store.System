using Itm.Inventory.Api.Protos;
using Itm.Order.Api.Events;
using Itm.Order.Api.Handlers;
using Itm.Order.Api.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Http.Resilience;
using System.Net.Http.Json;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar configuración del cliente gRPC para InventoryService
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(o =>
{
    o.Address = new Uri("https://localhost:5273"); // Puerto HTTPS actual de Inventory.Api (ver launchSettings.json del proyecto Inventory)
});

// Necesario para leer encabezados de la petición HTTP entrante
builder.Services.AddHttpContextAccessor();

// Registramos el DelegatingHandler que propagará el X-Correlation-ID
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Registro de clientes HTTP hacia los otros microservicios
builder.Services
    .AddHttpClient("InventoryClient", client =>
    {
        // Puerto actual de Inventory.Api (ver launchSettings.json del proyecto Inventory)
        client.BaseAddress = new Uri("http://localhost:5273");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient("PriceClient", client =>
    {
        // TODO: Ajustar al puerto real de Price.Api cuando exista el proyecto
        client.BaseAddress = new Uri("http://localhost:5022");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddStandardResilienceHandler();

// Configuración del Productor
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        //Peguen aquí su AMQP URL DE CLOUDAMQP (Entre comillas dobles)
        // En un trabajo real, esto va en el KeyVault o en las variables de entorno, no hardcodeado
        cfg.Host("amqps://wqwoltap:bYzWB8MvzX891TQSA8YY5HB8ePSdLWda@turkey.rmq.cloudamqp.com/wqwoltap");
    });
});



// SIGNALR
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HUB
app.MapHub<TicketHub>(
"/ticketHub");

// Endpoint principal de compra de boletas usando SAGA (reserva + compensación)
app.MapPost("/api/orders", async (
    CreateOrderDto order,
    IHttpClientFactory factory,
    IPublishEndpoint publisher,
    IHubContext<TicketHub> hub) =>
{
    var invClient = factory.CreateClient("InventoryClient");
    var priceClient = factory.CreateClient("PriceClient");

    // PASO 1: Reservar boletas
    var reduceResponse = await invClient.PostAsJsonAsync(
        "/api/inventory/reduce",
        new
        {
            ProductId = order.EventId,
            Quantity = order.Quantity
        });

    if (!reduceResponse.IsSuccessStatusCode)
    {
        return Results.BadRequest(new
        {
            Message = "No hay boletas disponibles para este evento"
        });
    }
    // PASO 2: Consultar precio
    var priceResult =
await priceClient.GetFromJsonAsync<PriceApiResponse>(
$"/api/prices/{order.EventId}");

    if (
    priceResult is null ||
    priceResult.Data is null
    )
    {
        throw new Exception(
        "No fue posible obtener precio");
    }

    var total =
    priceResult.Data.TicketPrice *
    order.Quantity;

    var ticketCode = $"ITM-{Guid.NewGuid().ToString()[..8]}";

    try
    {
        // PASO 2: Simulación pago
        var random = new Random();
        var paymentSuccess = true;

        if (!paymentSuccess)
        {
            throw new InvalidOperationException(
                "Pago rechazado");
        }

        // Evento RabbitMQ
        try
        {
            await publisher.Publish(
                new TicketPurchased(
                    Guid.NewGuid(),
                    order.EventId,
                    order.Quantity,
                    order.City,
                    ticketCode));

            Console.WriteLine(
                "[Rabbit] Evento enviado correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Rabbit ERROR] {ex}");

            throw;
        }
        await hub.Clients.All.SendAsync(
                                        "TicketReady",
                                        ticketCode,
                                        order.City
                                        );
        Console.WriteLine( $"[SIGNALR] Ticket enviado: {ticketCode}"
);
        // Compra exitosa
        return Results.Ok(new
        {
            Ticket = ticketCode,
            EventId = order.EventId,
            Quantity = order.Quantity,
            City = order.City,
            UnitPrice = priceResult.Data.TicketPrice,
            Total = total,
            Currency = priceResult.Data.Currency,
            Status = "CONFIRMED",
            Message = "Boleta generada correctamente"
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[ERROR] Compra falló: {ex.Message}");

        // Compensación SAGA
        var compensateResponse =
            await invClient.PostAsJsonAsync(
            "/api/inventory/release",
            new
            {
                ProductId = order.EventId,
                Quantity = order.Quantity
            });

        if (compensateResponse.IsSuccessStatusCode)
        {
            return Results.Problem(
                "El pago falló. Las boletas fueron liberadas.");
        }

        Console.WriteLine(
            "[CRITICAL] Error compensando inventario");

        return Results.Problem(
            "Error crítico del sistema.");
    }
});
// === NUEVO ENDPOINT gRPC ===
// El Order.Api llama al servicio remoto como si fuera un método inyectado. Cero manejo manual de JSON.
app.MapPost("/api/orders/grpc", async (int productId, InventoryService.InventoryServiceClient client) =>
{
    // Realizamos la llamada gRPC de forma asíncrona
    var reply = await client.CheckStockAsync(new StockRequest { ProductId = productId });

    if (!reply.IsAvailable)
    {
        return Results.BadRequest($"Stock insuficiente. Solo quedan {reply.Stock} unidades.");
    }

    return Results.Ok("Orden validada por gRPC y procesada.");
});

app.Run();

// DTOs locales para orquestación
public record CreateOrderDto(
int EventId,
int Quantity,
string City
);

public record InventoryResponse(
int EventId,
int AvailableTickets,
string EventCode
);

public record PriceApiResponse(
string Source,
PriceResponse Data
);

public record PriceResponse(
int EventId,
decimal TicketPrice,
string Currency,
string EventName
);

// Simulación de DTO de Pago (para futuras extensiones de la SAGA)
public record PaymentDto(int OrderId, decimal Amount);

namespace Itm.Order.Api.Events
{
    public record TicketPurchased(
        Guid OrderId,
        int EventId,
        int Quantity,
        string City,
        string TicketCode
    );
}

