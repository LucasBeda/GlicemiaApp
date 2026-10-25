using Android.App;
using Android.Content;
using Android.Content.PM;
using GlicemiaApp.Services;
using Android.OS;
using Android.Views;

namespace GlicemiaApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Bloqueia captura de tela, gravação e miniatura na lista de apps recentes.
        Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);

        // Ignora toques quando outra janela está sobre o app (anti-tapjacking).
        if (Window?.DecorView is { } decor) decor.FilterTouchesWhenObscured = true;

        HandleWidgetIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleWidgetIntent(intent);
    }

    static void HandleWidgetIntent(Intent? intent)
    {
        if (intent?.Action != GlucoseWidget.ActionQuickAdd) return;

        var context = intent.GetStringExtra(GlucoseWidget.ExtraContext);
        intent.SetAction(Intent.ActionMain); // consome: restaurar a activity não reabre o registro rápido
        QuickAddRequest.Request(context);
    }
}
