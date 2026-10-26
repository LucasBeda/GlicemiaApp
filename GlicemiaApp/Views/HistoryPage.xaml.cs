using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryViewModel _vm;

    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }

    async void OnAddClicked(object? sender, EventArgs e)
    {
        if (Handler?.MauiContext?.Services.GetService(typeof(AddPastReadingPage)) is not AddPastReadingPage modal) return;

        // Ao fechar o modal (salvou ou cancelou), recarrega a lista.
        modal.Disappearing += (_, _) => _vm.LoadCommand.Execute(null);
        await Navigation.PushModalAsync(modal);
    }

    void OnBreakfastTapped(object? sender, TappedEventArgs e) => DeleteReading(sender, day => day.Breakfast);

    void OnLunchTapped(object? sender, TappedEventArgs e) => DeleteReading(sender, day => day.Lunch);

    void OnDinnerTapped(object? sender, TappedEventArgs e) => DeleteReading(sender, day => day.Dinner);

    void DeleteReading(object? sender, Func<HistoryDay, ReadingRow?> selectReading)
    {
        if (sender is BindableObject { BindingContext: HistoryDay day } && selectReading(day) is ReadingRow row)
            _vm.DeleteCommand.Execute(row);
    }
}
