using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args); // La creación del builder

// 🛡️ ESCUDO DE SEGURIDAD: RATE LIMITING (NIVEL 5)
builder.Services.AddRateLimiter(options =>
{
    // Si alguien abusa, respondemos con un 429 (Too Many Requests)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política de "Ventana Fija": 10 peticiones cada 10 segundos por cada IP
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0 // No hacemos fila; se rechaza de inmediato
            }));
});

//1. Agregamos YARP a la caja de herramientas (DI)
// Le decimos que lea la configuración del archivo appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRateLimiter(); // 1. Activamos el motor

//2. Activamos el middleware de YARP y le aplicamos la política de Rate Limiting
app.MapReverseProxy().RequireRateLimiting("fixed");

app.Run();