using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Services;
using System.Collections.ObjectModel;

namespace GlicemiaApp.ViewModels;

public partial class HistoryViewModel : SecureViewModel
{
    readonly GlucoseRepository _repository;

    public ObservableCollection<HistoryDay> Days { get; } = [];

    public HistoryViewModel(GlucoseRepository repository, SessionService session)
    {
        _repository = repository;
        // Ao bloquear, os dados saem da memória da tela.
        session.Locked += () => MainThread.BeginInvokeOnMainThread(Days.Clear);
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(LoadCoreAsync);

    async Task LoadCoreAsync()
    {
        var rows = await _repository.ListAsync(days: GlicemiaApp.Core.InputValidator.MaxBackdateDays);
        var days = await Task.Run(() => rows
            .GroupBy(r => r.MeasuredAtUtc.ToLocalTime().Date)
            .OrderByDescending(g => g.Key)
            .Select(group => new HistoryDay(group.Key, group))
            .ToArray());

        Days.Clear();
        foreach (var day in days)
            Days.Add(day);

        Show(rows.Count == 0 ? "" : "Toque em um dia para editar as medições desse dia.");
    }
}
