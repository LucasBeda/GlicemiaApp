using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Core;
using GlicemiaApp.Services;
using System.Collections.ObjectModel;

namespace GlicemiaApp.ViewModels;

/// <summary>Modal do histórico para registrar até quatro medições da mesma data.</summary>
public partial class AddPastReadingViewModel(GlucoseRepository repository) : SecureViewModel
{
    int[] _editingReadingIds = [];

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

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(IsAddExtraReadingVisible)),
     NotifyPropertyChangedFor(nameof(IsNotEditing)),
     NotifyPropertyChangedFor(nameof(Title)),
     NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool isEditing;

    public string[] Contexts => InputValidator.Contexts;
    public ObservableCollection<EditableReading> EditingReadings { get; } = [];
    public bool HasChanges { get; private set; }
    public DateTime MinDate { get; } = DateTime.Today.AddDays(-InputValidator.MaxBackdateDays);
    public DateTime MaxDate { get; } = DateTime.Today;
    public bool IsAddExtraReadingVisible => IsNotEditing && !IsExtraReadingVisible;
    public bool IsNotEditing => !IsEditing;
    public string Title => IsEditing ? "Editar registro" : "Adicionar ao histórico";
    public string SaveButtonText => IsEditing ? "Salvar alterações" : "Salvar medições";

    public void PrepareForEdit(HistoryDay day)
    {
        HasChanges = false;
        _editingReadingIds = day.Readings.Select(reading => reading.Id).ToArray();
        IsEditing = true;
        Date = day.Day;
        Note = day.CombinedNote;
        EditingReadings.Clear();
        foreach (var reading in day.Readings)
            EditingReadings.Add(new EditableReading(reading));
    }

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
        if (IsEditing)
        {
            if (_editingReadingIds.Length == 0)
            {
                Show("Não foi possível identificar as medições para edição.", true);
                return;
            }

            var updates = EditingReadings.Select(reading =>
                (reading.Id, (string?)reading.ValueText, reading.Context,
                    DateTime.SpecifyKind(Date.Date + reading.Time, DateTimeKind.Local))).ToArray();
            var updateError = await repository.UpdateManyAsync(updates, Note);
            if (updateError is not null) { Show(updateError, true); return; }

            HasChanges = true;
            await Shell.Current.Navigation.PopModalAsync();
            return;
        }

        var readings = new List<(string? ValueText, string? Context, string? Note, DateTime MeasuredAtLocal)>();
        AddReading(readings, Reading1ValueText, Reading1Time, Reading1Context);
        AddReading(readings, Reading2ValueText, Reading2Time, Reading2Context);
        AddReading(readings, Reading3ValueText, Reading3Time, Reading3Context);
        if (IsExtraReadingVisible)
            AddReading(readings, Reading4ValueText, Reading4Time, Reading4Context);

        var error = await repository.AddManyBackdatedAsync(readings);
        if (error is not null) { Show(error, true); return; }

        HasChanges = true;
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

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (_editingReadingIds.Length == 0) return;

        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Excluir registros", "Excluir todas as medições deste dia? Isso não pode ser desfeito.", "Excluir", "Cancelar");
        if (!confirmed) return;

        var error = await repository.DeleteManyAsync(_editingReadingIds);
        if (error is not null) { Show(error, true); return; }
        HasChanges = true;
        await Shell.Current.Navigation.PopModalAsync();
    });
}

public partial class EditableReading : ObservableObject
{
    public EditableReading(ReadingRow reading)
    {
        Id = reading.Id;
        ValueText = reading.ValueMgDl.ToString();
        Time = reading.MeasuredAtLocal.TimeOfDay;
        Context = reading.Context;
    }

    public int Id { get; }
    public string[] Contexts => InputValidator.Contexts;

    [ObservableProperty] private string valueText = "";
    [ObservableProperty] private TimeSpan time;
    [ObservableProperty] private string? context;
}
