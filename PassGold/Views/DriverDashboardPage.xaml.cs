using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class DriverDashboardPage : ContentPage
{
    public DriverDashboardPage()
    {
        InitializeComponent();
        BindingContext = new DriverDashboardViewModel();
    }

    protected override async void OnAppearing() // ¡Añadido async!
    {
        base.OnAppearing();
        if (BindingContext is DriverDashboardViewModel vm)
        {
            await vm.LoadDriverDataCommand.ExecuteAsync(null); // Esto ya estaba bien, pero faltaba el async arriba.
        }
    }
}