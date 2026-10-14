using System;
using PassGold.ViewModels;
using PassGold.Services;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class PassengerDashboardPage : ContentPage
{
    public PassengerDashboardPage()
    {
        InitializeComponent();
        BindingContext = new PassengerDashboardViewModel();
    }

    protected override async void OnAppearing() // ¡Añadido async!
    {
        base.OnAppearing();
        if (BindingContext is PassengerDashboardViewModel vm)
        {
            await vm.LoadDashboardCommand.ExecuteAsync(null); // ¡Cambiado a await y ExecuteAsync!
        }

        // 🆕 Si el chofer canceló algún viaje que el pasajero tenía reservado, se le
        // avisa una sola vez, con el motivo (no hay clases, lluvia, chofer enfermo…).
        await CancelacionesService.MostrarAvisosPendientesAsync();
    }

    private async void BuscarFlotas_Clicked(object sender, EventArgs e)
    {
        // Navegación RELATIVA Se quitan las barras dobles //
        await Shell.Current.GoToAsync("fleet-directory");
    }
}