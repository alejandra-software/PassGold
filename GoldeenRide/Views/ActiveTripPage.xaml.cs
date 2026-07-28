using Microsoft.Maui.Controls;
using GoldeenRide.ViewModels;

namespace GoldeenRide.Views;

public partial class ActiveTripPage : ContentPage
{
    public ActiveTripPage()
    {
        InitializeComponent();
    }

    // Usamos OnAppearing para disparar la carga de datos cuando la pantalla se muestra
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ActiveTripViewModel viewModel)
        {
            // Más adelante aquí le pasaremos el ID de la Asignación real que viene del Dashboard
            await viewModel.LoadTripDataAsync("id_temporal");
        }
    }
}