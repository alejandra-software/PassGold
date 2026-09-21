using PassGold.ViewModels;
using PassGold.Helpers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Linq;

namespace PassGold.Views;

public partial class TripDetailsPage : ContentPage
{
    // Mismos colores que ya usa el badge IDA/REG en el resto de la app.
    private const string ColorRecogida = "#2E7D32";
    private const string ColorBajada = "#1565C0";

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
                || e.PropertyName == nameof(TripDetailsViewModel.IsMapaSubTabRecogida))
                RedibujarPines();
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

    // 🔧 Los pines de pasajeros ahora llevan su nombre escrito en una burbuja
    // (verde = punto de recogida, azul = punto de bajada) generada con
    // PinImageGenerator — antes solo mostraban un ícono genérico 👤 y había
    // que tocar cada uno para ver de quién era.
    private void RedibujarPines()
    {
        if (BindingContext is not TripDetailsViewModel vm) return;

        MapaRutaEnVivo.Pins.Clear();
        MapaPantallaCompleta.Pins.Clear();

        if (vm.MetaLatitud.HasValue && vm.MetaLongitud.HasValue)
        {
            var pinMeta = new Pin
            {
                Label = string.IsNullOrEmpty(vm.MetaTexto) ? AppResources.TripDetails_DriverMeta : $"🚩 {vm.MetaTexto}",
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
                        Label = $"👤 {p.Nombre}",
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
                        Label = $"👤 {p.Nombre}",
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

    private void BuscadorPasajeros_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(query)) return;

        var coincidencia = MapaPantallaCompleta.Pins.FirstOrDefault(p =>
            (p.Label?.ToLowerInvariant().Contains(query) ?? false) ||
            (p.Address?.ToLowerInvariant().Contains(query) ?? false));

        if (coincidencia != null)
        {
            MapaPantallaCompleta.MoveToRegion(MapSpan.FromCenterAndRadius(coincidencia.Location, Distance.FromKilometers(0.4)));
        }
    }

    // 🆕 Tocar la foto en la lista de "a bordo" la abre en grande (mismo visor
    // con zoom que ya se usa en Settings y en ActiveTripPage) — para cuando dos
    // pasajeros se parecen mucho de lejos, según reportó la usuaria.
    private async void FotoPasajero_Tapped(object sender, EventArgs e)
    {
        if (sender is Element el && el.BindingContext is PasajeroItem pasajero && !string.IsNullOrEmpty(pasajero.FotoUrl))
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(pasajero.FotoUrl));
        }
    }

    // 🆕 Tocar el resto de la fila (nombre/ubicación) abre la ficha completa del
    // pasajero — foto grande, teléfono con botón de llamar directo. Motivado por
    // que los pasajeros pierden la hora seguido y el chofer necesita poder
    // llamarlos sin salir de la app ni buscar el número a mano.
    private async void Pasajero_Tapped(object sender, EventArgs e)
    {
        if (sender is Element el && el.BindingContext is PasajeroItem pasajero)
        {
            await Navigation.PushModalAsync(new PasajeroDetallePage(pasajero));
        }
    }
}