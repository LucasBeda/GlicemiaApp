using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryViewModel _vm;
    bool _isReturningFromModal;

    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_isReturningFromModal)
        {
            _isReturningFromModal = false;
            return;
        }

        _vm.LoadCommand.Execute(null);
    }

    async void OnAddClicked(object? sender, EventArgs e)
    {
        if (Handler?.MauiContext?.Services.GetService(typeof(AddPastReadingPage)) is not AddPastReadingPage modal) return;

        await ShowModalAsync(modal);
    }

    async void OnHistoryDayTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: HistoryDay day })
            await OpenEditModalAsync(day);
    }

    async Task OpenEditModalAsync(HistoryDay day)
    {
        if (Handler?.MauiContext?.Services.GetService(typeof(AddPastReadingPage)) is not AddPastReadingPage modal) return;

        modal.PrepareForEdit(day);
        await ShowModalAsync(modal);
    }

    async Task ShowModalAsync(AddPastReadingPage modal)
    {
        EventHandler? onDisappearing = null;
        onDisappearing = (_, _) =>
        {
            modal.Disappearing -= onDisappearing;
            _isReturningFromModal = true;
            if (modal.HasChanges)
                _vm.LoadCommand.Execute(null);
        };
        modal.Disappearing += onDisappearing;
        await Navigation.PushModalAsync(modal);
    }
}
