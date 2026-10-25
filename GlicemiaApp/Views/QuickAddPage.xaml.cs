using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class QuickAddPage : ContentPage
{
    readonly QuickAddViewModel _vm;

    public QuickAddPage(QuickAddViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void Preset(string? context) => _vm.Preset(context);
}
