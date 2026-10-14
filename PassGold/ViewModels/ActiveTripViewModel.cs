using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System;
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
    public bool TieneFoto => !string.IsNullOrEmpty(FotoUrl);

    [ObservableProperty]
    private bool yaAbordo;
}

[QueryProperty(nameof(IdAsignacion), "idAsignacion")]
public partial class ActiveTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly LiveTrackingService _rastreo = LiveTrackingService.Instance;

    // Se guardan al cargar, para calcular la ventana de rastreo al iniciar el viaje
    private Asignacion? _asignacion;
    private Viaje? _viaje;
    private bool _escuchandoProblemas = false;

    public ObservableCollection<PasajeroAbordaje> PasajerosDelViaje { get; } = [];

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string tituloRuta = string.Empty;
    [ObservableProperty] private bool isViajeEnCurso = false;
    // Texto bajo los botones: "✅ Ubicación enviada a las 14:05:12", "❌ No se pudo enviar…"
    [ObservableProperty] private string estadoRastreo = string.Empty;

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
        System.Diagnostics.Debug.WriteLine($"[RASTREO] 2. Abrió la pantalla del viaje. idAsignacion='{idAsignacion}'");
        if (string.IsNullOrEmpty(idAsignacion) || idAsignacion == "id_temporal") return;

        IsLoading = true;
        PasajerosDelViaje.Clear();

        try
        {
            _asignacion = await _supabaseService.GetAsignacionByIdAsync(idAsignacion);
            if (_asignacion == null) return;

            // Antes se bajaban TODOS los viajes activos para buscar uno; ahora se pide solo ese.
            _viaje = await _supabaseService.GetTripByIdAsync(_asignacion.IdViaje);

            // Antes usaba .ToLocalTime(), que en un celular de El Salvador le restaba
            // 6 horas a una hora que ya venía en hora local. Regla de la app: .AHoraElSalvador()
            string horaTxt = _viaje != null ? _viaje.HoraSalida.AHoraElSalvador().ToString("hh:mm tt", AppResources.Culture) : "";
            TituloRuta = _viaje != null ? $"{AppResources.TripTypeLabel(_viaje.TipoViaje)} - {horaTxt}" : AppResources.TripDetails_Title;

            // Si el chofer salió de esta pantalla con el viaje en marcha y vuelve a entrar,
            // el rastreo sigue corriendo: la pantalla tiene que mostrarlo así.
            IsViajeEnCurso = _rastreo.EstaCompartiendo && _rastreo.IdAsignacionActiva == idAsignacion;
            if (IsViajeEnCurso)
            {
                EscucharProblemas();
                EstadoRastreo = _rastreo.EstadoTexto;
            }

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
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "ActiveTripViewModel.LoadTripDataAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task IniciarViaje()
    {
        System.Diagnostics.Debug.WriteLine($"[RASTREO] 3. Tocó Iniciar Viaje. idAsignacion='{IdAsignacion}', CompartirActivado={LiveTrackingService.CompartirActivado}");
        // Interruptor "Compartir mi ubicación" de Ajustes apagado: antes el viaje
        // iniciaba en silencio sin GPS y nadie se enteraba. Ahora se le pregunta al chofer.
        if (!LiveTrackingService.CompartirActivado)
        {
            bool activar = Shell.Current != null && await Shell.Current.DisplayAlert(
                AppResources.LiveTrack_ShareOffTitle,
                AppResources.LiveTrack_ShareOffBody,
                AppResources.LiveTrack_ShareOffYes,
                AppResources.LiveTrack_ShareOffNo);

            if (!activar)
            {
                IsViajeEnCurso = true;
                EstadoRastreo = AppResources.LiveTrack_StatusOff;
                return;
            }
            LiveTrackingService.CompartirActivado = true; // queda encendido también en Ajustes
        }

        IsViajeEnCurso = true;

        try
        {
            EscucharProblemas();

            // Antes, esta pantalla guardaba la posición directo en la tabla, sin pasar
            // por la Edge Function: la posición se veía, pero los avisos de cercanía
            // NUNCA se disparaban. Ahora todo pasa por LiveTrackingService.
            // Si el permiso de ubicación falta, el servicio lo pide y avisa por el evento Problema.
            var finVentana = CalcularFinDeVentana();
            bool arranco = await _rastreo.IniciarAsync(IdAsignacion, DateTime.Now, finVentana);
            System.Diagnostics.Debug.WriteLine($"[RASTREO] 4. IniciarAsync devolvió {arranco} (fin de ventana {finVentana})");
            if (!arranco) EstadoRastreo = AppResources.LiveTrack_StatusOff;
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "ActiveTripViewModel.IniciarViaje");
        }
    }

    // Hasta cuándo seguir compartiendo si el chofer olvida tocar "Finalizar".
    // Hora de llegada del viaje + 15 min, pero nunca menos de 1 h ni más de 4 h desde
    // ahora (por si va atrasado o abrió un viaje de otro día).
    private DateTime CalcularFinDeVentana()
    {
        DateTime ahora = DateTime.Now;
        DateTime fin = ahora.AddHours(2);

        if (_asignacion != null && _viaje != null)
        {
            DateTime dia = _asignacion.Fecha.AHoraElSalvador().Date;
            TimeSpan horaInicio = _viaje.HoraInicioRecorrido ?? _viaje.HoraSalida.AHoraElSalvador().TimeOfDay;
            TimeSpan horaFin = _viaje.HoraLlegadaDestino ?? horaInicio.Add(TimeSpan.FromHours(2));

            fin = dia.Add(horaFin).AddMinutes(15);
            if (horaFin < horaInicio) fin = fin.AddDays(1); // el viaje cruza la medianoche
        }

        if (fin < ahora.AddHours(1)) fin = ahora.AddHours(1);
        if (fin > ahora.AddHours(4)) fin = ahora.AddHours(4);
        return fin;
    }

    private void EscucharProblemas()
    {
        if (_escuchandoProblemas) return;
        _rastreo.Problema += Rastreo_Problema;
        _rastreo.EstadoTextoCambio += Rastreo_Estado;
        _escuchandoProblemas = true;
    }

    private void DejarDeEscucharProblemas()
    {
        if (!_escuchandoProblemas) return;
        _rastreo.Problema -= Rastreo_Problema;
        _rastreo.EstadoTextoCambio -= Rastreo_Estado;
        _escuchandoProblemas = false;
    }

    private void Rastreo_Estado(string texto) => EstadoRastreo = texto;

    // El servicio ya dispara este evento en el hilo principal
    private async void Rastreo_Problema(string mensaje)
    {
        try
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(AppResources.Global_Attention, mensaje, AppResources.Global_Ok);
        }
        catch { }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        // Ojo: volver atrás NO apaga el rastreo. Si el chofer va al inicio a mitad del
        // viaje, los pasajeros lo siguen viendo. Se apaga con "Finalizar", al cerrar
        // sesión o sola al terminar la ventana del viaje.
        DejarDeEscucharProblemas();

        if (Shell.Current != null) await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task FinalizarViajeAsync()
    {
        // Marcar a los pasajeros es OPCIONAL: el chofer va manejando. Antes, si no
        // marcaba a todos, se le exigía confirmar "Aún faltan X estudiantes".
        // Ahora solo se confirma una vez, para que un toque accidental no apague el GPS.
        if (Shell.Current != null)
        {
            bool confirmar = await Shell.Current.DisplayAlert(
                AppResources.ActiveTrip_FinishConfirmTitle,
                AppResources.ActiveTrip_FinishConfirmBody,
                AppResources.ActiveTrip_YesFinish, AppResources.Global_Cancel);

            if (!confirmar) return;
        }

        LiveTrackingService.MarcarFinalizadaManual(IdAsignacion); // que el arranque automático no lo reencienda
        if (_rastreo.IdAsignacionActiva == IdAsignacion) _rastreo.Detener();
        IsViajeEnCurso = false;
        EstadoRastreo = string.Empty;

        await GoBackAsync();
    }
}