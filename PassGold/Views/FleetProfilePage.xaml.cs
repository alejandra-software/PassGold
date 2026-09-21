using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class FleetProfilePage : ContentPage
{
    public FleetProfilePage()
    {
        InitializeComponent();
        BindingContext = new FleetProfileViewModel();
    }
}
