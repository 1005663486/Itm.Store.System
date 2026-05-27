using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// ======================================
// JWT SECURITY
// ======================================

var secretKey =
Encoding.UTF8.GetBytes(
"ITM-Super-Secret-Key-For-JWT-Class-2026-Nivel5");

builder.Services
.AddAuthentication(
JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "ItmIdentityServer",

        ValidateAudience = true,
        ValidAudience = "ItmStoreApis",

        ValidateLifetime = false,

        ValidateIssuerSigningKey = true,

        IssuerSigningKey =
        new SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddAuthorization();


// ======================================
// RATE LIMITING
// ======================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
    StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
            httpContext.Connection
            .RemoteIpAddress?
            .ToString(),

            factory: _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            }));
});


// ======================================
// YARP
// ======================================

builder.Services
.AddReverseProxy()
.LoadFromConfig(
builder.Configuration
.GetSection("ReverseProxy"));

var app = builder.Build();


// ======================================
// MIDDLEWARE
// ======================================

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();


// ======================================
// YARP
// ======================================

app.MapReverseProxy()
.RequireAuthorization()
.RequireRateLimiting("fixed");

app.Run();