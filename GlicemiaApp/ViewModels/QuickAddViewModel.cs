using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

/// <summary>Registro rápido (aberto pelo widget): glicemia, contexto e observação; data e hora são as de agora.</summary>
public partial class QuickAddViewModel(GlucoseRepository repository) : SecureViewModel
{
    [ObservableProperty] private string valueText = "";
    [ObservableProperty] private string? selectedContext = GetContextForTime(DateTime.Now.TimeOfDay);
    [ObservableProperty] private string note = "";

    public string[] Contexts => InputValidator.Contexts;

    private static string GetContextForTime(TimeSpan time) =>
        time.Hours >= 18 ? InputValidator.Contexts[2]
        : time.Hours >= 12 ? InputValidator.Contexts[1]
        : InputValidator.Contexts[0];

    public void Preset(string? context)
    {
        if (context is not null && Array.IndexOf(Contexts, context) >= 0) SelectedContext = context;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var error = await repository.AddAsync(ValueText, SelectedContext, Note);
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
