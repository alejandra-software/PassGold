using PassGold.ViewModels;
using PassGold.Helpers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class EditTripPage : ContentPage
{
    public EditTripPage()
    {
        InitializeComponent();
        BindingContext = new EditTripViewModel();

        MapaDestinosOficial.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion) &&
                MapaDestinosOficial.VisibleRegion != null &&
                BindingContext is EditTripViewModel vm)
            {
                var centro = MapaDestinosOficial.VisibleRegion.Center;
                vm.LatitudTemporal = centro.Latitude;
                vm.LongitudTemporal = centro.Longitude;
            }
        };

        if (BindingContext is EditTripViewModel viewModel)
        {
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EditTripViewModel.IsMapModalVisible) && viewModel.IsMapModalVisible)
                {
                    var ubicacion = new Location(viewModel.LatitudTemporal, viewModel.LongitudTemporal);
                    MapaDestinosOficial.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacion, Distance.FromKilometers(1)));
                }
            };
        }
    }

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

                if (BindingContext is EditTripViewModel vm)
                {
                    vm.LatitudTemporal = ubicacionEncontrada.Latitude;
                    vm.LongitudTemporal = ubicacionEncontrada.Longitude;
                }

                BuscadorMetas.Unfocus();
            }
            else
            {
                await DisplayAlert(AppResources.Global_NotFound, AppResources.Schedule_MapErrorLocation, AppResources.Global_Ok);
            }
        }
        catch (Exception)
        {
            await DisplayAlert(AppResources.Global_Error, AppResources.Global_NoInternetSearch, AppResources.Global_Ok);
        }
    }
}
