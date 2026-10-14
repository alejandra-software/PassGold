using PassGold.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace PassGold.Views;

public partial class MapaRastreoPage : ContentPage
{
    private bool _primerAjuste = true;

    public MapaRastreoPage()
    {
        InitializeComponent();
    }

    // 🛰️ Normal ⇄ satélite
    private void CambiarTipoMapa_Clicked(object? sender, EventArgs e)
    {
        MapaChofer.MapType = MapaChofer.MapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Mientras no llegue la primera señal, mostramos Santa Ana de fondo
        if (_primerAjuste)
        {
            var centroElSalvador = new Location(13.9946, -89.5597);
            MapaChofer.MoveToRegion(MapSpan.FromCenterAndRadius(centroElSalvador, Distance.FromKilometers(10)));
        }

        if (BindingContext is MapaRastreoViewModel vm)
        {
            // Primero se quita por si ya estaba (evita recibir cada cambio duplicado)
            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.PropertyChanged += Vm_PropertyChanged;
            await vm.InicializarMapaCommand.ExecuteAsync(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is MapaRastreoViewModel vm)
        {
            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.DetenerRastreo(); // Detenemos el temporizador para ahorrar batería y datos
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MapaRastreoViewModel.PosicionChofer)) return;
        if (BindingContext is not MapaRastreoViewModel vm || vm.PosicionChofer == null) return;

        var pos = vm.PosicionChofer;

        // El pin se vuelve a crear en cada señal: es más confiable que mover el
        // existente, y se comporta igual en Android y en iPhone.
        MapaChofer.Pins.Clear();
        MapaChofer.Pins.Add(new Pin
        {
            Label = PassGold.Helpers.AppResources.Track_YourRidePin,
            Type = PinType.Generic,
            Location = pos
        });

        // Seguimos al micro, pero respetando el zoom que el pasajero haya elegido
        var visible = MapaChofer.VisibleRegion;
        double latGrados = (_primerAjuste || visible == null) ? 0.01 : visible.LatitudeDegrees;
        double lonGrados = (_primerAjuste || visible == null) ? 0.01 : visible.LongitudeDegrees;
        MapaChofer.MoveToRegion(new MapSpan(pos, latGrados, lonGrados));
        _primerAjuste = false;
    }
}