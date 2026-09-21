using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Microsoft.Maui.Networking;
using PassGold.Models.Local;
using Microsoft.Extensions.DependencyInjection; //  LIBRERÍA AÑADIDA PARA GETSERVICE

namespace PassGold.ViewModels;

public class PassengerCachePayload
{
    public Usuario UserData { get; set; } = new();
    public List<Reserva> MisReservas { get; set; } = new();
    public List<Asignacion> AsignacionesAsociadas { get; set; } = new();
    public List<Viaje> ViajesAsociados { get; set; } = new();
    public List<Usuario> ChoferesAsociados { get; set; } = new();
    public List<Vehiculo> VehiculosAsociados { get; set; } = new();
    public List<Usuario> FlotasAsociadas { get; set; } = new();
    public List<Usuario> DirectorioFlotas { get; set; } = new();
}

public class BloqueMiReserva
{
    public string IdReserva { get; set; } = "";
    public string InfoRuta { get; set; } = "";
    public string InfoTransporte { get; set; } = "";
    public string InfoFlota { get; set; } = "";
    public Color ColorFondo { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent; //  RUTA EXACTA
    public bool TieneViaje { get; set; } = false;
}

public class TramoMiHorario
{
    public string HoraFija { get; set; } = "";
    public ObservableCollection<BloqueMiReserva> ReservasDeLaSemana { get; set; } = new();
}

public partial class OpcionViajePasajero : ObservableObject
{
    public string IdAsignacion { get; set; } = "";
    public string IdViajeBase { get; set; } = "";
    public string IdChofer { get; set; } = "";
    public DateTime FechaAsignacion { get; set; }
    public string ChoferInfo { get; set; } = "";
    public Color ColorFondo { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent; //  RUTA EXACTA
    public Color ColorTexto { get; set; } = Microsoft.Maui.Graphics.Colors.White; //  RUTA EXACTA
    public string TipoViajeCompleto { get; set; } = "";
    public string HoraSalida { get; set; } = "";
    public bool EsIda { get; set; }

    // 🆕 Para bloquear la re-reserva: si el pasajero ya tiene una reserva activa
    // para esta asignación exacta, el radio button no debe dejarse tocar de nuevo
    // — en vez de eso, la celda muestra un ✅ fijo y un botón de cancelar.
    public bool YaReservado { get; set; } = false;
    public string IdReservaExistente { get; set; } = "";

    [ObservableProperty] private bool isSelected;
}

public class CeldaCalendarioPasajero
{
    public bool TieneViaje => Opciones.Count > 0;
    public ObservableCollection<OpcionViajePasajero> Opciones { get; set; } = new();
}

public class TramoHorarioPasajero
{
    public string HoraFija { get; set; } = "";
    public ObservableCollection<CeldaCalendarioPasajero> ViajesDeLaSemana { get; set; } = new();
}

public class FlotaPasajeroCard
{
    public string IdFlota { get; set; } = "";
    public string NombreFlota { get; set; } = "";
    public string NombreJefe { get; set; } = "";
    public string FotoPerfil { get; set; } = "";
    public string Telefono { get; set; } = "";
    public bool TieneTelefono => !string.IsNullOrWhiteSpace(Telefono);
    public List<string> RutasList { get; set; } = new();
    public Usuario FlotaOriginal { get; set; } = new();
}

public partial class PassengerDashboardViewModel : ObservableObject, IQueryAttributable
{
    // FIX: [QueryProperty] en la pagina causaba "Object must implement IConvertible"
    // (bug conocido de Shell al pasar objetos complejos). IQueryAttributable recibe
    // el diccionario crudo, sin ningun intento de Convert.ChangeType.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("FlotaParaCalendario", out var value) && value is Usuario flota)
        {
            var card = new FlotaPasajeroCard
            {
                FlotaOriginal = flota,
                NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? $"Transportes {flota.Nombre}" : flota.NombreFlota,
                NombreJefe = flota.Nombre ?? "",
                FotoPerfil = flota.FotoPerfil ?? "",
                Telefono = flota.Telefono ?? ""
            };
            _ = SeleccionarFlotaAsync(card);
        }
    }
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    //  Se guardan acá (no solo dentro de ConstruirMiHorario) para poder cruzarlas
    // también cuando el pasajero está viendo el horario de UNA flota específica
    // para reservar (CargarHorarioFlotaAsync) — y así saber qué celdas ya tienen
    // reserva activa, sin pedirle los datos de nuevo a Supabase.
    private List<Reserva> _reservasActivasPasajero = new();

    //  RUTA EXACTA DE APPLICATION
    private LocalDatabaseService GetLocalDb() => Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private string studentName = AppResources.Dashboard_Loading;
    // 🆕 Mismo bug que en DriverDashboardViewModel: payload.UserData ya trae
    // FotoPerfil de Supabase, pero nunca se copiaba a nada bindeable por la
    // vista — el avatar del pasajero quedaba con el 🎓 fijo siempre.
    [ObservableProperty] private string fotoPerfilUsuario = "";
    [ObservableProperty] private bool hasFotoPerfilUsuario = false;

    partial void OnFotoPerfilUsuarioChanged(string value) => HasFotoPerfilUsuario = !string.IsNullOrWhiteSpace(value);
    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private DateTime fechaInicioSemana;
    [ObservableProperty] private string textoRangoFechas = "";

    [ObservableProperty] private bool isViewingMySchedule = true;
    [ObservableProperty] private bool isViewingDirectory = false;
    [ObservableProperty] private string textoBusqueda = "";

    public ObservableCollection<string> CabecerasDias { get; } = new();
    public ObservableCollection<TramoMiHorario> TramosMiHorario { get; } = new();
    [ObservableProperty] private bool isMyScheduleEmpty = true;

    private List<FlotaPasajeroCard> _todasLasFlotas = new();
    public ObservableCollection<FlotaPasajeroCard> FlotasDisponibles { get; } = new();

    [ObservableProperty] private string selectedFleetName = "";

    //  VARIABLE RESTAURADA
    private Usuario? _selectedFleet;

    public ObservableCollection<TramoHorarioPasajero> TramosDelDia { get; } = new();
    public ObservableCollection<UbicacionPasajero> MisUbicaciones { get; } = new();

    [ObservableProperty] private bool haySeleccionActiva = false;
    [ObservableProperty] private bool usarNuevaUbicacion = false;
    [ObservableProperty] private UbicacionPasajero? ubicacionSeleccionada;
    [ObservableProperty] private string puntoRecogidaTexto = "";
    [ObservableProperty] private string puntoBajadaTexto = "";
    [ObservableProperty] private string promptUbicacion = "";
    [ObservableProperty] private bool hacerReservaRecurrente = false;
    [ObservableProperty] private DateTime fechaLimiteReserva = DateTime.Today;

    public PassengerDashboardViewModel() { EstablecerSemanaActual(); }

    private void EstablecerSemanaActual()
    {
        int diasOffset = (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diasOffset < 0) diasOffset += 7;
        FechaInicioSemana = DateTime.Today.AddDays(-diasOffset);
        ActualizarTextoFechas();
    }

    private void ActualizarTextoFechas()
    {
        TextoRangoFechas = $"{AppResources.Dashboard_From} {FechaInicioSemana:dd MMM} {AppResources.Dashboard_To} {FechaInicioSemana.AddDays(6):dd MMM, yyyy}";
    }

    [RelayCommand]
    public void ToggleMainView(string vista)
    {
        IsViewingMySchedule = vista == "Horario";
        IsViewingDirectory = vista == "Directorio";
    }

    [RelayCommand]
    public async Task AvanzarSemana()
    {
        FechaInicioSemana = FechaInicioSemana.AddDays(7);
        ActualizarTextoFechas();
        //  FIX: este comando lo comparten "Mi Horario" (LoadDashboardAsync) y
        // "Horario de una Flota" (CargarHorarioFlotaAsync) — antes SIEMPRE llamaba
        // a LoadDashboardAsync, así que en FleetSchedulePage el título de la semana
        // cambiaba pero las celdas se quedaban con los datos de la semana vieja,
        // haciendo que tocar un día pareciera "ya pasó" sin ninguna razón visible.
        if (_selectedFleet != null) await CargarHorarioFlotaAsync(_selectedFleet.Id);
        else await LoadDashboardAsync();
    }

    [RelayCommand]
    public async Task RetrocederSemana()
    {
        FechaInicioSemana = FechaInicioSemana.AddDays(-7);
        ActualizarTextoFechas();
        if (_selectedFleet != null) await CargarHorarioFlotaAsync(_selectedFleet.Id);
        else await LoadDashboardAsync();
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        // Si ya está cargando y tocás "Mi Horario" de nuevo (fácil de hacer
        // cuando se siente lento), esto evita que se disparen varias cargas
        // superpuestas a la vez — lo cual solo empeoraba la lentitud.
        if (IsLoading) return;

        IsLoading = true;
        var localDb = GetLocalDb();
        string currentUserId = "";
        var authUser = _supabaseService.GetCurrentUser();

        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await localDb.ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (string.IsNullOrEmpty(currentUserId)) { IsLoading = false; return; }

        string cacheKey = $"PassengerData_{currentUserId}_{FechaInicioSemana:yyyyMMdd}";

        try
        {
            await Task.Delay(100);
            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                var payload = JsonConvert.DeserializeObject<PassengerCachePayload>(cacheJson, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
                if (payload != null) ProcesarDatosPasajero(payload);
            }

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var userData = await _supabaseService.GetUserDataAsync(currentUserId);
                        if (userData == null) return;

                        var nuevoPayload = new PassengerCachePayload { UserData = userData };
                        nuevoPayload.DirectorioFlotas = await _supabaseService.GetPublicFleetsAsync();

                        DateTime inicioReal = FechaInicioSemana.AddDays(-1);
                        DateTime finReal = FechaInicioSemana.AddDays(7);
                        var misReservas = await _supabaseService.GetReservasPorPasajeroYRangoAsync(currentUserId, inicioReal, finReal);
                        nuevoPayload.MisReservas = misReservas;

                        //  FIX DE LENTITUD: antes esto pedía cada Asignación, Viaje,
                        // Vehículo y Chofer UNO POR UNO, en fila — con muchas reservas
                        // (30, 50, 100...) eso eran decenas de viajes de red seguidos,
                        // sintiéndose como que la app se congelaba. Ahora se trae todo
                        // junto: 4 consultas en total, sin importar cuántas reservas haya.
                        if (misReservas.Count > 0)
                        {
                            var idsAsignaciones = misReservas.Select(r => r.IdAsignacion).Distinct().ToList();
                            var asignaciones = await _supabaseService.GetAsignacionesByIdsAsync(idsAsignaciones);
                            nuevoPayload.AsignacionesAsociadas.AddRange(asignaciones);

                            var idsViajes = asignaciones.Select(a => a.IdViaje).Distinct().ToList();
                            var viajes = await _supabaseService.GetViajesByIdsAsync(idsViajes);
                            nuevoPayload.ViajesAsociados.AddRange(viajes);

                            var idsVehiculos = asignaciones.Select(a => a.IdVehiculo).Distinct().ToList();
                            var vehiculos = await _supabaseService.GetVehiculosByIdsAsync(idsVehiculos);
                            nuevoPayload.VehiculosAsociados.AddRange(vehiculos);

                            var idsChoferes = asignaciones.Select(a => a.IdChofer).Distinct().ToList();
                            var choferes = await _supabaseService.GetUsuariosByIdsAsync(idsChoferes);
                            nuevoPayload.ChoferesAsociados.AddRange(choferes);

                            var idsFlotas = choferes.Select(c => c.IdJefe ?? c.Id).Distinct().ToList();
                            var flotas = await _supabaseService.GetUsuariosByIdsAsync(idsFlotas);
                            nuevoPayload.FlotasAsociadas.AddRange(flotas);
                        }

                        var misUb = await _supabaseService.GetUbicacionesPasajeroAsync(currentUserId);

                        //  RUTA EXACTA DE APPLICATION
                        Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                        {
                            MisUbicaciones.Clear();
                            foreach (var u in misUb.OrderByDescending(x => x.CreadoEn)) MisUbicaciones.Add(u);
                        });

                        await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevoPayload, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc }));
                        Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() => { ProcesarDatosPasajero(nuevoPayload); });
                    }
                    catch { }
                });
            }
        }
        catch { }
        finally { IsLoading = false; }
    }

    private void ProcesarDatosPasajero(PassengerCachePayload payload)
    {
        StudentName = payload.UserData.Nombre;
        FotoPerfilUsuario = payload.UserData.FotoPerfil ?? "";
        _todasLasFlotas.Clear();
        FlotasDisponibles.Clear();

        // Se guarda acá para reusar en CargarHorarioFlotaAsync (ver arriba).
        _reservasActivasPasajero = payload.MisReservas.Where(r => r.Estado != "cancelado").ToList();

        foreach (var flota in payload.DirectorioFlotas)
        {
            var rutasUnicas = new List<string>();
            if (!string.IsNullOrWhiteSpace(flota.RutasFlota))
                rutasUnicas = flota.RutasFlota.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).Distinct().ToList();
            if (rutasUnicas.Count == 0) rutasUnicas.Add("Rutas a convenir");

            var card = new FlotaPasajeroCard
            {
                IdFlota = flota.Id,
                NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? $"Transportes {flota.Nombre}" : flota.NombreFlota,
                NombreJefe = flota.Nombre,
                FotoPerfil = flota.FotoPerfil ?? "",
                Telefono = flota.Telefono ?? "",
                RutasList = rutasUnicas,
                FlotaOriginal = flota
            };

            _todasLasFlotas.Add(card);
            FlotasDisponibles.Add(card);
        }

        ConstruirMiHorario(payload);
    }

    private void ConstruirMiHorario(PassengerCachePayload payload)
    {
        CabecerasDias.Clear();
        TramosMiHorario.Clear();

        var abrevDias = new[] { "Lu", "Ma", "Mi", "Ju", "Vi", "Sa", "Do" };
        for (int i = 0; i < 7; i++)
        {
            DateTime fechaColumna = FechaInicioSemana.AddDays(i);
            CabecerasDias.Add($"{abrevDias[i]} {fechaColumna.Day}");
        }

        var reservasActivas = payload.MisReservas.Where(r => r.Estado != "cancelado").ToList();
        var reservasConContexto = reservasActivas.Select(r =>
        {
            var asig = payload.AsignacionesAsociadas.FirstOrDefault(a => a.Id == r.IdAsignacion);
            var viaje = asig != null ? payload.ViajesAsociados.FirstOrDefault(v => v.Id == asig.IdViaje) : null;
            return new { Reserva = r, Asignacion = asig, Viaje = viaje };
        }).Where(x => x.Asignacion != null && x.Viaje != null).ToList();

        var reservasSemana = reservasConContexto
            .Where(x => x.Asignacion!.Fecha.AHoraElSalvador().Date >= FechaInicioSemana.Date &&
                        x.Asignacion.Fecha.AHoraElSalvador().Date <= FechaInicioSemana.AddDays(6).Date).ToList();

        IsMyScheduleEmpty = reservasSemana.Count == 0;

        if (!IsMyScheduleEmpty)
        {
            //  FIX DE FONDO: antes se agrupaba por Viaje.Id — si ese mismo viaje tenía
            // reservas de prueba viejas con horas distintas (por ediciones posteriores),
            // todas caían en una sola fila y la etiqueta podía no coincidir con lo que
            // hay en cada día. Ahora se agrupa directo por la hora REAL de cada
            // asignación — así la etiqueta de la fila y su contenido SIEMPRE coinciden,
            // sin importar el historial de ediciones del viaje.
            var agrupadoPorHora = reservasSemana.GroupBy(x => x.Asignacion!.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

            foreach (var grupo in agrupadoPorHora)
            {
                string horaEtiqueta = DateTime.Today.Add(grupo.Key).ToString("hh:mm tt");
                bool esIda = grupo.First().Viaje!.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);

                var tramo = new TramoMiHorario { HoraFija = $"{horaEtiqueta}\n{(esIda ? "Hacia la U" : "Hacia Casa")}" };

                for (int i = 0; i < 7; i++)
                {
                    DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                    var itemDelDia = grupo.FirstOrDefault(x => x.Asignacion!.Fecha.AHoraElSalvador().Date == fechaEvaluar);

                    if (itemDelDia != null)
                    {
                        var viajeInfo = itemDelDia.Viaje!;
                        bool esIdaDelDia = viajeInfo.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);
                        var chofer = payload.ChoferesAsociados.FirstOrDefault(c => c.Id == itemDelDia.Asignacion!.IdChofer);
                        var vehiculo = payload.VehiculosAsociados.FirstOrDefault(v => v.Id == itemDelDia.Asignacion!.IdVehiculo);
                        string idJefe = chofer?.IdJefe ?? chofer?.Id ?? "";
                        var flota = payload.FlotasAsociadas.FirstOrDefault(f => f.Id == idJefe);

                        string nombreFlota = flota?.NombreFlota ?? $"Transportes {flota?.Nombre ?? ""}";

                        tramo.ReservasDeLaSemana.Add(new BloqueMiReserva
                        {
                            IdReserva = itemDelDia.Reserva.Id,
                            InfoFlota = string.IsNullOrWhiteSpace(nombreFlota) ? "Flota Local" : nombreFlota,
                            InfoTransporte = $"👤 {chofer?.Nombre ?? AppResources.Dashboard_Unknown} \n🚐 {vehiculo?.Placa ?? "N/A"}",
                            InfoRuta = viajeInfo.TipoViaje,
                            ColorFondo = esIdaDelDia ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                            TieneViaje = true
                        });
                    }
                    else
                    {
                        tramo.ReservasDeLaSemana.Add(new BloqueMiReserva { TieneViaje = false });
                    }
                }
                TramosMiHorario.Add(tramo);
            }
        }
    }

    [RelayCommand]
    public async Task CancelarMiReservaAsync(string idReserva)
    {
        if (string.IsNullOrEmpty(idReserva) || Shell.Current == null) return;
        bool confirmar = await Shell.Current.DisplayAlert("Anular Viaje", "¿Seguro que deseas anular tu asiento?", "Sí", "Volver");
        if (!confirmar) return;

        IsLoading = true;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, "Necesitas internet para anular.", AppResources.Global_Ok);
            IsLoading = false; return;
        }

        bool exito = await _supabaseService.DeleteReservaAsync(idReserva);
        if (exito)
        {
            await LoadDashboardAsync();
            // Si estamos viendo el calendario de una flota específica (no "Mi
            // Horario"), LoadDashboardAsync no lo refresca — sin esto, la celda
            // seguía mostrándose como "ya reservada" hasta salir y volver a entrar.
            if (_selectedFleet != null) await CargarHorarioFlotaAsync(_selectedFleet.Id);
            await Shell.Current.DisplayAlert("Listo", "Reserva anulada.", AppResources.Global_Ok);
        }
        else { await Shell.Current.DisplayAlert(AppResources.Global_Error, "No pudimos anular.", AppResources.Global_Ok); }
        IsLoading = false;
    }

    [RelayCommand]
    public void BuscarFlotas()
    {
        FlotasDisponibles.Clear();
        if (string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            foreach (var f in _todasLasFlotas) FlotasDisponibles.Add(f);
            return;
        }

        var palabrasClave = TextoBusqueda.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var resultados = _todasLasFlotas.Where(f =>
        {
            string datosFlota = $"{f.NombreFlota}{f.NombreJefe}{string.Join("", f.RutasList)}".ToLowerInvariant().Replace(" ", "");
            return palabrasClave.All(palabra => datosFlota.Contains(palabra));
        }).ToList();
        foreach (var f in resultados) FlotasDisponibles.Add(f);
    }

    [RelayCommand]
    public async Task IrAPerfilFlotaAsync(FlotaPasajeroCard flotaCard)
    {
        if (flotaCard == null || Shell.Current == null) return;

        var navigationParams = new Dictionary<string, object>
        {
            { "FlotaObj", flotaCard.FlotaOriginal }
        };
        await Shell.Current.GoToAsync("fleet-profile", navigationParams);
    }

    [RelayCommand]
    public async Task SeleccionarFlotaAsync(FlotaPasajeroCard flotaCard)
    {
        if (flotaCard == null) return;
        _selectedFleet = flotaCard.FlotaOriginal;
        SelectedFleetName = flotaCard.NombreFlota;

        //  FIX: FechaInicioSemana se comparte con "Mi Horario". Si el usuario había
        // navegado hacia atrás ahí (◀) para ver reservas viejas, esa fecha se quedaba
        // pegada — y al entrar a reservar en una flota, el calendario mostraba (y
        // dejaba tocar) los días de esa semana pasada en vez de la semana actual.
        // Por eso las reservas terminaban cayendo en fechas ya pasadas sin que el
        // usuario se diera cuenta. Reseteamos siempre a la semana de hoy al entrar
        // a reservar en una flota nueva.
        EstablecerSemanaActual();

        await CargarHorarioFlotaAsync(flotaCard.FlotaOriginal.Id);
    }

    private async Task CargarHorarioFlotaAsync(string idJefe)
    {
        IsLoading = true;
        TramosDelDia.Clear();

        // FIX: CabecerasDias (Lu, Ma, Mi...) nunca se llenaba aqui — solo se llenaba
        // en ConstruirMiHorario ("Mi Horario"), por eso al ver el horario de OTRA flota
        // se veian las horas pero nunca los dias.
        CabecerasDias.Clear();
        var abrevDias = new[] { "Lu", "Ma", "Mi", "Ju", "Vi", "Sa", "Do" };
        for (int i = 0; i < 7; i++)
        {
            CabecerasDias.Add($"{abrevDias[i]} {FechaInicioSemana.AddDays(i).Day}");
        }

        try
        {
            //  FIX: MisUbicaciones (donde vive "Mi Casa" y el historial de lugares
            // guardados) antes SOLO se cargaba en "Mi Horario" — acá, en el flujo de
            // reservar en una flota, se quedaba siempre vacía, así que aunque el
            // pasajero ya tuviera su casa guardada, el sistema nunca la encontraba.
            string currentUserId = "";
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
            else { var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var misUb = await _supabaseService.GetUbicacionesPasajeroAsync(currentUserId);
                MisUbicaciones.Clear();
                foreach (var u in misUb.OrderByDescending(x => x.CreadoEn)) MisUbicaciones.Add(u);
            }

            var equipo = await _supabaseService.GetEmployeesByBossAsync(idJefe);
            var dicChoferes = new Dictionary<string, string> { { idJefe, "Jefe/Independiente" } };
            var idsEquipo = new List<string> { idJefe };
            foreach (var emp in equipo) { idsEquipo.Add(emp.Id); dicChoferes[emp.Id] = emp.Nombre; }

            var inicioReal = FechaInicioSemana.AddDays(-1);
            var finReal = FechaInicioSemana.AddDays(7);

            var asignaciones = await _supabaseService.GetAsignacionesPorRangoAsync(inicioReal, finReal, idsEquipo);
            var viajesBase = await _supabaseService.GetAllActiveTripsAsync();

            var asigSemanaEstricta = asignaciones.Where(a => a.Fecha.AHoraElSalvador().Date >= FechaInicioSemana.Date && a.Fecha.AHoraElSalvador().Date <= FechaInicioSemana.AddDays(6).Date).ToList();

            if (asigSemanaEstricta.Count > 0)
            {
                var asignacionesConViaje = asigSemanaEstricta.Select(a => new { Asig = a, ViajeInfo = viajesBase.FirstOrDefault(v => v.Id == a.IdViaje) }).Where(x => x.ViajeInfo != null).ToList();

                //  FIX: mismo problema que ya arreglamos en "Mi Horario" — acá se
                // agrupaba por Viaje.HoraSalida (la plantilla), no por la hora real de
                // cada asignación. Si por cualquier motivo esos dos valores no
                // coinciden exactamente, la ETIQUETA de la fila queda mal aunque el
                // dato real (usado para armar la reserva) esté perfecto. Ahora se
                // agrupa por la hora real de la asignación, igual que en Mi Horario.
                var agrupadoPorHora = asignacionesConViaje.GroupBy(x => x.Asig.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

                foreach (var grupoHora in agrupadoPorHora)
                {
                    string horaDisplay = DateTime.Today.Add(grupoHora.Key).ToString("hh:mm tt");
                    var tramo = new TramoHorarioPasajero { HoraFija = horaDisplay };

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                        var celdaDia = new CeldaCalendarioPasajero();
                        var asignacionesDelDia = grupoHora.Where(x => x.Asig.Fecha.AHoraElSalvador().Date == fechaEvaluar).ToList();

                        foreach (var asigDia in asignacionesDelDia)
                        {
                            bool esIda = asigDia.ViajeInfo!.TipoViaje.Contains("Ida", StringComparison.OrdinalIgnoreCase);

                            //  Si ya existe una reserva activa mía para esta asignación
                            // exacta, la celda se marca como YaReservado — el radio button
                            // deja de poder tocarse para "reservar de nuevo" (ver
                            // AbrirPanelReserva) y en su lugar se muestra un botón de
                            // cancelar. Antes esto solo se avisaba con un popup al final,
                            // después de que el usuario ya había armado toda su selección.
                            var reservaExistente = _reservasActivasPasajero.FirstOrDefault(r => r.IdAsignacion == asigDia.Asig.Id);

                            celdaDia.Opciones.Add(new OpcionViajePasajero
                            {
                                IdAsignacion = asigDia.Asig.Id,
                                IdViajeBase = asigDia.ViajeInfo.Id,
                                IdChofer = asigDia.Asig.IdChofer,
                                FechaAsignacion = asigDia.Asig.Fecha.AHoraElSalvador(),
                                ChoferInfo = dicChoferes.TryGetValue(asigDia.Asig.IdChofer, out var nom) ? nom : AppResources.Dashboard_Unknown,
                                ColorFondo = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                                ColorTexto = Microsoft.Maui.Graphics.Colors.White, // 🔥 RUTA EXACTA
                                TipoViajeCompleto = asigDia.ViajeInfo.TipoViaje,
                                HoraSalida = horaDisplay,
                                EsIda = esIda,
                                IsSelected = false,
                                YaReservado = reservaExistente != null,
                                IdReservaExistente = reservaExistente?.Id ?? ""
                            });
                        }
                        tramo.ViajesDeLaSemana.Add(celdaDia);
                    }
                    TramosDelDia.Add(tramo);
                }
            }
        }
        catch { }
        IsLoading = false;
    }

    //  NUEVO: seleccion MULTIPLE (horario universitario real: distintos dias/horas de la misma flota)
    // Alias de compatibilidad: por si algún XAML viejo aun bindea a "ViajeParaReservar" directo.
    public OpcionViajePasajero? ViajeParaReservar => SeleccionesActuales.FirstOrDefault();

    public ObservableCollection<OpcionViajePasajero> SeleccionesActuales { get; } = new();

    [RelayCommand]
    public void AbrirPanelReserva(OpcionViajePasajero opcion)
    {
        if (opcion == null) return;

        //  Si ya hay una reserva activa para esta asignación, no se puede volver a
        // "seleccionar para reservar" — el usuario tiene que cancelarla primero
        // (botón de cancelar en la misma celda) si quiere liberar el cupo.
        if (opcion.YaReservado) return;

        //  Capa extra de seguridad: aunque ya reseteamos la semana al entrar a
        // reservar (ver SeleccionarFlotaAsync), esto evita reservar un día que ya
        // pasó por cualquier otro camino que se nos haya escapado.
        if (opcion.FechaAsignacion.Date < DateTime.Today)
        {
            Shell.Current?.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_PastDateError, AppResources.Global_Ok);
            return;
        }

        // Toggle: si ya estaba seleccionada, se quita; si no, se agrega.
        if (opcion.IsSelected)
        {
            opcion.IsSelected = false;
            SeleccionesActuales.Remove(opcion);
        }
        else
        {
            opcion.IsSelected = true;
            SeleccionesActuales.Add(opcion);
        }

        HaySeleccionActiva = SeleccionesActuales.Count > 0;
    }

    //  SIMPLIFICADO: antes esto disparaba una cadena de popups (¿dónde te
    // dejamos? -> ¿para cuándo?) más un camino aparte para el mapa. Ahora, después
    // de validar que la selección tenga sentido, abre UNA sola pantalla
    // (ConfirmarReservaPage) con todo el formulario visible junto: mapa, pin de
    // casa, pin alterno, paradas oficiales, historial y alcance. Se usa
    // Navigation.PushAsync (no una ruta de Shell) para no depender de que la ruta
    // esté registrada en AppShell.
    [RelayCommand]
    public async Task SiguienteAsync()
    {
        if (SeleccionesActuales.Count == 0 || Shell.Current == null) return;

        foreach (var opcion in SeleccionesActuales)
        {
            bool yaTieneViajeEsaHora = TramosMiHorario.Any(tramo => tramo.HoraFija.Contains(opcion.HoraSalida) && tramo.ReservasDeLaSemana.Any(reserva => reserva.TieneViaje));
            if (yaTieneViajeEsaHora)
            {
                await Shell.Current.DisplayAlert(AppResources.PassengerDash_ScheduleClash, AppResources.PassengerDash_AlreadyBookedHour, AppResources.PassengerDash_Understood);
                return;
            }
        }

        bool hayIda = SeleccionesActuales.Any(o => o.EsIda);
        bool hayRegreso = SeleccionesActuales.Any(o => !o.EsIda);
        if (hayIda && hayRegreso)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_MixedDirectionError, AppResources.Global_Ok);
            return;
        }

        await Shell.Current.Navigation.PushAsync(new PassGold.Views.ConfirmarReservaPage(SeleccionesActuales.ToList()));
    }

    [RelayCommand]
    public async Task ConfirmarReservaAsync()
    {
        if (ViajeParaReservar == null || Shell.Current == null) return;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, "Necesitas internet para hacer una reserva.", AppResources.Global_Ok); return;
        }

        string direccionFinal = PuntoRecogidaTexto;

        if (UsarNuevaUbicacion)
        {
            if (string.IsNullOrWhiteSpace(PuntoRecogidaTexto))
            {
                await Shell.Current.DisplayAlert(AppResources.Global_Attention, "Por favor ingresa la indicación exacta de recogida.", AppResources.Global_Ok); return;
            }
            direccionFinal = PuntoRecogidaTexto.Trim();
        }
        else if (UbicacionSeleccionada != null)
        {
            direccionFinal = UbicacionSeleccionada.DireccionTexto;
        }

        IsLoading = true;
        string currentUserId = "";
        var authUser = _supabaseService.GetCurrentUser();

        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            currentUserId = authUser.Id;
        else
        {
            var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync();
            if (sesionLocal != null) currentUserId = sesionLocal.Id;
        }

        if (!string.IsNullOrEmpty(currentUserId))
        {
            try
            {
                if (UsarNuevaUbicacion)
                {
                    var nuevaUb = new UbicacionPasajero
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdPasajero = currentUserId,
                        Alias = "Ubicación Guardada",
                        DireccionTexto = direccionFinal
                    };
                    await _supabaseService.CreateUbicacionAsync(nuevaUb);
                    MisUbicaciones.Insert(0, nuevaUb);
                }

                var reserva = new Reserva
                {
                    Id = Guid.NewGuid().ToString(),
                    IdAsignacion = ViajeParaReservar.IdAsignacion,
                    IdPasajero = currentUserId,
                    PuntoRecogidaTexto = direccionFinal,
                    Estado = "pendiente",
                    CreadoEn = DateTime.UtcNow
                };

                bool ok = await _supabaseService.CreateReservaAsync(reserva);
                if (ok)
                {
                    await Shell.Current.DisplayAlert("¡Listo!", "¡Asiento Reservado con éxito!", AppResources.Global_Ok);
                }
                else { await Shell.Current.DisplayAlert(AppResources.Global_Error, "Hubo un problema procesando la reserva.", AppResources.Global_Ok); }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.Global_Error, $"La app falló al procesar: {ex.Message}", AppResources.Global_Ok);
            }
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, "Tu sesión expiró. Inicia sesión nuevamente.", AppResources.Global_Ok);
        }

        IsLoading = false;
    }
}