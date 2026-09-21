using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using PassGold.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PassGold.Views;

public partial class SetHomeLocationPage : ContentPage
{
    private static readonly Location UbicacionDefault = new(13.9946, -89.5597);

    public SetHomeLocationPage()
    {
        InitializeComponent();
        var vm = new SetHomeLocationViewModel();
        BindingContext = vm;

        MapaCasa.MoveToRegion(MapSpan.FromCenterAndRadius(UbicacionDefault, Distance.FromKilometers(2)));

        vm.PinesMapa.CollectionChanged += (s, e) =>
        {
            MapaCasa.Pins.Clear();
            foreach (var pin in vm.PinesMapa) MapaCasa.Pins.Add(pin);
        };
    }

    private void MapaCasa_MapClicked(object sender, MapClickedEventArgs e)
    {
        if (BindingContext is SetHomeLocationViewModel vm)
        {
            vm.ProcesarToqueMapa(e.Location);
        }
    }

    private async void BuscadorCasa_SearchButtonPressed(object sender, System.EventArgs e)
    {
        var query = BuscadorCasa.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            var ubicacionEncontrada = await BuscarConReintentoAsync(query);

            if (ubicacionEncontrada != null)
            {
                MapaCasa.MoveToRegion(MapSpan.FromCenterAndRadius(ubicacionEncontrada, Distance.FromKilometers(0.5)));
                BuscadorCasa.Unfocus();
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