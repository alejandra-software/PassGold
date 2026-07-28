using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using GoldeenRide.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace GoldeenRide.ViewModels;

public class ViajeChoferCard
{
    public string IdAsignacion { get; set; } = "";
    public string HoraContextual { get; set; } = "";
    public string PlacaMicrobus { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string TipoViajeBadge { get; set; } = "";
    public Color ColorBadge { get; set; } = Colors.Transparent;
    public int OcupacionActual { get; set; }
    public int CapacidadMaxima { get; set; }
    public string FotoMicrobus { get; set; } = "";
}

public class PasajeroReserva
{
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public bool EsRecogida { get; set; }
}

public class BloqueViaje
{
    public string IdAsignacion { get; set; } = "";
    public string ChoferInfo { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string TipoViajeBadge { get; set; } = "";
    public Color ColorFondo { get; set; } = Colors.Transparent;
    public Color ColorTexto { get; set; } = Colors.White;
    public bool TieneViaje { get; set; } = false;
}

public class TramoHorario
{
    public string HoraFija { get; set; } = "";
    public ObservableCollection<BloqueViaje> ViajesDeLaSemana { get; set; } = new();
}

public partial class DriverDashboardViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private string driverName = AppResources.Dashboard_Loading;
    [ObservableProperty] private string fotoPerfilUsuario = "";
    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isIndependentDriver = false;
    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private bool canScheduleTrips = false;

    [ObservableProperty] private bool isTodayView = true;
    [ObservableProperty] private bool isWeeklyView = false;
    [ObservableProperty] private bool hasViajesHoy = false;
    [ObservableProperty] private bool isScheduleEmpty = true;
    [ObservableProperty] private bool isRefreshing = false;

    [ObservableProperty] private DateTime fechaInicioSemana;
    [ObservableProperty] private string textoRangoFechas = "";

    [ObservableProperty] private bool hasViajeEnCurso = false;

    public ObservableCollection<ViajeChoferCard> ViajesEnCursoAhora { get; } = new();

    [ObservableProperty] private ViajeChoferCard? proximoViajeHoy;
    [ObservableProperty] private bool hasProximoViajeHoy = false;

    public ObservableCollection<ViajeChoferCard> MisViajesHoy { get; } = new();
    public ObservableCollection<PasajeroReserva> PasajerosDelViaje { get; } = new();

    public ObservableCollection<string> CabecerasDias { get; } = new();
    public ObservableCollection<TramoHorario> TramosDelDia { get; } = new();

    private List<Asignacion> _asignacionesCacheadas = new();

    public DriverDashboardViewModel()
    {
        EstablecerSemanaActual();
    }

    private void EstablecerSemanaActual()
    {
        int diasOffset = (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diasOffset < 0) diasOffset += 7;
        FechaInicioSemana = DateTime.Today.AddDays(-diasOffset);
        ActualizarTextoFechas();
    }

    private void ActualizarTextoFechas()
    {
        DateTime finSemana = FechaInicioSemana.AddDays(6);
        TextoRangoFechas = $"{AppResources.Dashboard_From} {FechaInicioSemana:dd MMM} {AppResources.Dashboard_To} {finSemana:dd MMM, yyyy}";
    }

    [RelayCommand]
    public async Task AvanzarSemana()
    {
        FechaInicioSemana = FechaInicioSemana.AddDays(7);
        ActualizarTextoFechas();
        await LoadDriverDataAsync();
    }

    [RelayCommand]
    public async Task RetrocederSemana()
    {
        FechaInicioSemana = FechaInicioSemana.AddDays(-7);
        ActualizarTextoFechas();
        await LoadDriverDataAsync();
    }

    [RelayCommand]
    public async Task LoadDriverDataAsync()
    {
        IsScheduleEmpty = false;
        MisViajesHoy.Clear();
        PasajerosDelViaje.Clear();
        TramosDelDia.Clear();
        CabecerasDias.Clear();
        ViajesEnCursoAhora.Clear();
        HasViajeEnCurso = false;
        ProximoViajeHoy = null;
        HasProximoViajeHoy = false;

        try
        {
            var currentUser = _supabaseService.GetCurrentUser();
            if (currentUser == null) return;

            var userData = await _supabaseService.GetUserDataAsync(currentUser.Id);
            if (userData == null) return;

            DriverName = userData.Nombre;
            FotoPerfilUsuario = userData.FotoPerfil ?? "";

            string rolLower = userData.Rol.ToLower();
            IsFleetManager = rolLower.Contains("jefe") || rolLower.Contains("owner");
            IsIndependentDriver = rolLower.Contains("independiente");
            IsEmployee = rolLower.Contains("empleado");
            CanScheduleTrips = IsFleetManager || IsIndependentDriver;

            var dicChoferes = new Dictionary<string, string> { { userData.Id, userData.Nombre } };
            var dicVehiculos = new Dictionary<string, Vehiculo>();

            string idJefeParaFiltro = IsFleetManager ? currentUser.Id : (userData.IdJefe ?? currentUser.Id);

            var empleados = await _supabaseService.GetEmployeesByBossAsync(idJefeParaFiltro);
            foreach (var emp in empleados) dicChoferes[emp.Id] = emp.Nombre;

            var vehiculosFlota = await _supabaseService.GetVehiclesByOwnerAsync(idJefeParaFiltro);
            foreach (var v in vehiculosFlota) dicVehiculos[v.Id] = v;

            var idsFlota = dicChoferes.Keys.ToList();

            DateTime inicioReal = FechaInicioSemana.AddDays(-1);
            DateTime finReal = FechaInicioSemana.AddDays(7);

            var viajesBase = await _supabaseService.GetAllActiveTripsAsync(new List<string> { idJefeParaFiltro });
            var asignacionesSemana = await _supabaseService.GetAsignacionesPorRangoAsync(inicioReal, finReal, idsFlota);

            _asignacionesCacheadas = asignacionesSemana;

            var asigSemanaEstricta = asignacionesSemana
                .Where(a => a.Fecha.ToLocalTime().Date >= FechaInicioSemana.Date && a.Fecha.ToLocalTime().Date <= FechaInicioSemana.AddDays(6).Date)
                .ToList();

            IsScheduleEmpty = asigSemanaEstricta.Count == 0;

            if (!IsScheduleEmpty)
            {
                DateTime ahoraLocal = DateTime.Now;

                var asignacionesHoy = asigSemanaEstricta
                    .Where(a => a.Fecha.ToLocalTime().Date == ahoraLocal.Date && (IsFleetManager || a.IdChofer == currentUser.Id))
                    .OrderBy(a => a.Fecha)
                    .ToList();

                foreach (var asig in asignacionesHoy)
                {
                    var viajePlantilla = viajesBase.FirstOrDefault(v => v.Id == asig.IdViaje);
                    if (viajePlantilla == null) continue;

                    bool esIda = viajePlantilla.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);

                    // Ahora que SupabaseService nos garantiza que la hora NO se restará el doble
                    string horaExt = asig.Fecha.ToLocalTime().ToString("hh:mm tt");
                    var vehiculo = dicVehiculos.TryGetValue(asig.IdVehiculo, out var veh) ? veh : new Vehiculo { Placa = "N/A", Capacidad = 0 };

                    MisViajesHoy.Add(new ViajeChoferCard
                    {
                        IdAsignacion = asig.Id,
                        HoraContextual = horaExt,
                        PlacaMicrobus = vehiculo.Placa,
                        Ruta = viajePlantilla.RutaGeneral,
                        TipoViajeBadge = esIda ? AppResources.TripType_Inbound : AppResources.TripType_Outbound,
                        ColorBadge = esIda ? Color.FromArgb("#4A90E2") : Color.FromArgb("#F5A623"),
                        OcupacionActual = 0,
                        CapacidadMaxima = vehiculo.Capacidad
                    });
                }

                HasViajesHoy = MisViajesHoy.Count > 0;

                if (HasViajesHoy)
                {
                    var enCursoAhora = new List<Asignacion>();
                    Asignacion? proximaSinEmpezar = null;

                    foreach (var asig in asignacionesHoy)
                    {
                        var viajePlantilla = viajesBase.FirstOrDefault(v => v.Id == asig.IdViaje);
                        if (viajePlantilla == null) continue;

                        DateTime fechaDelDia = asig.Fecha.ToLocalTime().Date;
                        DateTime ventanaInicio, ventanaFin;

                        if (viajePlantilla.HoraInicioRecorrido.HasValue && viajePlantilla.HoraLlegadaDestino.HasValue)
                        {
                            ventanaInicio = fechaDelDia.Add(viajePlantilla.HoraInicioRecorrido.Value);
                            ventanaFin = fechaDelDia.Add(viajePlantilla.HoraLlegadaDestino.Value);
                        }
                        else
                        {
                            ventanaInicio = asig.Fecha.ToLocalTime();
                            ventanaFin = ventanaInicio.AddHours(2);
                        }

                        if (ahoraLocal >= ventanaInicio && ahoraLocal <= ventanaFin)
                        {
                            enCursoAhora.Add(asig);
                        }
                        else if (ahoraLocal < ventanaInicio && (proximaSinEmpezar == null || ventanaInicio < proximaSinEmpezar.Fecha.ToLocalTime()))
                        {
                            proximaSinEmpezar = asig;
                        }
                    }

                    foreach (var asig in enCursoAhora)
                    {
                        var card = MisViajesHoy.FirstOrDefault(v => v.IdAsignacion == asig.Id);
                        if (card != null) ViajesEnCursoAhora.Add(card);
                    }

                    HasViajeEnCurso = ViajesEnCursoAhora.Count > 0;

                    if (HasViajeEnCurso)
                    {
                        CargarPasajerosFalsos();
                    }
                    else if (proximaSinEmpezar != null)
                    {
                        ProximoViajeHoy = MisViajesHoy.FirstOrDefault(v => v.IdAsignacion == proximaSinEmpezar.Id);
                        HasProximoViajeHoy = ProximoViajeHoy != null;
                    }
                }

                ConstruirCalendarioMatricial(asigSemanaEstricta, viajesBase, dicChoferes);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cargando dashboard: {ex.Message}");
        }
    }

    private void ConstruirCalendarioMatricial(List<Asignacion> asignaciones, List<Viaje> viajesBase, Dictionary<string, string> choferesNombres)
    {
        CabecerasDias.Clear();
        var abrevDias = new[] { "Lu", "Ma", "Mi", "Ju", "Vi", "Sa", "Do" };

        for (int i = 0; i < 7; i++)
        {
            DateTime fechaColumna = FechaInicioSemana.AddDays(i);
            CabecerasDias.Add($"{abrevDias[i]} {fechaColumna.Day}");
        }

        var asignacionesConViaje = asignaciones
            .Select(a => new { Asig = a, ViajeInfo = viajesBase.FirstOrDefault(v => v.Id == a.IdViaje) })
            .Where(x => x.ViajeInfo != null)
            .ToList();

        var agrupadoPorViaje = asignacionesConViaje
            .GroupBy(x => x.ViajeInfo!.Id)
            .OrderBy(g => g.First().ViajeInfo!.HoraSalida.ToLocalTime().TimeOfDay);

        foreach (var grupoViaje in agrupadoPorViaje)
        {
            var viajeInfo = grupoViaje.First().ViajeInfo!;
            string horaEtiqueta = viajeInfo.HoraSalida.ToLocalTime().ToString("hh:mm tt");

            var tramo = new TramoHorario { HoraFija = $"{horaEtiqueta}\n{viajeInfo.RutaGeneral}" };

            for (int i = 0; i < 7; i++)
            {
                DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                var asigDelDia = grupoViaje.FirstOrDefault(x => x.Asig.Fecha.ToLocalTime().Date == fechaEvaluar);

                if (asigDelDia != null)
                {
                    bool esIda = asigDelDia.ViajeInfo!.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);
                    Color bgColor = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0");
                    string nombreChofer = choferesNombres.TryGetValue(asigDelDia.Asig.IdChofer, out var nom) ? nom : AppResources.Dashboard_Unknown;

                    tramo.ViajesDeLaSemana.Add(new BloqueViaje
                    {
                        IdAsignacion = asigDelDia.Asig.Id,
                        ChoferInfo = nombreChofer,
                        Ruta = asigDelDia.ViajeInfo.RutaGeneral,
                        ColorFondo = bgColor,
                        TieneViaje = true,
                        ColorTexto = Colors.White,
                        TipoViajeBadge = esIda ? "IDA" : "REG"
                    });
                }
                else
                {
                    tramo.ViajesDeLaSemana.Add(new BloqueViaje { TieneViaje = false });
                }
            }
            TramosDelDia.Add(tramo);
        }
    }

    private void CargarPasajerosFalsos()
    {
        PasajerosDelViaje.Clear();
        PasajerosDelViaje.Add(new PasajeroReserva { Nombre = "Ana López", Ubicacion = "Gasolinera Norte", EsRecogida = true });
        PasajerosDelViaje.Add(new PasajeroReserva { Nombre = "Carlos Gómez", Ubicacion = "Edificio Administrativo", EsRecogida = false });
    }

    [RelayCommand]
    public void ToggleMainView(string vista)
    {
        IsTodayView = vista == "Hoy";
        IsWeeklyView = vista == "Semana";
    }

    [RelayCommand]
    public async Task RefreshDashboardAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        try
        {
            await LoadDriverDataAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task VerDetalleViajeAsync(string idAsignacion)
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync($"trip-details?idAsignacion={idAsignacion}");
    }

    [RelayCommand]
    public async Task NavegarAEdicionAsync(string idAsignacion)
    {
        if (!CanScheduleTrips)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(AppResources.Global_Attention, "Solo el jefe de la flota puede editar o eliminar viajes.", AppResources.Global_Ok);
            return;
        }

        var asig = _asignacionesCacheadas.FirstOrDefault(a => a.Id == idAsignacion);
        if (asig != null && Shell.Current != null)
        {
            var navParams = new Dictionary<string, object> { { "AsignacionSeleccionada", asig } };
            await Shell.Current.GoToAsync("edit-trip", navParams);
        }
    }

    [RelayCommand]
    public void OpenSidebar()
    {
        if (Shell.Current != null) Shell.Current.FlyoutIsPresented = true;
    }
}