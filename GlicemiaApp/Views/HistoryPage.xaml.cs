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

    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ReadingRow row) return;
        ((CollectionView)sender!).SelectedItem = null; // permite tocar de novo no mesmo item
        _vm.DeleteCommand.Execute(row);
    }
}
