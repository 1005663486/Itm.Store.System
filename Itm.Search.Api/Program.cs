using Elastic.Clients.Elasticsearch;
using Itm.Search.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

// Configuramos el cliente apuntando a nuestro Docker local
var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("tickets"); // Índice por defecto

builder.Services.AddSingleton(new ElasticsearchClient(settings));

var app = builder.Build();

app.UseAuthorization();

// El Endpoint de Búsqueda "Google-Style"
app.MapGet("/api/search", async (string query, ElasticsearchClient client) =>
{
    // 🔍 Buscamos en el nombre del artista y el lugar del evento
    var response = await client.SearchAsync<TicketSearchDoc>(s => s
        .Index("tickets")
        .Query(q => q
            .MultiMatch(m => m
                .Query(query)
                .Fields(new[] { "artistName^2", "venue" })
                .Fuzziness(new Fuzziness("AUTO")) // Magia pura: tolera errores ortográficos
            )
        )
    );

    if (!response.IsValidResponse) return Results.Problem("Error en el motor de búsqueda");

    return Results.Ok(response.Documents);
});

app.Run();
