using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class LockPage : ContentPage
{
    readonly LockViewModel _vm;

    public LockPage(LockViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
