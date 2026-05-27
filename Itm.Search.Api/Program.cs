using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Itm.Search.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ======================================
// ELASTICSEARCH
// ======================================

var settings =
    new ElasticsearchClientSettings(
        new Uri("http://localhost:9200"))
    .DefaultIndex("tickets");

builder.Services.AddSingleton(
    new ElasticsearchClient(settings));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ======================================
// CREAR INDICE
// ======================================

app.MapPost(
"/api/search/create-index",
async (
ElasticsearchClient client) =>
{
    var exists =
        await client.Indices.ExistsAsync("tickets");

    if (exists.Exists)
    {
        return Results.Ok(new
        {
            Message = "El índice ya existe"
        });
    }

    var response =
        await client.Indices.CreateAsync(
        "tickets",
        c => c
            .Mappings(m => m
                .Properties<TicketSearchDoc>(p => p
                    .Keyword(k => k.Id)

                    .Text(t => t.ArtistName)

                    .Text(t => t.EventName)

                    .Text(t => t.City)

                    .Text(t => t.Venue)
                )));

    if (!response.IsValidResponse)
    {
        return Results.Problem(
     response.DebugInformation);
    }

    return Results.Ok(new
    {
        Message = "Índice creado correctamente"
    });
});


// ======================================
// CARGAR DATOS DEMO
// ======================================

app.MapPost(
"/api/search/load",
async (
ElasticsearchClient client) =>
{
    var docs =
    new List<TicketSearchDoc>
    {
        new()
        {
            Id = 1,
            ArtistName = "Karol G",
            Venue = "Atanasio Girardot",
            City = "Medellin",
            EventName = "Festival Medellin"
        },

        new()
        {
            Id = 2,
            ArtistName = "Feid",
            Venue = "Movistar Arena",
            City = "Bogota",
            EventName = "Ferxxo Tour"
        },

        new()
        {
            Id = 3,
            ArtistName = "Bad Bunny",
            Venue = "Wizink Center",
            City = "Madrid",
            EventName = "World Tour"
        },

        new()
        {
            Id = 4,
            ArtistName = "Jazz Night",
            Venue = "Blue Club",
            City = "Madrid",
            EventName = "Relax Session"
        }
    };

    foreach (var item in docs)
    {
        var indexResponse =
            await client.IndexAsync(
            item,
            i => i
                .Index("tickets")
                .Id(item.Id));

        if (!indexResponse.IsValidResponse)
        {
            return Results.Problem(
                $"Error indexando documento {item.Id}");
        }
    }

    // IMPORTANTE
    await client.Indices.RefreshAsync("tickets");

    return Results.Ok(new
    {
        Message = "Datos cargados",
        Count = docs.Count
    });
});


// ======================================
// BUSQUEDA
// ======================================

app.MapGet(
"/api/search",
async (
string query,
ElasticsearchClient client) =>
{
    var response =
        await client.SearchAsync<TicketSearchDoc>(
        s => s
            .Index("tickets")
            .Size(20)
            .Query(q =>
                q.MultiMatch(
                    new MultiMatchQuery
                    {
                        Query = query,

                        Fields = new[]
                        {
                            "artistName^3",
                            "eventName^2",
                            "venue",
                            "city"
                        },

                        Fuzziness =
                            new Fuzziness("AUTO")
                    }))
        );

    if (!response.IsValidResponse)
    {
        return Results.Problem(
            "Error consultando Elasticsearch");
    }

    return Results.Ok(new
    {
        Total =
            response.Documents.Count(),

        Results =
            response.Documents
    });
});

app.Run();