using GoldeenRide.ViewModels;
using Microsoft.Maui.Controls;

namespace GoldeenRide.Views;

public partial class TripDetailsPage : ContentPage
{
    public TripDetailsPage()
    {
        InitializeComponent();

        
        BindingContext = new TripDetailsViewModel();
    }
}