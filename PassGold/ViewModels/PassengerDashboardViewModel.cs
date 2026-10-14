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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

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
    public Color ColorFondo { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    public bool TieneViaje { get; set; } = false;
    // 🆕 Teléfono del chofer de ESE viaje (para llamarlo o escribirle por WhatsApp)
    public string TelefonoChofer { get; set; } = "";
    public bool TieneTelefono => !string.IsNullOrWhiteSpace(TelefonoChofer);
}

public class TramoMiHorario
{
    public string HoraFija { get; set; } = "";
    public ObservableCollection<BloqueMiReserva> ReservasDeLaSemana { get; set; } = new();
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

public partial class PassengerDashboardViewModel : ObservableObject, IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("FlotaParaCalendario", out var value) && value is Usuario flota)
        {
            var card = new FlotaPasajeroCard
            {
                FlotaOriginal = flota,
                NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? AppResources.F(AppResources.Fleet_DefaultName, flota.Nombre) : flota.NombreFlota,
                NombreJefe = flota.Nombre ?? "",
                FotoPerfil = flota.FotoPerfil ?? "",
                Telefono = flota.Telefono ?? ""
            };
            _ = SeleccionarFlotaAsync(card);
        }
    }

    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private List<Reserva> _reservasActivasPasajero = new();

    private LocalDatabaseService GetLocalDb() => Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private string studentName = AppResources.Dashboard_Loading;
    [ObservableProperty] private string fotoPerfilUsuario = "";
    [ObservableProperty] private bool hasFotoPerfilUsuario = false;

    partial void OnFotoPerfilUsuarioChanged(string value) => HasFotoPerfilUsuario = !string.IsNullOrWhiteSpace(value);

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private DateTime fechaInicioSemana;
    [ObservableProperty] private string textoRangoFechas = "";
    [ObservableProperty] private bool isViewingMySchedule = true;
    [ObservableProperty] private bool isViewingDirectory = false;
    [ObservableProperty] private string textoBusqueda = "";

    [ObservableProperty] private bool hasViajeEnCurso = false;
    [ObservableProperty] private string infoViajeEnCurso = "";
    [ObservableProperty] private string idReservaEnCurso = "";

    public ObservableCollection<string> CabecerasDias { get; } = new();
    public ObservableCollection<TramoMiHorario> TramosMiHorario { get; } = new();
    [ObservableProperty] private bool isMyScheduleEmpty = true;

    private List<FlotaPasajeroCard> _todasLasFlotas = new();
    public ObservableCollection<FlotaPasajeroCard> FlotasDisponibles { get; } = new();

    [ObservableProperty] private string selectedFleetName = "";
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

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MisUbicaciones.Clear();
                            foreach (var u in misUb.OrderByDescending(x => x.CreadoEn)) MisUbicaciones.Add(u);
                        });

                        await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevoPayload, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc }));
                        MainThread.BeginInvokeOnMainThread(() => { ProcesarDatosPasajero(nuevoPayload); });
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

        _reservasActivasPasajero = payload.MisReservas.Where(r => r.Estado != "cancelado").ToList();

        foreach (var flota in payload.DirectorioFlotas)
        {
            var rutasUnicas = new List<string>();
            if (!string.IsNullOrWhiteSpace(flota.RutasFlota))
                rutasUnicas = flota.RutasFlota.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).Distinct().ToList();
            if (rutasUnicas.Count == 0) rutasUnicas.Add(AppResources.Fleet_RoutesToAgree);

            var card = new FlotaPasajeroCard
            {
                IdFlota = flota.Id,
                NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? AppResources.F(AppResources.Fleet_DefaultName, flota.Nombre) : flota.NombreFlota,
                NombreJefe = flota.Nombre ?? "",
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
        HasViajeEnCurso = false;
        InfoViajeEnCurso = "";
        IdReservaEnCurso = "";

        var abrevDias = AppResources.WeekdayShortNames;
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

        TimeSpan horaActual = DateTime.Now.TimeOfDay;
        DateTime fechaHoyLocal = DateTime.Now.Date;

        if (!IsMyScheduleEmpty)
        {
            var agrupadoPorHora = reservasSemana.GroupBy(x => x.Asignacion!.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

            foreach (var grupo in agrupadoPorHora)
            {
                string horaEtiqueta = DateTime.Today.Add(grupo.Key).ToString("hh:mm tt", AppResources.Culture);
                bool esIda = AppResources.IsInboundTrip(grupo.First().Viaje!.TipoViaje);

                var tramo = new TramoMiHorario { HoraFija = $"{horaEtiqueta}\n{(esIda ? AppResources.Dash_ToUniversity : AppResources.Dash_ToHome)}" };

                for (int i = 0; i < 7; i++)
                {
                    DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                    var itemDelDia = grupo.FirstOrDefault(x => x.Asignacion!.Fecha.AHoraElSalvador().Date == fechaEvaluar);

                    if (itemDelDia != null)
                    {
                        var viajeInfo = itemDelDia.Viaje!;
                        bool esIdaDelDia = AppResources.IsInboundTrip(viajeInfo.TipoViaje);
                        var chofer = payload.ChoferesAsociados.FirstOrDefault(c => c.Id == itemDelDia.Asignacion!.IdChofer);
                        var vehiculo = payload.VehiculosAsociados.FirstOrDefault(v => v.Id == itemDelDia.Asignacion!.IdVehiculo);
                        string idJefe = chofer?.IdJefe ?? chofer?.Id ?? "";
                        var flota = payload.FlotasAsociadas.FirstOrDefault(f => f.Id == idJefe);

                        string nombreFlota = flota?.NombreFlota ?? AppResources.F(AppResources.Fleet_DefaultName, flota?.Nombre ?? "");
                        string infoTransporte = string.IsNullOrWhiteSpace(vehiculo?.Placa)
                            ? $"👤 {chofer?.Nombre ?? AppResources.Dashboard_Unknown}"
                            : $"👤 {chofer?.Nombre ?? AppResources.Dashboard_Unknown} \n🚐 {vehiculo!.Placa}";

                        tramo.ReservasDeLaSemana.Add(new BloqueMiReserva
                        {
                            TelefonoChofer = chofer?.Telefono ?? "",
                            IdReserva = itemDelDia.Reserva.Id,
                            InfoFlota = string.IsNullOrWhiteSpace(nombreFlota) ? AppResources.Dash_LocalFleet : nombreFlota,
                            InfoTransporte = infoTransporte,
                            InfoRuta = AppResources.TripTypeLabel(viajeInfo.TipoViaje),
                            ColorFondo = esIdaDelDia ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                            TieneViaje = true
                        });

                        if (fechaEvaluar == fechaHoyLocal && !HasViajeEnCurso)
                        {
                            TimeSpan horaInicioViaje = viajeInfo.HoraInicioRecorrido ?? viajeInfo.HoraSalida.AHoraElSalvador().TimeOfDay;
                            TimeSpan horaFinViaje = viajeInfo.HoraLlegadaDestino ?? horaInicioViaje.Add(TimeSpan.FromHours(2));

                            if (horaActual >= horaInicioViaje && horaActual <= horaFinViaje)
                            {
                                HasViajeEnCurso = true;
                                IdReservaEnCurso = itemDelDia.Reserva.Id;
                                InfoViajeEnCurso = $"{AppResources.TripTypeLabel(viajeInfo.TipoViaje)} - {infoTransporte.Replace("\n", " | ")}";
                            }
                        }
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
        bool confirmar = await Shell.Current.DisplayAlert(AppResources.Alert_CancelTrip, AppResources.Alert_CancelConfirm, AppResources.Alert_YesCancel, AppResources.Alert_GoBack);
        if (!confirmar) return;

        IsLoading = true;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Alert_NeedInternetCancel, AppResources.Global_Ok);
            IsLoading = false; return;
        }

        bool exito = await _supabaseService.DeleteReservaAsync(idReserva);
        if (exito)
        {
            await LoadDashboardAsync();
            if (_selectedFleet != null) await CargarHorarioFlotaAsync(_selectedFleet.Id);
            await Shell.Current.DisplayAlert(AppResources.Global_Done, AppResources.Alert_CancelSuccess, AppResources.Global_Ok);
        }
        else { await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.Alert_CancelError, AppResources.Global_Ok); }
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
        EstablecerSemanaActual();
        await CargarHorarioFlotaAsync(flotaCard.FlotaOriginal.Id);
    }

    private async Task CargarHorarioFlotaAsync(string idJefe)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoading = true;
            TramosDelDia.Clear();
            CabecerasDias.Clear();
            var abrevDias = AppResources.WeekdayShortNames;
            for (int i = 0; i < 7; i++)
            {
                CabecerasDias.Add($"{abrevDias[i]} {FechaInicioSemana.AddDays(i).Day}");
            }
        });

        try
        {
            string currentUserId = "";
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
            else { var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var misUb = await _supabaseService.GetUbicacionesPasajeroAsync(currentUserId);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MisUbicaciones.Clear();
                    foreach (var u in misUb.OrderByDescending(x => x.CreadoEn)) MisUbicaciones.Add(u);
                });
            }

            var equipo = await _supabaseService.GetEmployeesByBossAsync(idJefe);
            var dicChoferes = new Dictionary<string, string>();
            var idsEquipo = new List<string> { idJefe };
            foreach (var emp in equipo) { idsEquipo.Add(emp.Id); dicChoferes[emp.Id] = emp.Nombre; }

            var inicioReal = FechaInicioSemana.AddDays(-1);
            var finReal = FechaInicioSemana.AddDays(7);

            var asignaciones = await _supabaseService.GetAsignacionesPorRangoAsync(inicioReal, finReal, idsEquipo);
            var viajesBase = await _supabaseService.GetAllActiveTripsAsync();

            var asigSemanaEstricta = asignaciones.Where(a => a.Fecha.AHoraElSalvador().Date >= FechaInicioSemana.Date && a.Fecha.AHoraElSalvador().Date <= FechaInicioSemana.AddDays(6).Date).ToList();
            var tramosListos = new List<TramoHorarioPasajero>();

            if (asigSemanaEstricta.Count > 0)
            {
                var asignacionesConViaje = asigSemanaEstricta.Select(a => new { Asig = a, ViajeInfo = viajesBase.FirstOrDefault(v => v.Id == a.IdViaje) }).Where(x => x.ViajeInfo != null).ToList();
                var agrupadoPorHora = asignacionesConViaje.GroupBy(x => x.Asig.Fecha.AHoraElSalvador().TimeOfDay).OrderBy(g => g.Key);

                foreach (var grupoHora in agrupadoPorHora)
                {
                    string horaDisplay = DateTime.Today.Add(grupoHora.Key).ToString("hh:mm tt", AppResources.Culture);
                    var tramo = new TramoHorarioPasajero { HoraFija = horaDisplay };

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime fechaEvaluar = FechaInicioSemana.AddDays(i).Date;
                        var celdaDia = new CeldaCalendarioPasajero();
                        var asignacionesDelDia = grupoHora.Where(x => x.Asig.Fecha.AHoraElSalvador().Date == fechaEvaluar).ToList();

                        foreach (var asigDia in asignacionesDelDia)
                        {
                            bool esIda = AppResources.IsInboundTrip(asigDia.ViajeInfo!.TipoViaje);
                            var reservaExistente = _reservasActivasPasajero.FirstOrDefault(r => r.IdAsignacion == asigDia.Asig.Id);

                            celdaDia.Opciones.Add(new OpcionViajePasajero
                            {
                                IdAsignacion = asigDia.Asig.Id,
                                IdViajeBase = asigDia.ViajeInfo.Id,
                                IdChofer = asigDia.Asig.IdChofer,
                                FechaAsignacion = asigDia.Asig.Fecha.AHoraElSalvador(),
                                ChoferInfo = dicChoferes.TryGetValue(asigDia.Asig.IdChofer, out var nom) ? nom : AppResources.Dashboard_Unknown,
                                ColorFondo = esIda ? Color.FromArgb("#2E7D32") : Color.FromArgb("#1565C0"),
                                ColorTexto = Microsoft.Maui.Graphics.Colors.White,
                                TipoViajeCompleto = AppResources.TripTypeLabel(asigDia.ViajeInfo.TipoViaje),
                                HoraSalida = horaDisplay,
                                EsIda = esIda,
                                IsSelected = false,
                                YaReservado = reservaExistente != null,
                                IdReservaExistente = reservaExistente?.Id ?? ""
                            });
                        }
                        tramo.ViajesDeLaSemana.Add(celdaDia);
                    }
                    tramosListos.Add(tramo);
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var tramo in tramosListos) TramosDelDia.Add(tramo);
            });
        }
        catch { }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    public OpcionViajePasajero? ViajeParaReservar => SeleccionesActuales.FirstOrDefault();
    public ObservableCollection<OpcionViajePasajero> SeleccionesActuales { get; } = new();

    [RelayCommand]
    public void AbrirPanelReserva(OpcionViajePasajero opcion)
    {
        if (opcion == null) return;
        if (opcion.YaReservado) return;

        if (opcion.FechaAsignacion.Date < DateTime.Today)
        {
            Shell.Current?.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_PastDateError, AppResources.Global_Ok);
            return;
        }

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
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_NeedInternetToBook, AppResources.Global_Ok); return;
        }

        string direccionFinal = PuntoRecogidaTexto;

        if (UsarNuevaUbicacion)
        {
            if (string.IsNullOrWhiteSpace(PuntoRecogidaTexto))
            {
                await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Alert_EnterExactIndication, AppResources.Global_Ok); return;
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
                        Alias = AppResources.PDash_SavedLocationAlias,
                        DireccionTexto = direccionFinal
                    };
                    await _supabaseService.CreateUbicacionAsync(nuevaUb);
                    MainThread.BeginInvokeOnMainThread(() => MisUbicaciones.Insert(0, nuevaUb));
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
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (ok)
                    {
                        await Shell.Current.DisplayAlert(AppResources.Global_DoneExcl, AppResources.Alert_SeatBooked, AppResources.Global_Ok);
                    }
                    else { await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.Alert_BookingProblem, AppResources.Global_Ok); }
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.F(AppResources.Alert_AppFailed, ex.Message), AppResources.Global_Ok));
#else
                MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.Alert_BookingProblem, AppResources.Global_Ok));
#endif
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_SessionExpired, AppResources.Global_Ok));
        }

        MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
    }

    // ───────── 🆕 Contactar al chofer ─────────

    // Números de El Salvador (8 dígitos) se completan con +503
    private static string NormalizarTelefono(string telefono)
    {
        var digitos = new string((telefono ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 8) digitos = "503" + digitos;
        return digitos;
    }

    [RelayCommand]
    public async Task LlamarChoferAsync(string telefono)
    {
        var numero = NormalizarTelefono(telefono);
        if (string.IsNullOrEmpty(numero) || Shell.Current == null) return;
        try { await Launcher.Default.OpenAsync(new Uri($"tel:+{numero}")); }
        catch { await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.F(AppResources.PDash_CantOpenPhone, numero), AppResources.Global_Ok); }
    }

    [RelayCommand]
    public async Task WhatsAppChoferAsync(string telefono)
    {
        var numero = NormalizarTelefono(telefono);
        if (string.IsNullOrEmpty(numero) || Shell.Current == null) return;
        try { await Launcher.Default.OpenAsync(new Uri($"https://wa.me/{numero}")); }
        catch { await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.F(AppResources.PDash_CantOpenWhatsApp, numero), AppResources.Global_Ok); }
    }

    [RelayCommand]
    public async Task VerMapaChoferAsync(string idReserva)
    {
        if (string.IsNullOrEmpty(idReserva) || Shell.Current == null) return;

        IsLoading = true;

        var reserva = _reservasActivasPasajero.FirstOrDefault(r => r.Id == idReserva);
        if (reserva == null)
        {
            IsLoading = false;
            return;
        }

        string currentUserId = _supabaseService.GetCurrentUser()?.Id ?? "";
        string cacheKey = $"PassengerData_{currentUserId}_{FechaInicioSemana:yyyyMMdd}";
        var localDb = GetLocalDb();
        string? cacheJson = await localDb.LeerCacheAsync(cacheKey);

        if (string.IsNullOrEmpty(cacheJson))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PDash_TripLoadError, AppResources.Global_Ok);
            IsLoading = false;
            return;
        }

        var payload = JsonConvert.DeserializeObject<PassengerCachePayload>(cacheJson, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
        var asignacion = payload?.AsignacionesAsociadas.FirstOrDefault(a => a.Id == reserva.IdAsignacion);
        var viaje = asignacion != null ? payload?.ViajesAsociados.FirstOrDefault(v => v.Id == asignacion.IdViaje) : null;

        if (asignacion == null || viaje == null)
        {
            IsLoading = false;
            return;
        }

        DateTime fechaAsignacionLocal = asignacion.Fecha.AHoraElSalvador().Date;

        TimeSpan horaInicio = viaje.HoraInicioRecorrido ?? viaje.HoraSalida.AHoraElSalvador().TimeOfDay;
        TimeSpan horaFin = viaje.HoraLlegadaDestino ?? horaInicio.Add(TimeSpan.FromHours(2));

        DateTime inicioRastreo = fechaAsignacionLocal.Add(horaInicio);
        DateTime finRastreo = fechaAsignacionLocal.Add(horaFin);
        DateTime horaActual = DateTime.Now;

        if (horaActual < inicioRastreo)
        {
            await Shell.Current.DisplayAlert(AppResources.PDash_NotYetTitle, AppResources.F(AppResources.PDash_TrackingNotYet, inicioRastreo.ToString("hh:mm tt", AppResources.Culture)), AppResources.PassengerDash_Understood);
            IsLoading = false;
            return;
        }

        if (horaActual > finRastreo)
        {
            await Shell.Current.DisplayAlert(AppResources.PDash_TripEndedTitle, AppResources.PDash_TripEndedMsg, AppResources.PassengerDash_Understood);
            IsLoading = false;
            return;
        }

        bool esIda = AppResources.IsInboundTrip(viaje.TipoViaje);

        if (!esIda)
        {
            var paradas = await _supabaseService.GetParadasByViajeAsync(viaje.Id);
            var primeraParada = paradas.OrderBy(p => p.Orden).FirstOrDefault();

            if (primeraParada != null && (reserva.PuntoRecogidaTexto == primeraParada.NombreLugar || primeraParada.Orden == 1))
            {
                await Shell.Current.DisplayAlert(AppResources.PDash_PrivacyTitle, AppResources.PDash_PrivacyMsg, AppResources.PassengerDash_Understood);
                IsLoading = false;
                return;
            }
        }

        var navigationParams = new Dictionary<string, object>
        {
            // 🔧 Antes mandaba "IdChoferRastreo", pero el mapa espera "IdAsignacionRastreo":
            // abría sin saber qué micro seguir.
            { "IdAsignacionRastreo", asignacion.Id }
        };

        IsLoading = false;
        await Shell.Current.GoToAsync("mapa-pasajero", navigationParams);
    }
}