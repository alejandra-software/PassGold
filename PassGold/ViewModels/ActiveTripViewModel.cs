using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Services;
using PassGold.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace PassGold.ViewModels;

public partial class PasajeroAbordaje : ObservableObject
{
    public string IdReserva { get; set; } = string.Empty;
    public string NombreEstudiante { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string PuntoRecogida { get; set; } = string.Empty;
    public string? PuntoBajada { get; set; }

    public bool TieneBajadaAlterna => !string.IsNullOrEmpty(PuntoBajada);

    //  La foto del pasajero nunca se mostraba en ActiveTripPage — el círculo
    // del avatar tenía el ícono 👤 fijo, sin bindear a FotoUrl para nada.
    public bool TieneFoto => !string.IsNullOrEmpty(FotoUrl);

    [ObservableProperty]
    private bool yaAbordo;
}

[QueryProperty(nameof(IdAsignacion), "idAsignacion")]
public partial class ActiveTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    public ObservableCollection<PasajeroAbordaje> PasajerosDelViaje { get; } = [];

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string tituloRuta = string.Empty;

    public int PasajerosPendientes => PasajerosDelViaje.Count(p => !p.YaAbordo);

    private string _idAsignacion = "";
    public string IdAsignacion
    {
        get => _idAsignacion;
        set
        {
            if (SetProperty(ref _idAsignacion, value) && !string.IsNullOrEmpty(value))
            {
                _ = LoadTripDataAsync(value);
            }
        }
    }

    public ActiveTripViewModel()
    {
        PasajerosDelViaje.CollectionChanged += (s, e) => OnPropertyChanged(nameof(PasajerosPendientes));
    }

    [RelayCommand]
    public async Task LoadTripDataAsync(string idAsignacion)
    {
        if (string.IsNullOrEmpty(idAsignacion) || idAsignacion == "id_temporal") return;

        IsLoading = true;
        PasajerosDelViaje.Clear();

        var asignacion = await _supabaseService.GetAsignacionByIdAsync(idAsignacion);
        if (asignacion == null) { IsLoading = false; return; }

        var viajesActivos = await _supabaseService.GetAllActiveTripsAsync();
        var viaje = viajesActivos.FirstOrDefault(v => v.Id == asignacion.IdViaje);
        string horaTxt = viaje != null ? viaje.HoraSalida.ToLocalTime().ToString("hh:mm tt") : "";
        TituloRuta = viaje != null ? $"{viaje.TipoViaje} - {horaTxt}" : AppResources.TripDetails_Title;

        var reservas = await _supabaseService.GetReservasByAsignacionAsync(idAsignacion);

        foreach (var reserva in reservas)
        {
            var pasajero = await _supabaseService.GetUserDataAsync(reserva.IdPasajero);
            var item = new PasajeroAbordaje
            {
                IdReserva = reserva.Id,
                NombreEstudiante = pasajero?.Nombre ?? AppResources.Dashboard_Unknown,
                FotoUrl = pasajero?.FotoPerfil,
                PuntoRecogida = reserva.PuntoRecogidaTexto,
                PuntoBajada = reserva.PuntoBajadaTexto,
                YaAbordo = reserva.Estado == "a_bordo"
            };
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PasajeroAbordaje.YaAbordo))
                    OnPropertyChanged(nameof(PasajerosPendientes));
            };
            PasajerosDelViaje.Add(item);
        }

        IsLoading = false;
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (Shell.Current != null) await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task FinalizarViajeAsync()
    {
        if (PasajerosPendientes > 0)
        {
            bool confirmar = await Shell.Current!.DisplayAlert(
                AppResources.ActiveTrip_Incomplete,
                string.Format(AppResources.ActiveTrip_MissingStudents, PasajerosPendientes),
                AppResources.ActiveTrip_YesFinish, AppResources.Global_Cancel);

            if (!confirmar) return;
        }

        await GoBackAsync();
    }
}