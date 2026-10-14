// Evita el choque con Android.App.Application al compilar para Android
using Application = Microsoft.Maui.Controls.Application;
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
using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;

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
    // El comando viaja dentro de la tarjeta: así la vista no tiene que buscar el
    // ViewModel subiendo por el árbol (RelativeSource AncestorType), que es lento.
    public ICommand? AbrirCommand { get; set; }
}

public class PasajeroReserva { public string Nombre { get; set; } = ""; public string Ubicacion { get; set; } = ""; }

public class OpcionViajeChofer
{
    public string IdAsignacion { get; set; } = "";
    public string ChoferInfo { get; set; } = "";
    public string PlacaInfo { get; set; } = "";
    public string TipoViajeBadge { get; set; } = "";
    public Color ColorFondo { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    public bool PuedeEditar { get; set; }
    public ICommand? EditarCommand { get; set; }
    public ICommand? DetalleCommand { get; set; }
}

public class BloqueViaje
{
    public bool TieneViaje => Opciones.Count > 0;
    public double AltoFila { get; set; } = 160;
    // Lista simple: se arma completa antes de mostrarse, no hace falta ObservableCollection
    public List<OpcionViajeChofer> Opciones { get; set; } = new();
}

public class TramoHorario
{
    public string HoraFija { get; set; } = "";
    // Alto de la fila según cuántas unidades salen a esa hora (antes era fijo y
    // cada celda tenía su propio ScrollView, que es carísimo de dibujar).
    public double AltoFila { get; set; } = 160;
    public List<BloqueViaje> ViajesDeLaSemana { get; set; } = new();
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
    [ObservableProperty] private string fotoPerfilUsuario = "";
    [ObservableProperty] private bool hasFotoPerfilUsuario = false;
    [ObservableProperty] private string driverName = "";

    partial void OnFotoPerfilUsuarioChanged(string value) => HasFotoPerfilUsuario = !string.IsNullOrWhiteSpace(value);

    public ObservableCollection<ViajeChoferCard> ViajesEnCursoAhora { get; } = new();
    [ObservableProperty] private ViajeChoferCard? proximoViajeHoy;
    [ObservableProperty] private bool hasProximoViajeHoy = false;

    public ObservableCollection<PasajeroReserva> PasajerosDelViaje { get; } = new();
    public ObservableCollection<string> CabecerasDias { get; } = new();
    public ObservableCollection<TramoHorario> TramosDelDia { get; } = new();

    private List<Asignacion> _asignacionesCacheadas = new();

    // Huella de lo último que se dibujó: si los datos nuevos son idénticos, no se
    // vuelve a dibujar nada. Antes se redibujaba TODO dos veces cada vez que se
    // volvía a esta pantalla (una con la caché y otra con internet).
    private string _ultimaHuella = "";
    private readonly object _candado = new();

    // "Mi Semana" se arma recién cuando se abre esa pestaña
    private List<string> _cabecerasPendientes = new();
    private List<TramoHorario>? _tramosPendientes;

    // ===== Rastreo automático =====
    // Si "Compartir mi ubicación" está activado y el chofer abre la app con un viaje
    // SUYO de hoy (en curso o que sale dentro de la próxima hora), el GPS arranca solo.
    // No hace falta entrar a ninguna pantalla ni tocar "Iniciar".
    private readonly LiveTrackingService _rastreo = LiveTrackingService.Instance;
    private DriverCachePayload? _ultimoPayload;
    private bool _evaluandoRastreo;
    private readonly Dictionary<string, string> _etiquetasViaje = new();

    [ObservableProperty] private bool mostrarBannerRastreo;
    [ObservableProperty] private bool compartiendoAhora;
    [ObservableProperty] private string textoBannerRastreo = "";
    [ObservableProperty] private string estadoRastreo = "";
    [ObservableProperty] private string subtextoBannerRastreo = "";

    public DriverDashboardViewModel()
    {
        EstablecerSemanaActual();
        _rastreo.EstadoCambio += activo =>
        {
            ActualizarBannerRastreo();
            // Terminó un viaje (o se finalizó): ver si hay otro que deba arrancar
            if (!activo) _ = EvaluarRastreoAutomaticoAsync();
        };
        _rastreo.EstadoTextoCambio += texto => EstadoRastreo = texto;
    }

    private async Task EvaluarRastreoAutomaticoAsync()
    {
        if (_evaluandoRastreo) return;
        _evaluandoRastreo = true;
        try
        {
            var p = _ultimoPayload;
            if (p == null || string.IsNullOrEmpty(p.UserData?.Id)) return;
            string yo = p.UserData.Id;
            DateTime ahora = DateTime.Now;

            var candidatos = p.AsignacionesSemana
                .Where(a => a.IdChofer == yo && a.Fecha.AHoraElSalvador().Date == DateTime.Today)
                .Select(a => new { Asig = a, Viaje = p.ViajesBase.FirstOrDefault(v => v.Id == a.IdViaje) })
                .Where(x => x.Viaje != null)
                .Select(x =>
                {
                    var (ini, fin) = LiveTrackingService.VentanaDelViaje(x.Asig.Fecha.AHoraElSalvador().Date, x.Viaje!);
                    return new { x.Asig, Viaje = x.Viaje!, Ini = ini, Fin = fin };
                })
                .ToList();

            foreach (var c in candidatos)
            {
                string badge = AppResources.IsInboundTrip(c.Viaje.TipoViaje) ? AppResources.Driver_BadgeInbound : AppResources.Driver_BadgeOutbound;
                _etiquetasViaje[c.Asig.Id] = $"{badge} {DateTime.Today.Add(c.Viaje.HoraSalida.AHoraElSalvador().TimeOfDay).ToString("hh:mm tt", AppResources.Culture)}";
            }

            var elegido = candidatos
                .Where(c => ahora <= c.Fin && c.Ini <= ahora.AddMinutes(60) && !LiveTrackingService.FueFinalizadaManual(c.Asig.Id))
                .OrderBy(c => c.Ini)
                .FirstOrDefault();

            System.Diagnostics.Debug.WriteLine($"[RASTREO] Auto: candidato={(elegido?.Asig.Id ?? "ninguno")}, compartiendo={_rastreo.EstaCompartiendo}, ajuste={LiveTrackingService.CompartirActivado}");

            if (_rastreo.EstaCompartiendo || elegido == null)
            {
                if (elegido == null && !_rastreo.EstaCompartiendo) MostrarBannerRastreo = false;
                ActualizarBannerRastreo();
                return;
            }

            if (!LiveTrackingService.CompartirActivado)
            {
                // Hay viaje pero el chofer apagó "Compartir": se le avisa en el panel
                MostrarBannerRastreo = true;
                CompartiendoAhora = false;
                TextoBannerRastreo = AppResources.Dashboard_SharingOffBanner;
                SubtextoBannerRastreo = "";
                EstadoRastreo = "";
                return;
            }

            await _rastreo.IniciarAsync(elegido.Asig.Id, elegido.Ini, elegido.Fin);
            ActualizarBannerRastreo();
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "DriverDashboardViewModel.EvaluarRastreoAutomaticoAsync");
        }
        finally
        {
            _evaluandoRastreo = false;
        }
    }

    private void ActualizarBannerRastreo()
    {
        if (_rastreo.EstaCompartiendo)
        {
            string etiqueta = _rastreo.IdAsignacionActiva != null && _etiquetasViaje.TryGetValue(_rastreo.IdAsignacionActiva, out var e) ? e : "";
            MostrarBannerRastreo = true;
            CompartiendoAhora = true;
            TextoBannerRastreo = AppResources.F(AppResources.Dashboard_SharingBanner, etiqueta);
            SubtextoBannerRastreo = AppResources.Dashboard_SharingHint;
            EstadoRastreo = _rastreo.EstadoTexto;
        }
        else if (CompartiendoAhora)
        {
            MostrarBannerRastreo = false;
            CompartiendoAhora = false;
        }
    }

    // Tocar la franja: si está compartiendo, abre el detalle (mapa con pines + lista);
    // si está apagado, activa "Compartir mi ubicación" y arranca.
    [RelayCommand]
    public async Task TocarBannerRastreoAsync()
    {
        if (CompartiendoAhora && !string.IsNullOrEmpty(_rastreo.IdAsignacionActiva))
        {
            await VerDetalleViajeAsync(_rastreo.IdAsignacionActiva);
            return;
        }
        LiveTrackingService.CompartirActivado = true;
        await EvaluarRastreoAutomaticoAsync();
    }

    [RelayCommand]
    public async Task FinalizarRastreoAsync()
    {
        string? id = _rastreo.IdAsignacionActiva;
        if (string.IsNullOrEmpty(id) || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert(
            AppResources.ActiveTrip_FinishConfirmTitle,
            AppResources.ActiveTrip_FinishConfirmBody,
            AppResources.ActiveTrip_YesFinish, AppResources.Global_Cancel);
        if (!confirmar) return;

        LiveTrackingService.MarcarFinalizadaManual(id);
        _rastreo.Detener();
    }

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

    [RelayCommand]
    public void ToggleMainView(string vista)
    {
        IsTodayView = vista == "Hoy";
        IsWeeklyView = vista == "Semana";
        if (IsWeeklyView) PintarSemanaSiHaceFalta();
    }

    private void PintarSemanaSiHaceFalta()
    {
        if (_tramosPendientes == null) return;
        var tramos = _tramosPendientes;
        _tramosPendientes = null;

        CabecerasDias.Clear();
        foreach (var c in _cabecerasPendientes) CabecerasDias.Add(c);

        TramosDelDia.Clear();
        foreach (var t in tramos) TramosDelDia.Add(t);
    }

    [RelayCommand]
    public async Task LoadDriverDataAsync()
    {
        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        var localDb = GetLocalDb();
        string currentUserId = "";

        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await localDb.ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (string.IsNullOrEmpty(currentUserId))
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
            return;
        }

        string cacheKey = $"DriverData_{currentUserId}_{FechaInicioSemana:yyyyMMdd}";

        // 1. Mostrar caché primero si existe
        try
        {
            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                var payload = JsonConvert.DeserializeObject<DriverCachePayload>(cacheJson, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
                if (payload != null)
                {
                    // Procesar datos fuera del hilo principal para no congelar la pantalla
                    await Task.Run(() => ProcesarDatosChoferEnSegundoPlano(payload));
                    MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
                }
            }
        }
        catch { }

        // 2. Traer datos frescos de internet silenciosamente
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

                    foreach (var v in nuevoPayload.ViajesBase)
                    {
                        if (v.HoraSalida.Kind == DateTimeKind.Unspecified)
                            v.HoraSalida = DateTime.SpecifyKind(v.HoraSalida, DateTimeKind.Utc);
                    }

                    var dicChoferes = new Dictionary<string, string> { { userData.Id, userData.Nombre } };
                    foreach (var emp in nuevoPayload.Empleados) dicChoferes[emp.Id] = emp.Nombre;

                    DateTime inicioReal = FechaInicioSemana.AddDays(-1);
                    DateTime finReal = FechaInicioSemana.AddDays(7);

                    nuevoPayload.AsignacionesSemana = await _supabaseService.GetAsignacionesPorRangoAsync(inicioReal, finReal, dicChoferes.Keys.ToList());

                    await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevoPayload, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc }));

                    // Procesar y pintar sin bloquear la UI
                    ProcesarDatosChoferEnSegundoPlano(nuevoPayload);
                }
                catch { }
                finally
                {
                    MainThread.BeginInvokeOnMainThread(() => { IsLoading = false; IsRefreshing = false; });
                }
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => { IsLoading = false; IsRefreshing = false; });
        }
    }

    private void ProcesarDatosChoferEnSegundoPlano(DriverCachePayload payload)
    {
        // Esta función corre en segundo plano para procesar toda la lógica pesada

        // El rastreo automático se evalúa SIEMPRE (aunque la pantalla no cambie)
        _ultimoPayload = payload;
        MainThread.BeginInvokeOnMainThread(() => _ = EvaluarRastreoAutomaticoAsync());

        // Si es exactamente lo mismo que ya está en pantalla (y en el mismo bloque de
        // 5 minutos, porque "en curso" depende de la hora), no se toca la pantalla.
        string huella;
        try
        {
            huella = JsonConvert.SerializeObject(payload) + "|" + FechaInicioSemana.ToString("yyyyMMdd") + "|" + (DateTime.Now.Ticks / TimeSpan.FromMinutes(5).Ticks);
        }
        catch { huella = Guid.NewGuid().ToString(); }

        lock (_candado)
        {
            if (huella == _ultimaHuella) return;
            _ultimaHuella = huella;
        }

        // 🆕 Jefe, independiente o empleado con permiso del jefe (sub-jefe)
        bool canSchedule = RolesFlota.PuedeGestionarViajes(payload.UserData);
        string fotoPerfil = payload.UserData.FotoPerfil ?? "";
        string nombreChofer = payload.UserData.Nombre ?? "";
        var dicPlacas = payload.Vehiculos.GroupBy(v => v.Id).ToDictionary(g => g.Key, g => g.First().Placa);

        var dicChoferes = new Dictionary<string, string> { { payload.UserData.Id, payload.UserData.Nombre } };
        foreach (var emp in payload.Empleados) dicChoferes[emp.Id] = emp.Nombre;
        _asignacionesCacheadas = payload.AsignacionesSemana;

        var asigSemanaEstricta = payload.AsignacionesSemana.Where(a => a.Fecha.AHoraElSalvador().Date >= FechaInicioSemana.Date && a.Fecha.AHoraElSalvador().Date <= FechaInicioSemana.AddDays(6).Date).ToList();
        bool isScheduleEmptyVal = asigSemanaEstricta.Count == 0;

        var tempCabeceras = new List<string>();
        var tempTramos = new List<TramoHorario>();
        var tempViajesCurso = new List<ViajeChoferCard>();
        ViajeChoferCard? tempProximoViaje = null;
        bool tempHasViajesHoy = false;
        bool tempHasViajeEnCurso = false;

        if (!isScheduleEmptyVal)
        {
            // Construir calendario en segundo plano
            var abrevDias = AppResources.WeekdayShortNames;
            for (int i = 0; i < 7; i++) { tempCabeceras.Add($"{abrevDias[i]} {FechaInicioSemana.AddDays(i).Day}"); }

            var asignacionesConViaje = asigSemanaEstricta.Select(a => new { Asig = a, ViajeInfo = payload.ViajesBase.FirstOrDefault(v => v.Id == a.IdViaje) }).Where(x => x.ViajeInfo != null).ToList();
            var agrupadoPorHora = asignacionesConViaje.GroupBy(x => x.Asig.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

            foreach (var grupoHora in agrupadoPorHora)
            {
                string horaDisplay = DateTime.Today.Add(grupoHora.Key).ToString("hh:mm tt", AppResources.Culture);
                var tramo = new TramoHorario { HoraFija = horaDisplay };

                // Alto de la fila = lo que necesita el día con más unidades a esa hora
                int maxUnidades = Enumerable.Range(0, 7)
                    .Select(i => grupoHora.Count(x => x.Asig.Fecha.AHoraElSalvador().Date == FechaInicioSemana.AddDays(i).Date))
                    .DefaultIfEmpty(1).Max();
                tramo.AltoFila = Math.Max(160, maxUnidades * 82 + 8);

                for (int i = 0; i < 7; i++)
                {
                    DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                    var asignacionesDelDia = grupoHora.Where(x => x.Asig.Fecha.AHoraElSalvador().Date == fechaEvaluar).ToList();
                    var bloque = new BloqueViaje { AltoFila = tramo.AltoFila };

                    foreach (var asigDia in asignacionesDelDia)
                    {
                        bool esIda = AppResources.IsInboundTrip(asigDia.ViajeInfo!.TipoViaje);
                        bloque.Opciones.Add(new OpcionViajeChofer
                        {
                            IdAsignacion = asigDia.Asig.Id,
                            ChoferInfo = $"👤 {(dicChoferes.TryGetValue(asigDia.Asig.IdChofer, out var nom) ? nom : AppResources.Dashboard_Unknown)}",
                            PlacaInfo = $"🚐 {(dicPlacas.TryGetValue(asigDia.Asig.IdVehiculo, out var placa) ? placa : "N/A")}",
                            ColorFondo = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                            TipoViajeBadge = esIda ? AppResources.Driver_BadgeInbound : AppResources.Driver_BadgeOutbound,
                            PuedeEditar = canSchedule,
                            EditarCommand = NavegarAEdicionCommand,
                            DetalleCommand = VerDetalleViajeCommand
                        });
                    }
                    tramo.ViajesDeLaSemana.Add(bloque);
                }
                tempTramos.Add(tramo);
            }

            // Filtrar viajes de hoy
            var asignacionesHoy = asigSemanaEstricta.Where(a => a.Fecha.AHoraElSalvador().Date == DateTime.Today).ToList();
            if (asignacionesHoy.Count > 0)
            {
                tempHasViajesHoy = true;
                TimeSpan horaActual = DateTime.Now.TimeOfDay;

                foreach (var asig in asignacionesHoy)
                {
                    var viajeBase = payload.ViajesBase.FirstOrDefault(v => v.Id == asig.IdViaje);
                    var vehiculo = payload.Vehiculos.FirstOrDefault(v => v.Id == asig.IdVehiculo);
                    if (viajeBase == null) continue;

                    TimeSpan horaSalida = viajeBase.HoraSalida.AHoraElSalvador().TimeOfDay;
                    TimeSpan horaFin = horaSalida.Add(TimeSpan.FromHours(2));
                    bool esIda = AppResources.IsInboundTrip(viajeBase.TipoViaje);

                    var tarjetaViaje = new ViajeChoferCard
                    {
                        IdAsignacion = asig.Id,
                        HoraContextual = $"{AppResources.Dashboard_Departure} {DateTime.Today.Add(horaSalida).ToString("hh:mm tt", AppResources.Culture)}",
                        PlacaMicrobus = $"🚐 {vehiculo?.Placa ?? "N/A"}",
                        Ruta = AppResources.TripTypeLabel(viajeBase.TipoViaje),
                        TipoViajeBadge = esIda ? AppResources.Driver_BadgeInbound : AppResources.Driver_BadgeOutbound,
                        ColorBadge = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                        // El GPS ahora arranca solo (ver EvaluarRastreoAutomaticoAsync), así que
                        // la tarjeta abre el detalle: mapa con los pines y lista de pasajeros.
                        AbrirCommand = VerDetalleViajeCommand
                    };

                    System.Diagnostics.Debug.WriteLine($"[RASTREO] 0. Viaje de hoy {asig.Id}: salida {horaSalida}, ahora {horaActual}, en curso={(horaActual >= horaSalida && horaActual <= horaFin)}");
                    if (horaActual >= horaSalida && horaActual <= horaFin)
                    {
                        tempViajesCurso.Add(tarjetaViaje);
                        tempHasViajeEnCurso = true;
                    }
                    else if (horaActual < horaSalida && tempProximoViaje == null)
                    {
                        tempProximoViaje = tarjetaViaje;
                    }
                }
            }
        }

        // Finalmente empujamos todos los datos calculados de golpe a la pantalla
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CanScheduleTrips = canSchedule;
            FotoPerfilUsuario = fotoPerfil;
            DriverName = nombreChofer;
            IsScheduleEmpty = isScheduleEmptyVal;

            // El calendario semanal es lo más pesado de dibujar: se guarda y se arma
            // solo si esa pestaña está abierta (o cuando se abra).
            _cabecerasPendientes = tempCabeceras;
            _tramosPendientes = tempTramos;
            if (IsWeeklyView) PintarSemanaSiHaceFalta();

            ViajesEnCursoAhora.Clear();
            foreach (var v in tempViajesCurso) ViajesEnCursoAhora.Add(v);

            HasViajesHoy = tempHasViajesHoy;
            HasViajeEnCurso = tempHasViajeEnCurso;

            ProximoViajeHoy = tempProximoViaje;
            HasProximoViajeHoy = tempProximoViaje != null;
        });
    }

    [RelayCommand] public async Task RefreshDashboardAsync() { if (IsRefreshing) return; IsRefreshing = true; await LoadDriverDataAsync(); }
    // Pantalla del viaje en marcha: botón "Iniciar viaje" (GPS), lista de abordaje y "Finalizar"
    [RelayCommand]
    public async Task AbrirViajeActivoAsync(string id)
    {
        System.Diagnostics.Debug.WriteLine($"[RASTREO] 1. Tocó la tarjeta del viaje. idAsignacion='{id}'");
        if (Shell.Current != null && !string.IsNullOrEmpty(id)) await Shell.Current.GoToAsync($"active-trip?idAsignacion={id}");
    }
    [RelayCommand] public async Task VerDetalleViajeAsync(string id) { if (Shell.Current != null) await Shell.Current.GoToAsync($"trip-details?idAsignacion={id}"); }
    [RelayCommand] public async Task NavegarAEdicionAsync(string id) { var asig = _asignacionesCacheadas.FirstOrDefault(a => a.Id == id); if (asig != null && Shell.Current != null) await Shell.Current.GoToAsync("edit-trip", new Dictionary<string, object> { { "AsignacionSeleccionada", asig } }); }
    [RelayCommand] public void OpenSidebar() { if (Shell.Current != null) Shell.Current.FlyoutIsPresented = true; }
}