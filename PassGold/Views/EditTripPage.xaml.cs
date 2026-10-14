using PassGold.ViewModels;
using PassGold.Helpers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace PassGold.Views;

public partial class EditTripPage : ContentPage
{
    // Mismo patrón que ScheduleTripPage: pedimos el permiso de ubicación recién
    // en OnAppearing, y solo si se concede activamos IsShowingUser por código —
    // nunca directo en el XAML (eso era lo que causaba el crash).
    private bool _permisosVerificados = false;

    public EditTripPage()
    {
        InitializeComponent();

        if (App.Services != null)
        {
            BindingContext = App.Services.GetService<EditTripViewModel>();
        }

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_permisosVerificados)
        {
            await VerificarPermisoUbicacionAsync();
            _permisosVerificados = true;
        }
    }

    private async Task VerificarPermisoUbicacionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MapaDestinosOficial.IsShowingUser = true;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error solicitando permisos: {ex.Message}");
        }
    }

    private async void BuscadorMetas_SearchButtonPressed(object sender, EventArgs e)
    {
        var query = BuscadorMetas.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            // Buscador compartido (Apple Maps en iPhone + filtros para El Salvador)
            var ubicacionEncontrada = await BuscadorLugares.BuscarAsync(query, MapaDestinosOficial.VisibleRegion?.Center);

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