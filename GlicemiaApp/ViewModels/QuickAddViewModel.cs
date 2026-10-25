using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

/// <summary>Registro rápido (aberto pelo widget): só a glicemia e o contexto; data e hora são as de agora.</summary>
public partial class QuickAddViewModel(GlucoseRepository repository) : SecureViewModel
{
    [ObservableProperty] private string valueText = "";
    [ObservableProperty] private string? selectedContext = InputValidator.Contexts[0];

    public string[] Contexts => InputValidator.Contexts;

    public void Preset(string? context)
    {
        if (context is not null && Array.IndexOf(Contexts, context) >= 0) SelectedContext = context;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var error = await repository.AddAsync(ValueText, SelectedContext, "");
        if (error is not null) { Show(error, true); return; }

#if ANDROID
        Android.Widget.Toast.MakeText(Platform.AppContext, "Glicemia registrada", Android.Widget.ToastLength.Short)?.Show();
#endif
        await CloseAsync();
    });

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.Navigation.PopModalAsync();
#if ANDROID
        Platform.CurrentActivity?.MoveTaskToBack(true); // volta para a tela inicial, de onde o widget foi tocado
#endif
    }
}
