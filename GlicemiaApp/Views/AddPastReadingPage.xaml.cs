using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class AddPastReadingPage : ContentPage
{
    readonly AddPastReadingViewModel _vm;

    public AddPastReadingPage(AddPastReadingViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void PrepareForEdit(HistoryDay day) => _vm.PrepareForEdit(day);
}
