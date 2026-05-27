using Itm.Store.Mobile.Services;

namespace Itm.Store.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Completa todos los campos.");
            return;
        }

        SetLoading(true);

        try
        {
            var result = await _api.LoginAsync(email, password);

            if (result is null)
            {
                // ── MODO DEMO ──────────────────────────────────────────────
                // Si no tienes Auth.Api, usamos credenciales de prueba.
                if (email == "admin@itm.edu" && password == "1234")
                {
                    // JWT demo compatible con la clave del Gateway
                    const string demoToken =
                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                        "eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkBpdG0uZWR1Iiw" +
                        "icm9sZSI6IkFkbWluaXN0cmFkb3IiLCJleHAiOjE5OTk5OTk5OTl9." +
                        "DEMO_SIGNATURE";

                    _api.SetToken(demoToken);
                    await NavigateToMain();
                    return;
                }

                ShowError("Credenciales incorrectas.");
                return;
            }

            _api.SetToken(result.Token);
            await NavigateToMain();
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

    private async Task NavigateToMain()
    {
        Application.Current!.MainPage = new AppShell();
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        LoginButton.IsEnabled = !loading;
    }
}