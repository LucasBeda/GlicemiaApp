using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class AddPastReadingPage : ContentPage
{
    public AddPastReadingPage(AddPastReadingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
