using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using PassGold.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PassGold.Views;

public partial class PasajeroReservaPage : ContentPage
{
    private static readonly Location UbicacionDefault = new(13.9946, -89.5597);

    public PasajeroReservaPage()
    {
        InitializeComponent();
        var vm = new PasajeroReservaViewModel();
        BindingContext = vm;

        MapaPasajero.MoveToRegion(MapSpan.FromCenterAndRadius(UbicacionDefault, Distance.FromKilometers(2)));

        vm.PinesMapa.CollectionChanged += (s, e) =>
        {
            MapaPasajero.Pins.Clear();
            foreach (var pin in vm.PinesMapa) MapaPasajero.Pins.Add(pin);

            var pinOficial = vm.PinesMapa.FirstOrDefault(p => p.Type == PinType.Place);
            if (pinOficial != null)
            {
                MapaPasajero.MoveToRegion(MapSpan.FromCenterAndRadius(pinOficial.Location, Distance.FromKilometers(1)));
            }
        };
    }

    private void MapaPasajero_MapClicked(object sender, MapClickedEventArgs e)
    {
        if (BindingContext is PasajeroReservaViewModel vm)
        {
            vm.ProcesarToqueMapa(e.Location);
        }
    }

    private async void BuscadorPasajero_SearchButtonPressed(object sender, System.EventArgs e)
    {
        var query = BuscadorPasajero.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            var ubicacionEncontrada = await BuscarConReintentoAsync(query);

            if (ubicacionEncontrada != null)
            {
                MapaPasajero.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacionEncontrada, Distance.FromKilometers(0.5)));
                BuscadorPasajero.Unfocus();
            }
            else
            {
                await DisplayAlert(PassGold.Helpers.AppResources.Global_NotFound, PassGold.Helpers.AppResources.Schedule_MapErrorLocation, PassGold.Helpers.AppResources.Global_Ok);
            }
        }
        catch (System.Exception)
        {
            await DisplayAlert(PassGold.Helpers.AppResources.Global_Error, PassGold.Helpers.AppResources.Global_NoInternetSearch, PassGold.Helpers.AppResources.Global_Ok);
        }
    }

    // Buscador compartido: siglas (Unicaes, UES...), sin tildes, Apple Maps en iPhone
    // y filtro para que no devuelva lugares fuera del país o el "centro del país".
    private Task<Location?> BuscarConReintentoAsync(string query)
        => PassGold.Helpers.BuscadorLugares.BuscarAsync(query, MapaPasajero.VisibleRegion?.Center);
}