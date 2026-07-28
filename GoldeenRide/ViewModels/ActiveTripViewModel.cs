using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GoldeenRide.ViewModels;

public partial class PasajeroAbordaje : ObservableObject
{
    public string IdReserva { get; set; } = string.Empty;
    public string NombreEstudiante { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string PuntoRecogida { get; set; } = string.Empty;
    public string? PuntoBajada { get; set; }

    public bool TieneBajadaAlterna => !string.IsNullOrEmpty(PuntoBajada);

    [ObservableProperty]
    private bool yaAbordo;
}

public partial class ActiveTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    // Inicialización simplificada sugerida por VS
    public ObservableCollection<PasajeroAbordaje> PasajerosDelViaje { get; } = [];

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string tituloRuta = string.Empty;

    public int PasajerosPendientes => PasajerosDelViaje.Count(p => !p.YaAbordo);

    public ActiveTripViewModel()
    {
        PasajerosDelViaje.CollectionChanged += (s, e) => OnPropertyChanged(nameof(PasajerosPendientes));
    }

    [RelayCommand]
    public async Task LoadTripDataAsync(string idAsignacion)
    {
        IsLoading = true;

        TituloRuta = "Ruta Norte - 6:30 AM";
        PasajerosDelViaje.Clear();

        PasajerosDelViaje.Add(new PasajeroAbordaje { IdReserva = "1", NombreEstudiante = "Ana López", PuntoRecogida = "Gasolinera Uno", YaAbordo = false });
        PasajerosDelViaje.Add(new PasajeroAbordaje { IdReserva = "2", NombreEstudiante = "Carlos Martínez", PuntoRecogida = "Metrocentro", PuntoBajada = "Edificio Administrativo", YaAbordo = false });

        foreach (var pasajero in PasajerosDelViaje)
        {
            pasajero.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PasajeroAbordaje.YaAbordo))
                {
                    OnPropertyChanged(nameof(PasajerosPendientes));
                }
            };
        }

        IsLoading = false;
    }

    // NUEVO COMANDO PARA REGRESAR ATRÁS (Soluciona el error de GoToAsync)
    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (Microsoft.Maui.Controls.Shell.Current != null)
        {
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    public async Task FinalizarViajeAsync()
    {
        if (PasajerosPendientes > 0)
        {
            bool confirmar = await Microsoft.Maui.Controls.Shell.Current.DisplayAlert(
                "Viaje Incompleto",
                $"Aún faltan {PasajerosPendientes} estudiantes por subir. ¿Seguro que deseas finalizar?",
                "Sí, Finalizar", "Cancelar");

            if (!confirmar) return;
        }

        await GoBackAsync();
    }
}