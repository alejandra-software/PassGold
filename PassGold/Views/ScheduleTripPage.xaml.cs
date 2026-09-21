using PassGold.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class ScheduleTripPage : ContentPage
{
    public ScheduleTripPage()
    {
        InitializeComponent();

        // Cuando el mapa nativo cambia su región visible (el usuario arrastra el mapa
        // a mano, además de cuando buscamos), guardamos el centro real en el ViewModel
        // para que ConfirmarPuntoMapa() ya no use el placeholder 0,0.
        MapaDestinosOficial.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion) &&
                MapaDestinosOficial.VisibleRegion != null &&
                BindingContext is ScheduleTripViewModel vm)
            {
                var centro = MapaDestinosOficial.VisibleRegion.Center;
                vm.LatitudTemporal = centro.Latitude;
                vm.LongitudTemporal = centro.Longitude;
            }
        };

        // Cuando se abre el modal del mapa, lo centramos en la última posición conocida
        // (o el default de Santa Ana si es la primera vez).
        if (BindingContext is ScheduleTripViewModel viewModel)
        {
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ScheduleTripViewModel.IsMapModalVisible) && viewModel.IsMapModalVisible)
                {
                    var ubicacion = new Location(viewModel.LatitudTemporal, viewModel.LongitudTemporal);
                    MapaDestinosOficial.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacion, Distance.FromKilometers(1)));
                }
            };
        }
    }

    //  Buscador gratuito: usa el motor de geocodificación del propio celular,
    // sin API key ni costo — el mismo patrón que ya usan en PassengerDashboardPage.
    private async void BuscadorMetas_SearchButtonPressed(object sender, EventArgs e)
    {
        var query = BuscadorMetas.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            string busquedaExacta = $"{query}, El Salvador";
            var ubicaciones = await Geocoding.Default.GetLocationsAsync(busquedaExacta);
            var ubicacionEncontrada = ubicaciones?.FirstOrDefault();

            if (ubicacionEncontrada != null)
            {
                MapaDestinosOficial.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacionEncontrada, Distance.FromKilometers(0.5)));

                if (BindingContext is ScheduleTripViewModel vm)
                {
                    vm.LatitudTemporal = ubicacionEncontrada.Latitude;
                    vm.LongitudTemporal = ubicacionEncontrada.Longitude;
                }

                BuscadorMetas.Unfocus();
            }
            else
            {
                await DisplayAlert("No encontrado", "No pudimos localizar ese lugar exacto. Intenta con un nombre más general, o simplemente arrastra el mapa hasta el lugar correcto.", "OK");
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Error", "Necesitas internet para usar el buscador de direcciones.", "OK");
        }
    }
}