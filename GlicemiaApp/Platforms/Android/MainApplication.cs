using Android.App;
using Android.Runtime;
using GlicemiaApp.Services;

namespace GlicemiaApp;

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        base.OnCreate();

        // Salvou ou excluiu um registro: redesenha o widget da tela inicial (uma assinatura por processo).
        GlucoseRepository.Changed += () => _ = GlucoseWidget.RefreshAsync(this);
    }
}
