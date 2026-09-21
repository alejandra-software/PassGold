using System;
using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class PassengerDashboardPage : ContentPage
{
    public PassengerDashboardPage()
    {
        InitializeComponent();
        BindingContext = new PassengerDashboardViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PassengerDashboardViewModel vm)
        {
            vm.LoadDashboardCommand.Execute(null);
        }
    }

    // FIX MAC/IOS: Ruta absoluta con // para evitar crashes de navegación en iOS.
    private async void BuscarFlotas_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//fleet-directory");
    }
}