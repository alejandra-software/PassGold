using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class FleetSchedulePage : ContentPage
{
    public FleetSchedulePage()
    {
        InitializeComponent();
        BindingContext = new PassengerDashboardViewModel();
    }
}