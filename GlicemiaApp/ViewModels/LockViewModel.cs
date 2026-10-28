using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

public partial class LockViewModel(
    SecurityService security,
    SessionService session,
    AuditService audit,
    IBiometricAuthenticationService biometrics) : SecureViewModel
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Title)), NotifyPropertyChangedFor(nameof(Subtitle)),
     NotifyPropertyChangedFor(nameof(ButtonText)), NotifyPropertyChangedFor(nameof(ShowBiometricButton))]
    private bool isCreating;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowBiometricButton))]
    private bool isBiometricAvailable;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BiometricButtonText))]
    private bool isBiometricEnabled;

    [ObservableProperty] private string pin = "";
    [ObservableProperty] private string confirmPin = "";

    public bool ShowBiometricButton => !IsCreating && IsBiometricAvailable;
    public string Title => IsCreating ? "Crie seu PIN" : "Bem-vindo de volta";
    public string Subtitle => IsCreating
        ? "Use 6 dígitos. Ele protege seus registros neste aparelho."
        : "Digite seu PIN para ver seus registros.";
    public string ButtonText => IsCreating ? "Criar PIN" : "Desbloquear";
    public string BiometricButtonText => IsBiometricEnabled
        ? "Desativar desbloqueio biométrico"
        : "Ativar desbloqueio biométrico";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        IsCreating = !await security.HasPinAsync();
        IsBiometricAvailable = !IsCreating && biometrics.IsAvailable;
        IsBiometricEnabled = IsBiometricAvailable && await security.IsBiometricEnabledAsync();

        if (IsBiometricEnabled)
            await AuthenticateBiometricAsync();
    });

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

        await CompleteUnlockAsync();
    });

    [RelayCommand]
    private Task ToggleBiometricAsync() => RunAsync(async () =>
    {
        if (IsBiometricEnabled)
        {
            await security.SetBiometricEnabledAsync(false);
            await audit.LogAsync("biometric_disabled");
            IsBiometricEnabled = false;
            Show("Desbloqueio biométrico desativado.");
            return;
        }

        await AuthenticateBiometricAsync(enableAfterSuccess: true);
    });

    private async Task AuthenticateBiometricAsync(bool enableAfterSuccess = false)
    {
        var result = await biometrics.AuthenticateAsync(enableAfterSuccess
            ? "Confirme sua identidade para ativar o desbloqueio biométrico."
            : "Confirme sua identidade para desbloquear o app.");

        if (result == BiometricAuthenticationResult.Success)
        {
            if (enableAfterSuccess)
            {
                await security.SetBiometricEnabledAsync(true);
                await audit.LogAsync("biometric_enabled");
                IsBiometricEnabled = true;
            }

            await audit.LogAsync("unlock_biometric");
            await CompleteUnlockAsync();
            return;
        }

        await audit.LogAsync(result == BiometricAuthenticationResult.Canceled
            ? "unlock_biometric_canceled"
            : "unlock_biometric_failed");

        if (result == BiometricAuthenticationResult.Unavailable)
        {
            IsBiometricAvailable = false;
            IsBiometricEnabled = false;
            Show("Biometria indisponível. Desbloqueie com seu PIN.", true);
            return;
        }

        Show(result == BiometricAuthenticationResult.Canceled
            ? "Use seu PIN para desbloquear o app."
            : "Não foi possível validar a biometria. Tente novamente ou use seu PIN.", true);
    }

    private async Task CompleteUnlockAsync()
    {
        Pin = ConfirmPin = "";
        Show("");
        session.Unlock();
        await Shell.Current.GoToAsync("//main");
        await QuickAddRequest.OpenIfPendingAsync(); // se o usuário veio pelo widget, abre o registro rápido
    }
}
