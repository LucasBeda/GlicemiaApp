using Foundation;
using UIKit;

namespace GlicemiaApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    UIView? _cover;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // Cobre a tela quando o app perde o foco, para a miniatura do seletor de apps não mostrar dados.
        NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, _ => Cover());
        NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, _ => Uncover());
        return base.FinishedLaunching(application, launchOptions);
    }

    void Cover()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>().SelectMany(s => s.Windows).FirstOrDefault(w => w.IsKeyWindow) ?? Window;
        if (window is null || _cover is not null) return;

        _cover = new UIView(window.Bounds) { BackgroundColor = UIColor.FromRGB(0x0F, 0x76, 0x6E) };
        window.AddSubview(_cover);
    }

    void Uncover()
    {
        _cover?.RemoveFromSuperview();
        _cover = null;
    }
}
