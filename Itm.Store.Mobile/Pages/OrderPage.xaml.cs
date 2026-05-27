using Itm.Store.Mobile.Models;
using Itm.Store.Mobile.Services;

namespace Itm.Store.Mobile.Pages;

public partial class OrderPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly TicketDoc _event;

    private int _quantity = 1;
    private decimal _unitPrice = 0;
    private string _currency = "COP";

    public OrderPage(ApiService api, TicketDoc selectedEvent)
    {
        InitializeComponent();
        _api = api;
        _event = selectedEvent;
        _signalR = new SignalRService();

        _signalR.TicketReceived += OnTicketReceived;
    }

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ArtistLabel.Text = _event.ArtistName;
        EventLabel.Text = _event.EventName;
        CityLabel.Text = $"📍 {_event.City} — {_event.Venue}";

        await ConnectSignalRAsync();
        await LoadPriceAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _signalR.TicketReceived -= OnTicketReceived;
        await _signalR.DisposeAsync();
    }

    // ─── SIGNALR ──────────────────────────────────────────────────────────────
    private async Task ConnectSignalRAsync()
    {
        try
        {
            await _signalR.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] No se pudo conectar: {ex.Message}");
            // No bloqueamos la compra si SignalR falla
        }
    }

    /// <summary>
    /// Se ejecuta cuando Order.Api publica "TicketReady" vía SignalR.
    /// </summary>
    private void OnTicketReceived(string ticketCode, string city)
    {
        TicketCodeLabel.Text = ticketCode;
        TicketCityLabel.Text = $"Ciudad: {city}";
        ConfirmationPanel.IsVisible = true;

        ConfirmationPanel.Opacity = 0;
        ConfirmationPanel.FadeTo(1, 500);
    }

    // ─── PRECIO ───────────────────────────────────────────────────────────────
    private async Task LoadPriceAsync()
    {
        try
        {
            var priceResp = await _api.GetPriceAsync(_event.Id);
            if (priceResp?.Data is null) return;

            _unitPrice = priceResp.Data.TicketPrice;
            _currency = priceResp.Data.Currency;

            PriceLabel.Text = $"$ {_unitPrice:N0}";
            CurrencyLabel.Text = _currency;
            // "Redis" o "Database" — útil para mostrar en la demo
            PriceSourceLabel.Text = $"Fuente: {priceResp.Source}";

            UpdateTotal();
        }
        catch (Exception ex)
        {
            PriceLabel.Text = "Error al consultar precio";
            Console.WriteLine($"[Price] {ex.Message}");
        }
    }

    // ─── CANTIDAD ─────────────────────────────────────────────────────────────
    private void OnIncreaseClicked(object sender, EventArgs e)
    {
        if (_quantity >= 10) return;
        _quantity++;
        UpdateQuantityUI();
    }

    private void OnDecreaseClicked(object sender, EventArgs e)
    {
        if (_quantity <= 1) return;
        _quantity--;
        UpdateQuantityUI();
    }

    private void UpdateQuantityUI()
    {
        QuantityLabel.Text = _quantity.ToString();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        TotalLabel.Text = $"$ {_unitPrice * _quantity:N0} {_currency}";
    }

    // ─── COMPRA ───────────────────────────────────────────────────────────────
    private async void OnBuyClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        ConfirmationPanel.IsVisible = false;
        SetLoading(true);

        try
        {
            var (success, order, error) = await _api.CreateOrderAsync(
                _event.Id,
                _quantity,
                _event.City);

            if (!success)
            {
                ShowError($"Error en la compra: {error}");
                return;
            }

            // Si SignalR no está conectado, mostramos la confirmación manualmente
            if (order is not null && !_signalR.IsConnected)
            {
                OnTicketReceived(order.Ticket, order.City);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error de conexión: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────
    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        BuyButton.IsEnabled = !loading;
    }
}