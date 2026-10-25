using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

/// <summary>Modal do histórico: mesmos campos da tela Registrar, mais data e hora da medição.</summary>
public partial class AddPastReadingViewModel(GlucoseRepository repository) : SecureViewModel
{
    [ObservableProperty] private string valueText = "";
    [ObservableProperty] private string? selectedContext = GetContextForTime(DateTime.Now.TimeOfDay);
    [ObservableProperty] private string note = "";
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] public TimeSpan time = new(DateTime.Now.Hour, DateTime.Now.Minute, 0);

    public string[] Contexts => InputValidator.Contexts;
    public DateTime MinDate { get; } = DateTime.Today.AddDays(-InputValidator.MaxBackdateDays);
    public DateTime MaxDate { get; } = DateTime.Today;

    partial void OnTimeChanged(TimeSpan value) => SelectedContext = GetContextForTime(value);

    private static string GetContextForTime(TimeSpan time) =>
        time.Hours >= 18 ? InputValidator.Contexts[2]
        : time.Hours >= 12 ? InputValidator.Contexts[1]
        : InputValidator.Contexts[0];

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var measuredAt = DateTime.SpecifyKind(Date.Date + Time, DateTimeKind.Local);
        var error = await repository.AddAsync(ValueText, SelectedContext, Note, measuredAt);
        if (error is not null) { Show(error, true); return; }

        await Shell.Current.Navigation.PopModalAsync();
    });

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.Navigation.PopModalAsync();
}
