namespace Itm.Store.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Colores de navegación — se aplican por código en MAUI 9
        Shell.SetBackgroundColor(this, Color.FromArgb("#0D0D1A"));
        Shell.SetForegroundColor(this, Colors.White);
        Shell.SetTitleColor(this, Colors.White);
        Shell.SetTabBarBackgroundColor(this, Color.FromArgb("#0D0D1A"));
        Shell.SetTabBarForegroundColor(this, Color.FromArgb("#A78BFA"));
        Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#4B5563"));
        Shell.SetTabBarTitleColor(this, Color.FromArgb("#A78BFA"));
    }
}