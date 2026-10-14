using PassGold.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PassGold.Views;

public partial class ScheduleTripPage : ContentPage
{
    private bool _eventosAsignados = false;
    private bool _permisosVerificados = false;

    public ScheduleTripPage()
    {
        InitializeComponent();

        if (App.Services != null)
        {
            BindingContext = App.Services.GetService<ScheduleTripViewModel>();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_eventosAsignados)
        {
            MapaDestinosOficial.PropertyChanged += MapaDestinosOficial_PropertyChanged;

            if (BindingContext is ScheduleTripViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            _eventosAsignados = true;
        }

        if (!_permisosVerificados)
        {
            await VerificarPermisoUbicacionAsync();
            _permisosVerificados = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_eventosAsignados)
        {
            MapaDestinosOficial.PropertyChanged -= MapaDestinosOficial_PropertyChanged;

            if (BindingContext is ScheduleTripViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _eventosAsignados = false;
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

    private void MapaDestinosOficial_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Microsoft.Maui.Controls.Maps.Map.VisibleRegion) &&
            MapaDestinosOficial.VisibleRegion != null &&
            BindingContext is ScheduleTripViewModel vm)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var centro = MapaDestinosOficial.VisibleRegion.Center;
                vm.LatitudTemporal = centro.Latitude;
                vm.LongitudTemporal = centro.Longitude;
            });
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ScheduleTripViewModel viewModel)
        {
            if (e.PropertyName == nameof(ScheduleTripViewModel.IsMapModalVisible) && viewModel.IsMapModalVisible)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var ubicacion = new Location(viewModel.LatitudTemporal, viewModel.LongitudTemporal);
                    MapaDestinosOficial.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacion, Distance.FromKilometers(1)));
                });
            }
        }
    }

    private async void BuscadorMetas_SearchButtonPressed(object sender, EventArgs e)
    {
        var query = BuscadorMetas.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            // Buscador compartido (Apple Maps en iPhone + filtros para El Salvador)
            var ubicacionEncontrada = await PassGold.Helpers.BuscadorLugares.BuscarAsync(query, MapaDestinosOficial.VisibleRegion?.Center);

            if (ubicacionEncontrada != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MapaDestinosOficial.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacionEncontrada, Distance.FromKilometers(0.5)));

                    if (BindingContext is ScheduleTripViewModel vm)
                    {
                        vm.LatitudTemporal = ubicacionEncontrada.Latitude;
                        vm.LongitudTemporal = ubicacionEncontrada.Longitude;
                    }

                    BuscadorMetas.Unfocus();
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert(PassGold.Helpers.AppResources.Global_NotFound, PassGold.Helpers.AppResources.Schedule_PlaceNotFound, PassGold.Helpers.AppResources.Global_Ok);
                });
            }
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert(PassGold.Helpers.AppResources.Global_Error, PassGold.Helpers.AppResources.Schedule_NeedInternetSearch, PassGold.Helpers.AppResources.Global_Ok);
            });
        }
    }
}