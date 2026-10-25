using GlicemiaApp.Services;

namespace GlicemiaApp;

public partial class App : Application
{
    readonly SessionService _session;

    public App(SessionService session)
    {
        InitializeComponent();
        _session = session;

        // Qualquer bloqueio (inatividade, botão "Bloquear", app em segundo plano) volta para a tela de PIN.
        _session.Locked += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current is not { } shell) return;
            while (shell.Navigation.ModalStack.Count > 0) await shell.Navigation.PopModalAsync(false); // fecha o modal de inclusão
            await shell.GoToAsync("//lock");
        });

        // Pedido do widget com o app já desbloqueado abre direto; bloqueado, fica pendente até o PIN.
        QuickAddRequest.Requested += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_session.IsUnlocked) await QuickAddRequest.OpenIfPendingAsync();
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Stopped += (_, _) => _session.Lock(); // app foi para segundo plano
        return window;
    }
}
