using Itm.Store.Mobile.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Itm.Store.Mobile.Services;

/// <summary>
/// Servicio centralizado que encapsula toda comunicación con el backend.
/// El Gateway (YARP) es el único punto de entrada — todas las URLs apuntan a él.
/// </summary>
public class ApiService
{
    // Cambia esta URL por la del Gateway en producción (Kubernetes Ingress)
    private const string GatewayBase = "http://localhost:5183";

    // URLs directas en dev (evita rate-limit del Gateway durante pruebas)
    private const string OrderBase = "http://localhost:5110";
    private const string SearchBase = "http://localhost:5062";
    private const string PriceBase = "http://localhost:5022";

    private readonly HttpClient _http;
    private string? _jwtToken;

    public ApiService()
    {
        // Ignoramos certificados auto-firmados en dev
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _http = new HttpClient(handler);
    }

    // ─── TOKEN ────────────────────────────────────────────────────────────────
    public void SetToken(string token)
    {
        _jwtToken = token;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken);

    // ─── AUTH ─────────────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{GatewayBase}/api/auth/login",
                new LoginRequest(email, password));

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LoginResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // ─── SEARCH ───────────────────────────────────────────────────────────────
    public async Task<SearchResult?> SearchEventsAsync(string query)
    {
        var response = await _http.GetAsync(
            $"{SearchBase}/api/search?query={Uri.EscapeDataString(query)}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SearchResult>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // ─── PRICES ───────────────────────────────────────────────────────────────
    public async Task<PriceApiResponse?> GetPriceAsync(int eventId)
    {
        var response = await _http.GetAsync($"{PriceBase}/api/prices/{eventId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<PriceApiResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // ─── ORDERS ───────────────────────────────────────────────────────────────
    public async Task<(bool Success, OrderResponse? Order, string Error)> CreateOrderAsync(
        int eventId, int quantity, string city)
    {
        var response = await _http.PostAsJsonAsync(
            $"{OrderBase}/api/orders",
            new CreateOrderRequest(eventId, quantity, city));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (false, null, err);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (true, order, string.Empty);
    }
}