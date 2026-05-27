using Itm.Product.Api.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese: Bearer {token}"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                    new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },

                Array.Empty<string>()
            }
        });
});

// Necesario para leer el Authorization que llega
builder.Services.AddHttpContextAccessor();

// Handler que reenvía el JWT hacia Inventory
builder.Services.AddTransient<AuthForwardingDelegatingHandler>();

// =====================================
// CLIENTE INVENTORY
// =====================================

builder.Services.AddHttpClient("InventoryClient", client =>
{
    // Puerto REAL donde corre Inventory
    client.BaseAddress = new Uri("http://localhost:5273");

    client.Timeout = TimeSpan.FromSeconds(5);

})
.AddHttpMessageHandler<AuthForwardingDelegatingHandler>()
.AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =====================================
// ENDPOINT ORQUESTADOR
// =====================================

app.MapGet(
"/api/products/{id}/check-stock",
async (
int id,
IHttpClientFactory factory
) =>
{
    var client =
    factory.CreateClient("InventoryClient");

    try
    {
        var stockData =
        await client.GetFromJsonAsync
        <InventoryResponse>(
        $"/api/inventory/{id}"
        );

        return Results.Ok(new
        {
            ProductId = id,

            MarketingName =
            "Super Laptop Gamer",

            StockInfo = stockData,

            Source =
            "Live from Microservice"
        });
    }

    catch (HttpRequestException ex)
    {
        return Results.Problem(
        $"El servicio de Inventario no responde. Detalle: {ex.Message}"
        );
    }
});

app.Run();

record InventoryResponse(
int ProductId,
int Stock,
string Sku
);

record ProductResponse(
int ProductId,
decimal Amount,
string Currency
);