using Itm.Store.Mobile.Pages;
using Itm.Store.Mobile.Services;

namespace Itm.Store.Mobile;

public partial class App : Application
{
    private readonly ApiService _api;

    public App(ApiService api)
    {
        InitializeComponent();
        _api = api;

        // La app siempre arranca en el Login
        MainPage = new NavigationPage(new LoginPage(_api))
        {
            BarBackgroundColor = Color.FromArgb("#0D0D1A"),
            BarTextColor = Colors.White
        };
    }
}