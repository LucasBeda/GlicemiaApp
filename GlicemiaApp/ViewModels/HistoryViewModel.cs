using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

public partial class HistoryViewModel : SecureViewModel
{
    readonly GlucoseRepository _repository;

    [ObservableProperty]
    private IReadOnlyList<HistoryDay> days = Array.Empty<HistoryDay>();

    public HistoryViewModel(GlucoseRepository repository, SessionService session)
    {
        _repository = repository;
        // Ao bloquear, os dados saem da memória da tela.
        session.Locked += () => MainThread.BeginInvokeOnMainThread(() => Days = Array.Empty<HistoryDay>());
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

        Days = days;

        Show(rows.Count == 0 ? "" : "Toque em um dia para editar as medições desse dia.");
    }
}
