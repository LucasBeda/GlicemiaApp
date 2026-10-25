using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

public partial class AddReadingViewModel(GlucoseRepository repository, SessionService session) : SecureViewModel
{
    [ObservableProperty] private string valueText = "";
    [ObservableProperty] private string? selectedContext = GetContextForTime(DateTime.Now.TimeOfDay);
    [ObservableProperty] private string note = "";

    public string[] Contexts => InputValidator.Contexts;

    private static string GetContextForTime(TimeSpan time) =>
        time.Hours >= 18 ? InputValidator.Contexts[2]
        : time.Hours >= 12 ? InputValidator.Contexts[1]
        : InputValidator.Contexts[0];

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var error = await repository.AddAsync(ValueText, SelectedContext, Note);
        if (error is not null) { Show(error, true); return; }

        ValueText = "";
        Note = "";
        Show($"Registrado às {DateTime.Now:HH:mm}.");
    });

    [RelayCommand]
    private void LockNow() => session.Lock();
}
