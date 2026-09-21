using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Microsoft.Maui.Networking;
using PassGold.Models.Local;
using Microsoft.Extensions.DependencyInjection;

namespace PassGold.ViewModels;

public class DriverCachePayload
{
    public Usuario UserData { get; set; } = new();
    public List<Usuario> Empleados { get; set; } = new();
    public List<Vehiculo> Vehiculos { get; set; } = new();
    public List<Viaje> ViajesBase { get; set; } = new();
    public List<Asignacion> AsignacionesSemana { get; set; } = new();
}

public class ViajeChoferCard
{
    public string IdAsignacion { get; set; } = "";
    public string HoraContextual { get; set; } = "";
    public string PlacaMicrobus { get; set; } = "";
    public string Ruta { get; set; } = "";
    public string TipoViajeBadge { get; set; } = "";
    public Color ColorBadge { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    public int OcupacionActual { get; set; }
    public int CapacidadMaxima { get; set; }
}

public class PasajeroReserva { public string Nombre { get; set; } = ""; public string Ubicacion { get; set; } = ""; }

public class OpcionViajeChofer
{
    public string IdAsignacion { get; set; } = "";
    public string ChoferInfo { get; set; } = "";
    public string TipoViajeBadge { get; set; } = "";
    public Color ColorFondo { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
}

public class BloqueViaje
{
    public bool TieneViaje => Opciones.Count > 0;
    public ObservableCollection<OpcionViajeChofer> Opciones { get; set; } = new();
}

public class TramoHorario
{
    public string HoraFija { get; set; } = "";
    public ObservableCollection<BloqueViaje> ViajesDeLaSemana { get; set; } = new();
}

public partial class DriverDashboardViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private LocalDatabaseService GetLocalDb() => Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private bool canScheduleTrips = false;
    [ObservableProperty] private bool isTodayView = true;
    [ObservableProperty] private bool isWeeklyView = false;
    [ObservableProperty] private bool hasViajesHoy = false;
    [ObservableProperty] private bool isScheduleEmpty = true;
    [ObservableProperty] private bool isRefreshing = false;
    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private DateTime fechaInicioSemana;
    [ObservableProperty] private string textoRangoFechas = "";
    [ObservableProperty] private bool hasViajeEnCurso = false;
    // FIX: el avatar del sidebar bindeaba a "FotoPerfilUsuario", una propiedad
    // que nunca existió en este ViewModel — el dato del usuario sí se descargaba
    // (payload.UserData.FotoPerfil), pero nunca se copiaba a nada visible por la
    // vista. Por eso el avatar quedaba vacío siempre, no a veces.
    [ObservableProperty] private string fotoPerfilUsuario = "";
    [ObservableProperty] private bool hasFotoPerfilUsuario = false;

    partial void OnFotoPerfilUsuarioChanged(string value) => HasFotoPerfilUsuario = !string.IsNullOrWhiteSpace(value);

    public ObservableCollection<ViajeChoferCard> ViajesEnCursoAhora { get; } = new();
    [ObservableProperty] private ViajeChoferCard? proximoViajeHoy;
    [ObservableProperty] private bool hasProximoViajeHoy = false;

    public ObservableCollection<PasajeroReserva> PasajerosDelViaje { get; } = new();
    public ObservableCollection<string> CabecerasDias { get; } = new();
    public ObservableCollection<TramoHorario> TramosDelDia { get; } = new();

    private List<Asignacion> _asignacionesCacheadas = new();

    public DriverDashboardViewModel() { EstablecerSemanaActual(); }

    private void EstablecerSemanaActual()
    {
        int diasOffset = (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diasOffset < 0) diasOffset += 7;
        FechaInicioSemana = DateTime.Today.AddDays(-diasOffset);
        ActualizarTextoFechas();
    }
    private void ActualizarTextoFechas() { TextoRangoFechas = $"{AppResources.Dashboard_From} {FechaInicioSemana:dd MMM} {AppResources.Dashboard_To} {FechaInicioSemana.AddDays(6):dd MMM, yyyy}"; }

    [RelayCommand] public async Task AvanzarSemana() { FechaInicioSemana = FechaInicioSemana.AddDays(7); ActualizarTextoFechas(); await LoadDriverDataAsync(); }
    [RelayCommand] public async Task RetrocederSemana() { FechaInicioSemana = FechaInicioSemana.AddDays(-7); ActualizarTextoFechas(); await LoadDriverDataAsync(); }

    [RelayCommand] public void ToggleMainView(string vista) { IsTodayView = vista == "Hoy"; IsWeeklyView = vista == "Semana"; }

    [RelayCommand]
    public async Task LoadDriverDataAsync()
    {
        IsLoading = true; IsScheduleEmpty = false;
        var localDb = GetLocalDb();
        string currentUserId = "";

        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await localDb.ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (string.IsNullOrEmpty(currentUserId)) { IsLoading = false; return; }
        string cacheKey = $"DriverData_{currentUserId}_{FechaInicioSemana:yyyyMMdd}";

        try
        {
            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                var payload = JsonConvert.DeserializeObject<DriverCachePayload>(cacheJson, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
                if (payload != null) Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() => ProcesarDatosChofer(payload, currentUserId));
            }
        }
        catch { }

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var userData = await _supabaseService.GetUserDataAsync(currentUserId);
                    if (userData == null) return;

                    var nuevoPayload = new DriverCachePayload { UserData = userData };
                    string rolLower = userData.Rol.ToLower();
                    bool esJefe = rolLower.Contains("jefe") || rolLower.Contains("owner");
                    string idJefeParaFiltro = esJefe ? currentUserId : (userData.IdJefe ?? currentUserId);

                    nuevoPayload.Empleados = await _supabaseService.GetEmployeesByBossAsync(idJefeParaFiltro);
                    nuevoPayload.Vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(idJefeParaFiltro);
                    nuevoPayload.ViajesBase = await _supabaseService.GetAllActiveTripsAsync(new List<string> { idJefeParaFiltro });

                    // FIX: normalizar Kind=Utc en HoraSalida (igual que ya se hace con Asignacion.Fecha),
                    // para que ToLocalTime() convierta de verdad y no se quede como no-op.
                    foreach (var v in nuevoPayload.ViajesBase)
                    {
                        if (v.HoraSalida.Kind == DateTimeKind.Unspecified)
                            v.HoraSalida = DateTime.SpecifyKind(v.HoraSalida, DateTimeKind.Utc);
                    }

                    var dicChoferes = new Dictionary<string, string> { { userData.Id, userData.Nombre } };
                    foreach (var emp in nuevoPayload.Empleados) dicChoferes[emp.Id] = emp.Nombre;

                    DateTime inicioReal = FechaInicioSemana.AddDays(-1);
                    DateTime finReal = FechaInicioSemana.AddDays(7);

                    // Asegurarnos de que baje la data fresca directo del servidor
                    nuevoPayload.AsignacionesSemana = await _supabaseService.GetAsignacionesPorRangoAsync(inicioReal, finReal, dicChoferes.Keys.ToList());

                    await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevoPayload, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc }));
                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() => ProcesarDatosChofer(nuevoPayload, currentUserId));
                }
                catch { }
            });
        }
        IsLoading = false;
    }

    private void ProcesarDatosChofer(DriverCachePayload payload, string currentUserId)
    {
        ViajesEnCursoAhora.Clear(); PasajerosDelViaje.Clear(); TramosDelDia.Clear(); CabecerasDias.Clear();
        HasViajeEnCurso = false; ProximoViajeHoy = null; HasProximoViajeHoy = false; HasViajesHoy = false;

        CanScheduleTrips = payload.UserData.Rol.ToLower().Contains("jefe") || payload.UserData.Rol.ToLower().Contains("independiente");
        FotoPerfilUsuario = payload.UserData.FotoPerfil ?? "";

        var dicChoferes = new Dictionary<string, string> { { payload.UserData.Id, payload.UserData.Nombre } };
        foreach (var emp in payload.Empleados) dicChoferes[emp.Id] = emp.Nombre;
        _asignacionesCacheadas = payload.AsignacionesSemana;

        var asigSemanaEstricta = payload.AsignacionesSemana.Where(a => a.Fecha.AHoraElSalvador().Date >= FechaInicioSemana.Date && a.Fecha.AHoraElSalvador().Date <= FechaInicioSemana.AddDays(6).Date).ToList();
        IsScheduleEmpty = asigSemanaEstricta.Count == 0;

        if (!IsScheduleEmpty)
        {
            ConstruirCalendarioMatricial(asigSemanaEstricta, payload.ViajesBase, dicChoferes);

            var asignacionesHoy = asigSemanaEstricta.Where(a => a.Fecha.AHoraElSalvador().Date == DateTime.Today).ToList();
            if (asignacionesHoy.Count > 0)
            {
                HasViajesHoy = true;
                TimeSpan horaActual = DateTime.Now.TimeOfDay;

                foreach (var asig in asignacionesHoy)
                {
                    var viajeBase = payload.ViajesBase.FirstOrDefault(v => v.Id == asig.IdViaje);
                    var vehiculo = payload.Vehiculos.FirstOrDefault(v => v.Id == asig.IdVehiculo);
                    if (viajeBase == null) continue;

                    TimeSpan horaSalida = viajeBase.HoraSalida.AHoraElSalvador().TimeOfDay;
                    TimeSpan horaFin = horaSalida.Add(TimeSpan.FromHours(2));

                    bool esIda = viajeBase.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);
                    var tarjetaViaje = new ViajeChoferCard
                    {
                        IdAsignacion = asig.Id,
                        HoraContextual = $"{AppResources.Dashboard_Departure} {DateTime.Today.Add(horaSalida):hh:mm tt}",
                        PlacaMicrobus = $"🚐 {vehiculo?.Placa ?? "N/A"}",
                        Ruta = viajeBase.TipoViaje,
                        TipoViajeBadge = esIda ? "IDA" : "REG",
                        ColorBadge = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0")
                    };

                    if (horaActual >= horaSalida && horaActual <= horaFin)
                    {
                        ViajesEnCursoAhora.Add(tarjetaViaje);
                        HasViajeEnCurso = true;
                    }
                    else if (horaActual < horaSalida && ProximoViajeHoy == null)
                    {
                        ProximoViajeHoy = tarjetaViaje;
                        HasProximoViajeHoy = true;
                    }
                }
            }
        }
    }

    private void ConstruirCalendarioMatricial(List<Asignacion> asignaciones, List<Viaje> viajesBase, Dictionary<string, string> choferesNombres)
    {
        CabecerasDias.Clear();
        var abrevDias = new[] { "Lu", "Ma", "Mi", "Ju", "Vi", "Sa", "Do" };
        for (int i = 0; i < 7; i++) { CabecerasDias.Add($"{abrevDias[i]} {FechaInicioSemana.AddDays(i).Day}"); }

        var asignacionesConViaje = asignaciones.Select(a => new { Asig = a, ViajeInfo = viajesBase.FirstOrDefault(v => v.Id == a.IdViaje) }).Where(x => x.ViajeInfo != null).ToList();

        //  DIAGNOSTICO TEMPORAL — borrar después.
        var regresoDeHoy = asignacionesConViaje.FirstOrDefault(x => x.ViajeInfo!.TipoViaje.Contains("Regreso", StringComparison.OrdinalIgnoreCase));
        if (regresoDeHoy != null)
        {
            var crudaAsig = regresoDeHoy.Asig.Fecha;
            var convertidaAsig = crudaAsig.AHoraElSalvador();
            var crudaViaje = regresoDeHoy.ViajeInfo!.HoraSalida;
            var convertidaViaje = crudaViaje.AHoraElSalvador();
            string diagChofer = $"ASIGNACION.Fecha:\nCruda: {crudaAsig:yyyy-MM-dd HH:mm:ss} Kind={crudaAsig.Kind}\nConvertida: {convertidaAsig:hh:mm tt}\n\n" +
                                 $"VIAJE.HoraSalida:\nCruda: {crudaViaje:yyyy-MM-dd HH:mm:ss} Kind={crudaViaje.Kind}\nConvertida: {convertidaViaje:hh:mm tt}";
            Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
            {
                if (Shell.Current != null) await Shell.Current.DisplayAlert("DIAGNOSTICO CHOFER (borrar después)", diagChofer, "OK");
            });
        }

        var agrupadoPorHora = asignacionesConViaje.GroupBy(x => x.Asig.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

        foreach (var grupoHora in agrupadoPorHora)
        {
            string horaDisplay = DateTime.Today.Add(grupoHora.Key).ToString("hh:mm tt");
            var tramo = new TramoHorario { HoraFija = horaDisplay };

            for (int i = 0; i < 7; i++)
            {
                DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                var asignacionesDelDia = grupoHora.Where(x => x.Asig.Fecha.AHoraElSalvador().Date == fechaEvaluar).ToList();
                var bloque = new BloqueViaje();

                foreach (var asigDia in asignacionesDelDia)
                {
                    bool esIda = asigDia.ViajeInfo!.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);
                    bloque.Opciones.Add(new OpcionViajeChofer
                    {
                        IdAsignacion = asigDia.Asig.Id,
                        ChoferInfo = $"👤 {(choferesNombres.TryGetValue(asigDia.Asig.IdChofer, out var nom) ? nom : AppResources.Dashboard_Unknown)}",
                        ColorFondo = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                        TipoViajeBadge = esIda ? "IDA" : "REG"
                    });
                }
                tramo.ViajesDeLaSemana.Add(bloque);
            }
            TramosDelDia.Add(tramo);
        }
    }

    [RelayCommand] public async Task RefreshDashboardAsync() { if (IsRefreshing) return; IsRefreshing = true; try { await LoadDriverDataAsync(); } finally { IsRefreshing = false; } }
    [RelayCommand] public async Task VerDetalleViajeAsync(string id) { if (Shell.Current != null) await Shell.Current.GoToAsync($"trip-details?idAsignacion={id}"); }
    [RelayCommand] public async Task NavegarAEdicionAsync(string id) { var asig = _asignacionesCacheadas.FirstOrDefault(a => a.Id == id); if (asig != null && Shell.Current != null) await Shell.Current.GoToAsync("edit-trip", new Dictionary<string, object> { { "AsignacionSeleccionada", asig } }); }
    [RelayCommand] public void OpenSidebar() { if (Shell.Current != null) Shell.Current.FlyoutIsPresented = true; }
}