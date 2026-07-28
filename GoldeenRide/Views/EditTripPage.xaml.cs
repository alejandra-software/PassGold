using GoldeenRide.ViewModels;

namespace GoldeenRide.Views;

public partial class EditTripPage : ContentPage
{
    public EditTripPage()
    {
        InitializeComponent();
        BindingContext = new EditTripViewModel();
    }
}