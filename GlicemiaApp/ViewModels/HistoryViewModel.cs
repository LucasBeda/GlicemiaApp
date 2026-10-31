using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlicemiaApp.Services;

namespace GlicemiaApp.ViewModels;

public partial class HistoryViewModel : SecureViewModel
{
    const int RecentDays = 7;

    readonly GlucoseRepository _repository;

    [ObservableProperty]
    private IReadOnlyList<HistoryDay> days = Array.Empty<HistoryDay>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HistoryRangeButtonText))]
    private bool isShowingAllHistory;

    public string HistoryRangeButtonText =>
        IsShowingAllHistory ? "Mostrar últimos 7 dias" : "Carregar todo o histórico";

    public HistoryViewModel(GlucoseRepository repository, SessionService session)
    {
        _repository = repository;
        // Ao bloquear, os dados saem da memória da tela.
        session.Locked += () => MainThread.BeginInvokeOnMainThread(() =>
        {
            Days = Array.Empty<HistoryDay>();
            IsShowingAllHistory = false;
        });
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(() => LoadCoreAsync(IsShowingAllHistory));

    [RelayCommand]
    private Task ToggleHistoryRangeAsync() => RunAsync(async () =>
    {
        var showAll = !IsShowingAllHistory;
        await LoadCoreAsync(showAll);
        IsShowingAllHistory = showAll;
    });

    async Task LoadCoreAsync(bool showAll)
    {
        var rows = await _repository.ListAsync(days: showAll ? null : RecentDays);
        var days = await Task.Run(() => rows
            .GroupBy(r => r.MeasuredAtUtc.ToLocalTime().Date)
            .OrderByDescending(g => g.Key)
            .Select(group => new HistoryDay(group.Key, group))
            .ToArray());

        Days = days;

        Show(rows.Count == 0 ? "" : "Toque em um dia para editar as medições desse dia.");
    }
}
