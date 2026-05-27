using Itm.Store.Mobile.Pages;
using Itm.Store.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Itm.Store.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ─── SERVICIOS (DI) ───────────────────────────────────────────────────
        // Singleton: una sola instancia que comparte el JWT en toda la app
        builder.Services.AddSingleton<ApiService>();

        // Páginas
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<EventsPage>();
        // OrderPage recibe parámetros → se instancia manualmente en EventsPage

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}