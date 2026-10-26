using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

/// <summary>Modal do histórico para registrar até quatro medições da mesma data.</summary>
public partial class AddPastReadingViewModel(GlucoseRepository repository) : SecureViewModel
{
    [ObservableProperty] private string reading1ValueText = "";
    [ObservableProperty] private string? reading1Context = GetContextForTime(TimeSpan.FromHours(8));
    [ObservableProperty] private TimeSpan reading1Time = TimeSpan.FromHours(8);

    [ObservableProperty] private string reading2ValueText = "";
    [ObservableProperty] private string? reading2Context = GetContextForTime(TimeSpan.FromHours(12));
    [ObservableProperty] private TimeSpan reading2Time = TimeSpan.FromHours(12);

    [ObservableProperty] private string reading3ValueText = "";
    [ObservableProperty] private string? reading3Context = GetContextForTime(TimeSpan.FromHours(18));
    [ObservableProperty] private TimeSpan reading3Time = TimeSpan.FromHours(18);

    [ObservableProperty] private string reading4ValueText = "";
    [ObservableProperty] private string? reading4Context = InputValidator.Contexts[3];
    [ObservableProperty] private TimeSpan reading4Time = TimeSpan.FromHours(00);

    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string note = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsAddExtraReadingVisible))]
    private bool isExtraReadingVisible;

    public string[] Contexts => InputValidator.Contexts;
    public DateTime MinDate { get; } = DateTime.Today.AddDays(-InputValidator.MaxBackdateDays);
    public DateTime MaxDate { get; } = DateTime.Today;
    public bool IsAddExtraReadingVisible => !IsExtraReadingVisible;

    partial void OnReading1TimeChanged(TimeSpan value) => Reading1Context = GetContextForTime(value);
    partial void OnReading2TimeChanged(TimeSpan value) => Reading2Context = GetContextForTime(value);
    partial void OnReading3TimeChanged(TimeSpan value) => Reading3Context = GetContextForTime(value);
    partial void OnReading4TimeChanged(TimeSpan value) => Reading4Context = GetContextForTime(value);

    private static string GetContextForTime(TimeSpan time) =>
        time.Hours >= 18 ? InputValidator.Contexts[2]
        : time.Hours >= 12 ? InputValidator.Contexts[1]
        : InputValidator.Contexts[0];

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var readings = new List<(string? ValueText, string? Context, string? Note, DateTime MeasuredAtLocal)>();
        AddReading(readings, Reading1ValueText, Reading1Time, Reading1Context);
        AddReading(readings, Reading2ValueText, Reading2Time, Reading2Context);
        AddReading(readings, Reading3ValueText, Reading3Time, Reading3Context);
        if (IsExtraReadingVisible)
            AddReading(readings, Reading4ValueText, Reading4Time, Reading4Context);

        var error = await repository.AddManyBackdatedAsync(readings);
        if (error is not null) { Show(error, true); return; }

        await Shell.Current.Navigation.PopModalAsync();
    });

    private void AddReading(
        ICollection<(string? ValueText, string? Context, string? Note, DateTime MeasuredAtLocal)> readings,
        string valueText,
        TimeSpan time,
        string? context)
    {
        if (string.IsNullOrWhiteSpace(valueText)) return;

        var measuredAt = DateTime.SpecifyKind(Date.Date + time, DateTimeKind.Local);
        readings.Add((valueText, context, Note, measuredAt));
    }

    [RelayCommand]
    private void AddExtraReading() => IsExtraReadingVisible = true;

    [RelayCommand]
    private void RemoveExtraReading()
    {
        Reading4ValueText = "";
        Reading4Time = TimeSpan.FromHours(20);
        IsExtraReadingVisible = false;
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.Navigation.PopModalAsync();
}
