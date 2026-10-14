using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

using PassGold.Helpers;

namespace PassGold.ViewModels;

public partial class PasajeroDetalleViewModel : ObservableObject
{
    // Variables de la interfaz
    [ObservableProperty]
    private bool isMapExpanded = false;

    [ObservableProperty]
    private bool showDetailsPanel = true;

    // Datos del historial de la reserva
    [ObservableProperty]
    private string puntoSubida = AppResources.Global_Loading;

    [ObservableProperty]
    private string puntoBajada = AppResources.Global_Loading;

    [ObservableProperty]
    private string estadoReserva = AppResources.PaxDetail_Pending;

    public PasajeroDetalleViewModel()
    {
        // Aquí luego conectaremos con Supabase para jalar la reserva real
        CargarDatosPrueba();
    }

    private void CargarDatosPrueba()
    {
        PuntoSubida = "Universidad Nacional (UES)";
        PuntoBajada = "Metrocentro Santa Ana";
        EstadoReserva = AppResources.PaxDetail_BusOnTheWay;
    }

    [RelayCommand]
    public void ToggleMap()
    {
        IsMapExpanded = !IsMapExpanded;
        ShowDetailsPanel = !IsMapExpanded; // Oculta los detalles si el mapa se expande
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (App.Current?.MainPage != null)
            await App.Current.MainPage.Navigation.PopAsync();
    }
}