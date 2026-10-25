using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

public partial class LockViewModel(SecurityService security, SessionService session, AuditService audit) : SecureViewModel
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Title)), NotifyPropertyChangedFor(nameof(Subtitle)), NotifyPropertyChangedFor(nameof(ButtonText))]
    private bool isCreating;

    [ObservableProperty] private string pin = "";
    [ObservableProperty] private string confirmPin = "";

    public string Title => IsCreating ? "Crie seu PIN" : "Bem-vindo de volta";
    public string Subtitle => IsCreating
        ? "Use 6 dígitos. Ele protege seus registros neste aparelho."
        : "Digite seu PIN para ver seus registros.";
    public string ButtonText => IsCreating ? "Criar PIN" : "Desbloquear";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () => IsCreating = !await security.HasPinAsync());

    [RelayCommand]
    private Task SubmitAsync() => RunAsync(async () =>
    {
        if (IsCreating)
        {
            var problem = PinHasher.ValidateNew(Pin, ConfirmPin);
            if (problem is not null) { Show(problem, true); return; }
            await security.SetPinAsync(Pin);
            await audit.LogAsync("pin_created");
        }
        else
        {
            var (ok, wait) = await security.VerifyPinAsync(Pin);
            await audit.LogAsync(ok ? "unlock" : "unlock_failed");
            if (!ok)
            {
                Pin = "";
                // Mensagem genérica: não revela se o PIN "quase" estava certo.
                Show(wait > TimeSpan.Zero
                    ? $"Muitas tentativas. Aguarde {(int)Math.Ceiling(wait.TotalSeconds)} s."
                    : "PIN incorreto.", true);
                return;
            }
        }

        Pin = ConfirmPin = "";
        Show("");
        session.Unlock();
        await Shell.Current.GoToAsync("//main");
        await QuickAddRequest.OpenIfPendingAsync(); // se o usuário veio pelo widget, abre o registro rápido
    });
}
