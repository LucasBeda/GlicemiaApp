using GlicemiaApp.Services;
using GlicemiaApp.ViewModels;
using GlicemiaApp.Views;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace GlicemiaApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if MAUI_DEVFLOW
        builder.AddMauiDevFlowAgent();
#endif

        // Sem pacote de logging de propósito: nada de dados de saúde em logs.
        builder.Services
#if ANDROID
            .AddSingleton<IBiometricAuthenticationService, GlicemiaApp.Platforms.BiometricAuthenticationService>()
#elif IOS
            .AddSingleton<IBiometricAuthenticationService, GlicemiaApp.Platforms.BiometricAuthenticationService>()
#endif
            .AddSingleton<SessionService>()
            .AddSingleton<SecurityService>()
            .AddSingleton<DatabaseService>()
            .AddSingleton<AuditService>()
            .AddSingleton<GlucoseRepository>()
            .AddSingleton<WidgetFeed>()
            .AddTransient<LockViewModel>()
            .AddTransient<AddReadingViewModel>()
            .AddTransient<HistoryViewModel>()
            .AddTransient<AddPastReadingViewModel>()
            .AddTransient<AddPastReadingPage>()
            .AddTransient<QuickAddViewModel>()
            .AddTransient<QuickAddPage>()
            .AddTransient<LockPage>()
            .AddTransient<AddReadingPage>()
            .AddTransient<HistoryPage>();

        return builder.Build();
    }
}
