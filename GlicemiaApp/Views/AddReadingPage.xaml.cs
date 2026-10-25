using GlicemiaApp.ViewModels;

namespace GlicemiaApp.Views;

public partial class AddReadingPage : ContentPage
{
    public AddReadingPage(AddReadingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
