using Itm.Store.Mobile.Models;
using Itm.Store.Mobile.Services;

namespace Itm.Store.Mobile.Pages;

public partial class EventsPage : ContentPage
{
    private readonly ApiService _api;

    public EventsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    // ─── BÚSQUEDA ─────────────────────────────────────────────────────────────
    private async void OnSearchClicked(object sender, EventArgs e)
        => await DoSearch();

    private async Task DoSearch()
    {
        var query = SearchEntry.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        StatusLabel.Text = "Buscando...";
        StatusLabel.IsVisible = true;
        ResultsList.ItemsSource = null;

        try
        {
            var result = await _api.SearchEventsAsync(query);

            if (result is null || result.Total == 0)
            {
                StatusLabel.Text = "Sin resultados para esa búsqueda.";
                return;
            }

            ResultsList.ItemsSource = result.Results;
            StatusLabel.Text = $"{result.Total} evento(s) encontrado(s)";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    // ─── SELECCIÓN → navegar a compra ─────────────────────────────────────────
    private async void OnEventSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TicketDoc selected)
            return;

        ((CollectionView)sender).SelectedItem = null;

        await Navigation.PushAsync(new OrderPage(_api, selected));
    }
}