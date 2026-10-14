using Microsoft.Maui.Controls;
using PassGold.ViewModels;
using System;

namespace PassGold.Views;

public partial class PasajeroDetallePage : ContentPage
{
    public PasajeroDetallePage(PasajeroItem pasajero)
    {
        InitializeComponent();
        BindingContext = pasajero;
    }

    private async void Cerrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}