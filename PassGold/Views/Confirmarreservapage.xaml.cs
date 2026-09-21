using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using PassGold.ViewModels;
using PassGold.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PassGold.Views;

public partial class ConfirmarReservaPage : ContentPage
{
    private static readonly Location UbicacionDefault = new(13.9946, -89.5597);

    public ConfirmarReservaPage(List<OpcionViajePasajero> selecciones)
    {
        InitializeComponent();
        var vm = new ConfirmarReservaViewModel(selecciones);
        BindingContext = vm;

        MapaConfirmar.MoveToRegion(MapSpan.FromCenterAndRadius(UbicacionDefault, Distance.FromKilometers(2)));

        vm.PinesMapa.CollectionChanged += (s, e) =>
        {
            MapaConfirmar.Pins.Clear();
            foreach (var pin in vm.PinesMapa) MapaConfirmar.Pins.Add(pin);

            var primerPin = vm.PinesMapa.FirstOrDefault();
            if (primerPin != null)
            {
                MapaConfirmar.MoveToRegion(MapSpan.FromCenterAndRadius(primerPin.Location, Distance.FromKilometers(1)));
            }
        };
    }

    private void MapaConfirmar_MapClicked(object sender, MapClickedEventArgs e)
    {
        if (BindingContext is ConfirmarReservaViewModel vm)
        {
            vm.ProcesarToqueMapa(e.Location);
        }
    }

    //  Buscador restaurado — se me había olvidado al armar esta pantalla nueva.
    // Igual que en PasajeroReservaPage: solo mueve/acerca el mapa a la dirección
    // buscada, el usuario todavía tiene que tocar el punto exacto para fijar el pin
    // (casa o alterno, según cuál esté activo).
    private async void BuscadorConfirmar_SearchButtonPressed(object sender, System.EventArgs e)
    {
        var query = BuscadorConfirmar.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            var ubicacionEncontrada = await BuscarConReintentoAsync(query);

            if (ubicacionEncontrada != null)
            {
                MapaConfirmar.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacionEncontrada, Distance.FromKilometers(0.5)));
                BuscadorConfirmar.Unfocus();
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

    private async Task<Location?> BuscarConReintentoAsync(string query)
    {
        string busquedaExacta = $"{query}, El Salvador";
        var ubicaciones = await Geocoding.Default.GetLocationsAsync(busquedaExacta);
        var encontrada = ubicaciones?.FirstOrDefault();
        if (encontrada != null) return encontrada;

        string sinTildes = QuitarTildes(query);
        if (sinTildes != query)
        {
            var ubicaciones2 = await Geocoding.Default.GetLocationsAsync($"{sinTildes}, El Salvador");
            encontrada = ubicaciones2?.FirstOrDefault();
        }
        return encontrada;
    }

    private static string QuitarTildes(string texto)
    {
        var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalizado)
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}