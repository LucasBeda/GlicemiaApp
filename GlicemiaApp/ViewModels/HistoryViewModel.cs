using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Services;
using System.Collections.ObjectModel;

namespace GlicemiaApp.ViewModels;

public partial class HistoryViewModel : SecureViewModel
{
    readonly GlucoseRepository _repository;

    public ObservableCollection<DayGroup> Days { get; } = [];

    public HistoryViewModel(GlucoseRepository repository, SessionService session)
    {
        _repository = repository;
        // Ao bloquear, os dados saem da memória da tela.
        session.Locked += () => MainThread.BeginInvokeOnMainThread(Days.Clear);
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(LoadCoreAsync);

    [RelayCommand]
    private async Task DeleteAsync(ReadingRow? row)
    {
        if (row is null) return;
        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Excluir registro", $"Excluir a medição das {row.Time}? Isso não pode ser desfeito.", "Excluir", "Cancelar");
        if (!confirmed) return;

        await RunAsync(async () =>
        {
            await _repository.DeleteAsync(row.Id);
            await LoadCoreAsync();
        });
    }

    async Task LoadCoreAsync()
    {
        var rows = await _repository.ListAsync(days: GlicemiaApp.Core.InputValidator.MaxBackdateDays);
        Days.Clear();
        foreach (var day in rows.GroupBy(r => r.MeasuredAtUtc.ToLocalTime().Date).OrderByDescending(g => g.Key))
            Days.Add(new DayGroup(day.Key, day.OrderByDescending(r => r.MeasuredAtUtc).Select(r => new ReadingRow(r))));

        Show(rows.Count == 0 ? "" : "Toque em um registro para excluí-lo.");
    }
}
