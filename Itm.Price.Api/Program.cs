using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
sp =>
ConnectionMultiplexer.Connect(
"localhost:6379")
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Base simulada (BD en memoria)
var pricesDb = new List<PriceDto>
{
    new(
        1,
        250000,
        "COP",
        "Festival Medellin"
    ),

    new(
        2,
        350000,
        "EUR",
        "Festival Madrid"
    )
};

app.MapGet(
"/api/prices/{eventId}",
async (
int eventId,
IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();

    var cacheKey =
    $"price:{eventId}";

    // =========================
    // BUSCAR EN REDIS
    // =========================
    var cached =
    await db.StringGetAsync(
        cacheKey);

    if (cached.HasValue)
    {
        // Convertir JSON guardado a objeto
        var price =
        JsonSerializer.Deserialize<PriceDto>(
            cached.ToString());

        return Results.Ok(
        new
        {
            Source = "Redis",
            Data = price
        });
    }

    // =========================
    // BUSCAR EN "BD"
    // =========================
    var priceDb =
    pricesDb.FirstOrDefault(
    x => x.EventId == eventId);

    if (priceDb is null)
    {
        return Results.NotFound(
        new
        {
            Message =
            "Evento no encontrado"
        });
    }

    // =========================
    // GUARDAR EN REDIS
    // =========================
    await db.StringSetAsync(
        cacheKey,

        JsonSerializer.Serialize(
            priceDb),

        TimeSpan.FromMinutes(5)
    );

    // Primera consulta
    return Results.Ok(
    new
    {
        Source = "Database",
        Data = priceDb
    });

});

app.Run();

public record PriceDto(
int EventId,
decimal TicketPrice,
string Currency,
string EventName
);