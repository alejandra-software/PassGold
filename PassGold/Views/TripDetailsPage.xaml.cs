using PassGold.ViewModels;
using PassGold.Helpers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Linq;

namespace PassGold.Views;

public partial class TripDetailsPage : ContentPage
{
    public TripDetailsPage()
    {
        InitializeComponent();
        var vm = new TripDetailsViewModel();
        BindingContext = vm;

        vm.Pasajeros.CollectionChanged += (s, e) => RedibujarPines();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TripDetailsViewModel.MetaLatitud)
                || e.PropertyName == nameof(TripDetailsViewModel.MetaLongitud)
                || e.PropertyName == nameof(TripDetailsViewModel.IsMapaSubTabRecogida)
                || e.PropertyName == nameof(TripDetailsViewModel.EsIda))
                RedibujarPines();
        };

        // NUEVO: Suscripción al evento para centrar la cámara cuando el chofer toca "Ubicar"
        vm.OnCentrarMapa += (lat, lng) =>
        {
            var loc = new Location(lat, lng);
            var radioEnfoque = MapSpan.FromCenterAndRadius(loc, Distance.FromKilometers(0.3));
            MapaRutaEnVivo.MoveToRegion(radioEnfoque);
            MapaPantallaCompleta.MoveToRegion(radioEnfoque);
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var ubicacionInicial = new Location(13.9778, -89.5639);
        var radioVista = MapSpan.FromCenterAndRadius(ubicacionInicial, Distance.FromKilometers(1.5));
        MapaRutaEnVivo.MoveToRegion(radioVista);
        MapaPantallaCompleta.MoveToRegion(radioVista);
    }

    private void RedibujarPines()
    {
        if (BindingContext is not TripDetailsViewModel vm) return;

        MapaRutaEnVivo.Pins.Clear();
        MapaPantallaCompleta.Pins.Clear();

        bool metaVaEnRecogida = !vm.EsIda;
        bool mostrarMeta = (vm.IsMapaSubTabRecogida && metaVaEnRecogida) || (!vm.IsMapaSubTabRecogida && !metaVaEnRecogida);

        if (mostrarMeta && vm.MetaLatitud.HasValue && vm.MetaLongitud.HasValue)
        {
            var pinMeta = new Pin
            {
                Label = string.IsNullOrEmpty(vm.MetaTexto) ? AppResources.TripDetails_DriverMeta : $"🏁 {vm.MetaTexto}",
                Type = PinType.Generic,
                Location = new Location(vm.MetaLatitud.Value, vm.MetaLongitud.Value)
            };
            MapaRutaEnVivo.Pins.Add(pinMeta);
            MapaPantallaCompleta.Pins.Add(new Pin { Label = pinMeta.Label, Type = pinMeta.Type, Location = pinMeta.Location });
        }

        foreach (var p in vm.Pasajeros)
        {
            if (vm.IsMapaSubTabRecogida)
            {
                if (p.Latitud.HasValue && p.Longitud.HasValue)
                {
                    var pin = new Pin
                    {
                        // Se usa el comportamiento estandar de MAUI:
                        // Solo muestra este nombre cuando el chofer toca el pin rojo
                        Label = p.Nombre,
                        Address = p.Ubicacion,
                        Type = PinType.Place,
                        Location = new Location(p.Latitud.Value, p.Longitud.Value)
                    };
                    MapaRutaEnVivo.Pins.Add(pin);
                    MapaPantallaCompleta.Pins.Add(new Pin { Label = pin.Label, Address = pin.Address, Type = pin.Type, Location = pin.Location });
                }
            }
            else
            {
                if (p.LatitudBajada.HasValue && p.LongitudBajada.HasValue)
                {
                    var pin = new Pin
                    {
                        Label = p.Nombre,
                        Address = p.PuntoBajada ?? "",
                        Type = PinType.SavedPin,
                        Location = new Location(p.LatitudBajada.Value, p.LongitudBajada.Value)
                    };
                    MapaRutaEnVivo.Pins.Add(pin);
                    MapaPantallaCompleta.Pins.Add(new Pin { Label = pin.Label, Address = pin.Address, Type = pin.Type, Location = pin.Location });
                }
            }
        }
    }

    private void AbrirPantallaCompleta_Clicked(object sender, System.EventArgs e)
    {
        if (BindingContext is TripDetailsViewModel vm) vm.IsMapaPantallaCompleta = true;
    }

    private void CerrarPantallaCompleta_Clicked(object sender, System.EventArgs e)
    {
        if (BindingContext is TripDetailsViewModel vm) vm.IsMapaPantallaCompleta = false;
    }

    // Buscador Inteligente Reparado
    private void BuscadorPasajeros_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(query)) return;

        var coincidencia = MapaPantallaCompleta.Pins.FirstOrDefault(p =>
            (p.Label != null && p.Label.ToLowerInvariant().Contains(query)) ||
            (p.Address != null && p.Address.ToLowerInvariant().Contains(query)));

        if (coincidencia != null)
        {
            MapaPantallaCompleta.MoveToRegion(MapSpan.FromCenterAndRadius(coincidencia.Location, Distance.FromKilometers(0.3)));
        }
    }
}