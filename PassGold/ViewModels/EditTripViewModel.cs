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
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using Microsoft.Maui.ApplicationModel;
using Operador = Supabase.Postgrest.Constants.Operator;

namespace PassGold.ViewModels;

// Una "unidad" = un chofer + un microbús dentro de este horario.
// En hora pico un mismo viaje sale con 2 o más unidades (varias filas en
// "asignaciones" con el mismo id_viaje y la misma fecha).
public partial class UnidadEditable : ObservableObject
{
    // null = unidad nueva que todavía no existe en Supabase
    public string? IdAsignacionOriginal { get; init; }
    public string ChoferOriginalId { get; init; } = "";
    public string VehiculoOriginalId { get; init; } = "";
    public int ReservasDelDia { get; init; }

    [ObservableProperty] private Usuario? chofer;
    [ObservableProperty] private Vehiculo? vehiculo;
    [ObservableProperty] private string titulo = "";

    public bool EsNueva => IdAsignacionOriginal == null;
    public bool TieneReservas => ReservasDelDia > 0;
    public string TextoReservas => ReservasDelDia == 1
        ? AppResources.EditTrip_ReservedOne
        : AppResources.F(AppResources.EditTrip_ReservedMany, ReservasDelDia);
}

public partial class EditTripViewModel : ObservableObject, IQueryAttributable
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private string clasificacionViaje = "";

    [ObservableProperty] private DateTime fechaInicioAplicacion = DateTime.Today;
    [ObservableProperty] private DateTime fechaFinAplicacion = DateTime.Today;
    [ObservableProperty] private bool usandoRangoPersonalizado = false;

    [ObservableProperty] private TimeSpan nuevaHora = new(6, 30, 0);
    [ObservableProperty] private TimeSpan horaInicioRecorrido = new(4, 50, 0);
    [ObservableProperty] private TimeSpan horaLlegadaDestino = new(6, 20, 0);

    public ObservableCollection<string> TiposViaje { get; } = new();
    [ObservableProperty] private string selectedTipoViaje = "";

    public ObservableCollection<string> OpcionesAlcance { get; } = new();
    [ObservableProperty] private string alcanceSeleccionado = "";

    public ObservableCollection<Usuario> ChoferesActivos { get; } = new();
    public ObservableCollection<Vehiculo> VehiculosActivos { get; } = new();

    // 🆕 Todas las unidades de este horario (antes solo se veía una, la que se tocó
    // en el calendario, y los "microbuses de apoyo" ya existentes no aparecían).
    public ObservableCollection<UnidadEditable> Unidades { get; } = new();
    private readonly List<UnidadEditable> _unidadesQuitadas = new();
    [ObservableProperty] private string resumenUnidades = "";

    public ObservableCollection<WaypointTemporal> MetasDelViaje { get; } = new();
    public bool HasMetas => MetasDelViaje.Count > 0;

    public ObservableCollection<WaypointTemporal> DestinosFrecuentes { get; } = new();
    public bool HasDestinosFrecuentes => DestinosFrecuentes.Count > 0;

    [ObservableProperty] private WaypointTemporal? destinoFrecuenteSeleccionado;
    [ObservableProperty] private bool isMapModalVisible = false;
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType() => CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;

    [ObservableProperty] private string nuevoAliasPunto = "";
    [ObservableProperty] private double latitudTemporal = 13.9946;
    [ObservableProperty] private double longitudTemporal = -89.5597;

    private Asignacion? _asignacionOriginal;
    private Viaje? _viajeOriginal;

    public EditTripViewModel()
    {
        // Se muestran en el idioma de la app; al guardar se convierten al valor de la BD
        TiposViaje.Add(AppResources.TripType_Inbound);
        TiposViaje.Add(AppResources.TripType_Outbound);
        TiposViaje.Add(AppResources.TripType_Special);

        OpcionesAlcance.Add(AppResources.EditTrip_ScopeOnlyThis);
        OpcionesAlcance.Add(AppResources.EditTrip_ScopeWeek);
        OpcionesAlcance.Add(AppResources.EditTrip_ScopeMonth);
        OpcionesAlcance.Add(AppResources.EditTrip_ScopeSeries);
        OpcionesAlcance.Add(AppResources.EditTrip_ScopeCustom);
        AlcanceSeleccionado = OpcionesAlcance[0];

        MetasDelViaje.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMetas));
        DestinosFrecuentes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasDestinosFrecuentes));
        Unidades.CollectionChanged += (s, e) => ActualizarTitulosUnidades();
    }

    partial void OnAlcanceSeleccionadoChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        UsandoRangoPersonalizado = value == AppResources.EditTrip_ScopeCustom;

        if (value == AppResources.EditTrip_ScopeOnlyThis) FechaFinAplicacion = FechaInicioAplicacion;
        else if (value == AppResources.EditTrip_ScopeWeek) FechaFinAplicacion = FechaInicioAplicacion.AddDays(7);
        else if (value == AppResources.EditTrip_ScopeMonth) FechaFinAplicacion = FechaInicioAplicacion.AddDays(30);
        else if (value == AppResources.EditTrip_ScopeSeries) FechaFinAplicacion = FechaInicioAplicacion.AddYears(2);
    }

    partial void OnFechaInicioAplicacionChanged(DateTime value)
    {
        OnAlcanceSeleccionadoChanged(AlcanceSeleccionado);
    }

    // ───────────────────────── Destinos frecuentes y mapa (sin cambios) ─────────────────────────

    private async Task CargarDestinosFrecuentesFlotaAsync(string ownerId)
    {
        var unicos = new Dictionary<string, WaypointTemporal>();

        try
        {
            string json = Preferences.Default.Get("MisDestinosFrecuentes", "");
            if (!string.IsNullOrEmpty(json))
            {
                var listaLocal = JsonConvert.DeserializeObject<List<WaypointTemporal>>(json);
                if (listaLocal != null)
                {
                    foreach (var d in listaLocal)
                    {
                        if (!string.IsNullOrWhiteSpace(d.Alias) && !unicos.ContainsKey(d.Alias.ToLowerInvariant()))
                            unicos[d.Alias.ToLowerInvariant()] = d;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error leyendo preferencias locales: {ex.Message}");
        }

        try
        {
            var viajesActivos = await _supabaseService.GetAllActiveTripsAsync(new List<string> { ownerId });
            if (viajesActivos != null)
            {
                foreach (var viaje in viajesActivos)
                {
                    if (!string.IsNullOrWhiteSpace(viaje.MetaTexto) && viaje.MetaLatitud.HasValue && viaje.MetaLongitud.HasValue)
                    {
                        string clave = viaje.MetaTexto.ToLowerInvariant();
                        if (!unicos.ContainsKey(clave))
                        {
                            unicos[clave] = new WaypointTemporal
                            {
                                Alias = viaje.MetaTexto,
                                Latitud = viaje.MetaLatitud.Value,
                                Longitud = viaje.MetaLongitud.Value
                            };
                        }
                    }

                    try
                    {
                        var paradas = await _supabaseService.GetParadasByViajeAsync(viaje.Id);
                        if (paradas != null)
                        {
                            foreach (var p in paradas)
                            {
                                if (!string.IsNullOrWhiteSpace(p.NombreLugar) && p.Latitud.HasValue && p.Longitud.HasValue)
                                {
                                    string clave = p.NombreLugar.ToLowerInvariant();
                                    if (!unicos.ContainsKey(clave))
                                    {
                                        unicos[clave] = new WaypointTemporal
                                        {
                                            Alias = p.NombreLugar,
                                            Latitud = p.Latitud.Value,
                                            Longitud = p.Longitud.Value
                                        };
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error conectando con Supabase para destinos: {ex.Message}");
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            DestinosFrecuentes.Clear();
            foreach (var item in unicos.Values.OrderBy(x => x.Alias))
                DestinosFrecuentes.Add(item);
            OnPropertyChanged(nameof(HasDestinosFrecuentes));
        });
    }

    private void GuardarDestinoFrecuente(WaypointTemporal meta)
    {
        string json = Preferences.Default.Get("MisDestinosFrecuentes", "[]");
        var lista = JsonConvert.DeserializeObject<List<WaypointTemporal>>(json) ?? new List<WaypointTemporal>();

        if (!lista.Any(x => x.Alias.Equals(meta.Alias, StringComparison.OrdinalIgnoreCase)))
        {
            lista.Add(meta);
            Preferences.Default.Set("MisDestinosFrecuentes", JsonConvert.SerializeObject(lista));
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DestinosFrecuentes.Add(meta);
                OnPropertyChanged(nameof(HasDestinosFrecuentes));
            });
        }
    }

    partial void OnDestinoFrecuenteSeleccionadoChanged(WaypointTemporal? value)
    {
        if (value != null)
        {
            if (!MetasDelViaje.Any(m => m.Alias.Equals(value.Alias, StringComparison.OrdinalIgnoreCase)))
                MetasDelViaje.Add(new WaypointTemporal { Alias = value.Alias, Latitud = value.Latitud, Longitud = value.Longitud });
            MainThread.BeginInvokeOnMainThread(() => DestinoFrecuenteSeleccionado = null);
        }
    }

    [RelayCommand]
    public void AbrirMapaMetas() { NuevoAliasPunto = ""; IsMapModalVisible = true; }

    [RelayCommand]
    public void CerrarMapa() => IsMapModalVisible = false;

    [RelayCommand]
    public void ConfirmarPuntoMapa()
    {
        if (string.IsNullOrWhiteSpace(NuevoAliasPunto))
        {
            Shell.Current?.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoName, AppResources.Global_Ok);
            return;
        }
        var nuevaMeta = new WaypointTemporal { Alias = NuevoAliasPunto, Latitud = LatitudTemporal, Longitud = LongitudTemporal };
        MetasDelViaje.Add(nuevaMeta);
        GuardarDestinoFrecuente(nuevaMeta);
        IsMapModalVisible = false;
    }

    [RelayCommand]
    public void EliminarMeta(WaypointTemporal meta) { if (meta != null) MetasDelViaje.Remove(meta); }

    // ───────────────────────── Carga ─────────────────────────

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("AsignacionSeleccionada", out var asignacionObj) && asignacionObj is Asignacion asig)
        {
            _asignacionOriginal = asig;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FechaInicioAplicacion = asig.Fecha.AHoraElSalvador().Date;
                NuevaHora = asig.Fecha.AHoraElSalvador().TimeOfDay;
            });

            Task.Run(async () =>
            {
                try { await CargarDatosFlotaAsync(); }
                catch (Exception ex) { CrashLogger.Guardar(ex, "EditTripViewModel.ApplyQueryAttributes"); }
            });
        }
    }

    private async Task CargarDatosFlotaAsync()
    {
        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        try
        {
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser == null || _asignacionOriginal == null) return;

            var usuarioActual = await _supabaseService.GetUserDataAsync(authUser.Id);
            bool isManager = false;
            if (usuarioActual != null)
            {
                // 🆕 Jefe, independiente o sub-jefe
                isManager = RolesFlota.PuedeGestionarViajes(usuarioActual);
            }

            var viajeOrig = await _supabaseService.GetTripByIdAsync(_asignacionOriginal.IdViaje);
            List<ViajeParada> paradasExistentes = new();
            if (viajeOrig != null) paradasExistentes = await _supabaseService.GetParadasByViajeAsync(viajeOrig.Id);

            string ownerId = usuarioActual != null ? RolesFlota.IdFlota(usuarioActual) : authUser.Id;
            var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(ownerId);
            var empleados = await _supabaseService.GetEmployeesByBossAsync(ownerId);

            var choferes = new List<Usuario>();
            if (usuarioActual != null && isManager)
            {
                // El dueño de la flota también maneja; si quien edita es un sub-jefe, se agrega al dueño
                var duenoFlota = ownerId == usuarioActual.Id ? usuarioActual : await _supabaseService.GetUserDataAsync(ownerId);
                if (duenoFlota != null) choferes.Add(duenoFlota);
            }
            choferes.AddRange(empleados);
            var vehiculosActivos = vehiculos.Where(v => v.Estado == "activo").ToList();

            // 🆕 Todas las unidades de ESTE viaje en ESTE día (no solo la que se tocó)
            DateTime dia = _asignacionOriginal.Fecha.AHoraElSalvador().Date;
            var idsFlota = choferes.Select(c => c.Id).ToList();
            var delDia = await _supabaseService.GetAsignacionesPorRangoAsync(dia, dia.AddDays(1).AddSeconds(-1), idsFlota);
            var hermanas = delDia.Where(a => a.IdViaje == _asignacionOriginal.IdViaje).ToList();
            if (!hermanas.Any(a => a.Id == _asignacionOriginal.Id)) hermanas.Add(_asignacionOriginal);
            hermanas = hermanas.OrderBy(a => a.CreadoEn).ToList();

            var unidades = new List<UnidadEditable>();
            foreach (var a in hermanas)
            {
                var reservas = await _supabaseService.GetReservasByAsignacionAsync(a.Id);
                var unidad = new UnidadEditable
                {
                    IdAsignacionOriginal = a.Id,
                    ChoferOriginalId = a.IdChofer,
                    VehiculoOriginalId = a.IdVehiculo,
                    ReservasDelDia = reservas.Count(r => r.Estado != "cancelado"),
                    // Mismas instancias que las listas de fotos, para que se vean marcadas
                    Chofer = choferes.FirstOrDefault(c => c.Id == a.IdChofer),
                    Vehiculo = vehiculosActivos.FirstOrDefault(v => v.Id == a.IdVehiculo)
                };
                unidades.Add(unidad);
            }

            await CargarDestinosFrecuentesFlotaAsync(ownerId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsFleetManager = isManager;
                _viajeOriginal = viajeOrig;

                if (_viajeOriginal != null)
                {
                    SelectedTipoViaje = AppResources.TripTypeLabel(_viajeOriginal.TipoViaje);
                    HoraInicioRecorrido = _viajeOriginal.HoraInicioRecorrido ?? NuevaHora.Subtract(TimeSpan.FromMinutes(100));
                    HoraLlegadaDestino = _viajeOriginal.HoraLlegadaDestino ?? NuevaHora.Subtract(TimeSpan.FromMinutes(10));

                    MetasDelViaje.Clear();
                    if (paradasExistentes != null && paradasExistentes.Count > 0)
                    {
                        foreach (var parada in paradasExistentes.OrderBy(p => p.Orden))
                            MetasDelViaje.Add(new WaypointTemporal { Alias = parada.NombreLugar, Latitud = parada.Latitud ?? 0, Longitud = parada.Longitud ?? 0 });
                    }
                    else if (!string.IsNullOrWhiteSpace(_viajeOriginal.MetaTexto) && _viajeOriginal.MetaLatitud.HasValue && _viajeOriginal.MetaLongitud.HasValue)
                    {
                        MetasDelViaje.Add(new WaypointTemporal { Alias = _viajeOriginal.MetaTexto, Latitud = _viajeOriginal.MetaLatitud.Value, Longitud = _viajeOriginal.MetaLongitud.Value });
                    }
                }

                VehiculosActivos.Clear();
                foreach (var v in vehiculosActivos) VehiculosActivos.Add(v);

                ChoferesActivos.Clear();
                foreach (var c in choferes) ChoferesActivos.Add(c);

                _unidadesQuitadas.Clear();
                Unidades.Clear();
                foreach (var u in unidades) Unidades.Add(u);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "EditTripViewModel.CargarDatosFlotaAsync");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.EditTrip_LoadError, AppResources.Global_Ok);
            });
        }
        finally { MainThread.BeginInvokeOnMainThread(() => IsLoading = false); }
    }

    // ───────────────────────── Unidades ─────────────────────────

    private void ActualizarTitulosUnidades()
    {
        for (int i = 0; i < Unidades.Count; i++)
        {
            var u = Unidades[i];
            u.Titulo = u.EsNueva ? AppResources.F(AppResources.EditTrip_UnitNew, i + 1) : AppResources.F(AppResources.EditTrip_Unit, i + 1);
        }

        int n = Unidades.Count;
        ResumenUnidades = n == 1
            ? AppResources.EditTrip_SummaryOneUnit
            : AppResources.F(AppResources.EditTrip_SummaryManyUnits, n);
    }

    [RelayCommand]
    public void AgregarUnidad()
    {
        if (!IsFleetManager) return;
        Unidades.Add(new UnidadEditable());
    }

    [RelayCommand]
    public async Task QuitarUnidadAsync(UnidadEditable? unidad)
    {
        if (unidad == null || Shell.Current == null || !IsFleetManager) return;

        if (Unidades.Count <= 1)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention,
                AppResources.EditTrip_NeedOneUnitDelete,
                AppResources.Global_Ok);
            return;
        }

        if (unidad.TieneReservas)
        {
            bool seguir = await Shell.Current.DisplayAlert(AppResources.EditTrip_UnitHasPassengersTitle,
                AppResources.F(AppResources.EditTrip_UnitHasPassengersMsg, unidad.TextoReservas.Replace("👥 ", "")),
                AppResources.EditTrip_YesRemove, AppResources.Global_No);
            if (!seguir) return;
        }

        Unidades.Remove(unidad);
        if (!unidad.EsNueva) _unidadesQuitadas.Add(unidad);
    }

    // ───────────────────────── Guardar ─────────────────────────

    [RelayCommand]
    public async Task GuardarCambiosAsync()
    {
        if (Shell.Current == null) return;
        if (!IsFleetManager) { await Shell.Current.DisplayAlert(AppResources.EditTrip_RestrictedTitle, AppResources.EditTrip_RestrictedMsg, AppResources.Global_Ok); return; }
        if (MetasDelViaje.Count == 0) { await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoWaypoints, AppResources.Global_Ok); return; }
        if (_asignacionOriginal == null || _viajeOriginal == null) return;

        if (Unidades.Count == 0) { await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.EditTrip_NeedOneUnit, AppResources.Global_Ok); return; }
        var incompleta = Unidades.FirstOrDefault(u => u.Chofer == null || u.Vehiculo == null);
        if (incompleta != null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.F(AppResources.EditTrip_UnitIncomplete, incompleta.Titulo), AppResources.Global_Ok);
            return;
        }
        var choferRepetido = Unidades.GroupBy(u => u.Chofer!.Id).FirstOrDefault(g => g.Count() > 1);
        if (choferRepetido != null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.F(AppResources.EditTrip_DriverDuplicated, choferRepetido.First().Chofer!.Nombre), AppResources.Global_Ok);
            return;
        }
        var microRepetido = Unidades.GroupBy(u => u.Vehiculo!.Id).FirstOrDefault(g => g.Count() > 1);
        if (microRepetido != null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.F(AppResources.EditTrip_BusDuplicated, microRepetido.First().Vehiculo!.Placa), AppResources.Global_Ok);
            return;
        }

        DateTime desde = FechaInicioAplicacion.Date;
        DateTime hasta = FechaFinAplicacion.Date < desde ? desde : FechaFinAplicacion.Date;
        bool esSerieCompleta = AlcanceSeleccionado == AppResources.EditTrip_ScopeSeries;

        // Copias para trabajar fuera del hilo principal
        var unidades = Unidades.ToList();
        var quitadas = _unidadesQuitadas.ToList();
        var metas = MetasDelViaje.ToList();
        var idsFlota = ChoferesActivos.Select(c => c.Id).ToList();
        TimeSpan hora = NuevaHora;
        var viaje = _viajeOriginal;

        IsLoading = true;

        var resultado = await Task.Run(() => GuardarEnSupabaseAsync(viaje, unidades, quitadas, metas, idsFlota, desde, hasta, hora, esSerieCompleta));

        IsLoading = false;

        if (resultado.Exito)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_DoneExcl, resultado.Mensaje, AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.EditTrip_NotAllSaved, resultado.Mensaje, AppResources.Global_Ok);
        }
    }

    private async Task<(bool Exito, string Mensaje)> GuardarEnSupabaseAsync(
        Viaje viaje, List<UnidadEditable> unidades, List<UnidadEditable> quitadas, List<WaypointTemporal> metas,
        List<string> idsFlota, DateTime desde, DateTime hasta, TimeSpan hora, bool esSerieCompleta)
    {
        var client = _supabaseService.Client;
        if (client == null) return (false, AppResources.Global_NoServerConnection);

        var avisos = new List<string>();

        try
        {
            // 1) Paradas / destinos (son del viaje entero, no de un día).
            //    Antes se borraban y se volvían a crear, pero si algún pasajero eligió
            //    una parada como punto de bajada, Supabase no deja borrarla y TODO el
            //    guardado fallaba. La función reemplazar_paradas las actualiza en su lugar.
            var (paradasOk, paradasError) = await CancelacionesService.ReemplazarParadasAsync(viaje.Id, metas);
            if (!paradasOk) return (false, AppResources.F(AppResources.EditTrip_WaypointsError, paradasError));

            // 2) Todas las asignaciones de este viaje dentro del rango, agrupadas por día
            var enRango = await _supabaseService.GetAsignacionesPorRangoAsync(desde, hasta.AddDays(1).AddSeconds(-1), idsFlota);
            var porDia = enRango.Where(a => a.IdViaje == viaje.Id)
                                .GroupBy(a => a.Fecha.AHoraElSalvador().Date)
                                .OrderBy(g => g.Key)
                                .ToList();

            var aActualizar = new List<Asignacion>();
            var aCrear = new List<Asignacion>();
            var movimientos = new List<(string Desde, string Hacia)>();   // reservas de una unidad quitada → otra
            var aBorrar = new List<string>();

            foreach (var grupo in porDia)
            {
                DateTime dia = grupo.Key;
                DateTime fechaNuevaUtc = dia.Add(hora).DesdeElSalvadorAUtc();
                var libres = grupo.ToList();

                // Se reconoce cada unidad en cada día por su chofer + microbús ORIGINALES.
                // Así se modifica la fila existente (y sus reservas siguen intactas) en vez
                // de borrarla y crear otra, que era lo que hacía antes.
                var conservadas = new List<string>();
                foreach (var u in unidades.Where(u => !u.EsNueva))
                {
                    var fila = libres.FirstOrDefault(a => a.IdChofer == u.ChoferOriginalId && a.IdVehiculo == u.VehiculoOriginalId);
                    if (fila == null) continue; // ese día el viaje tenía otra combinación: no se toca
                    libres.Remove(fila);
                    conservadas.Add(fila.Id);

                    bool cambia = fila.IdChofer != u.Chofer!.Id
                               || fila.IdVehiculo != u.Vehiculo!.Id
                               || fila.Fecha.AHoraElSalvador().TimeOfDay != hora;
                    if (cambia) aActualizar.Add(Normalizar(fila, u.Chofer!.Id, u.Vehiculo!.Id, fechaNuevaUtc));
                }

                var quitadasDelDia = new List<Asignacion>();
                foreach (var u in quitadas)
                {
                    var fila = libres.FirstOrDefault(a => a.IdChofer == u.ChoferOriginalId && a.IdVehiculo == u.VehiculoOriginalId);
                    if (fila == null) continue;
                    libres.Remove(fila);
                    quitadasDelDia.Add(fila);
                }

                // Filas de ese día que no corresponden a ninguna unidad de la pantalla:
                // solo se les ajusta la hora si cambió, para que el horario no quede partido.
                foreach (var fila in libres)
                {
                    conservadas.Add(fila.Id);
                    if (fila.Fecha.AHoraElSalvador().TimeOfDay != hora)
                        aActualizar.Add(Normalizar(fila, fila.IdChofer, fila.IdVehiculo, fechaNuevaUtc));
                }

                // Unidades nuevas: se agregan en cada día que este viaje ya sale
                foreach (var u in unidades.Where(u => u.EsNueva))
                {
                    var nueva = new Asignacion
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdViaje = viaje.Id,
                        IdChofer = u.Chofer!.Id,
                        IdVehiculo = u.Vehiculo!.Id,
                        Fecha = fechaNuevaUtc,
                        Estado = "programado"
                    };
                    aCrear.Add(nueva);
                    conservadas.Add(nueva.Id);
                }

                foreach (var q in quitadasDelDia)
                {
                    string? destino = conservadas.FirstOrDefault();
                    if (destino != null) movimientos.Add((q.Id, destino));
                    aBorrar.Add(q.Id);
                }
            }

            // 3) Actualizar en lotes (una llamada por cada 100 filas, no una por fila)
            foreach (var lote in EnLotes(aActualizar, 100))
                await client.From<Asignacion>().Upsert(lote);

            // 4) Crear las unidades nuevas ANTES de mover reservas hacia ellas
            if (aCrear.Count > 0)
            {
                var (ok, error) = await _supabaseService.CreateAssignmentsBulkAsync(aCrear);
                if (!ok) return (false, AppResources.F(AppResources.EditTrip_NewUnitsError, error));
            }

            // 5) Pasar las reservas de las unidades quitadas a otra unidad del mismo día
            //    (función mover_reservas de Supabase: el jefe no puede editar reservas
            //    ajenas directamente por las reglas de seguridad).
            foreach (var (idDesde, idHacia) in movimientos)
            {
                var (movOk, _, movError) = await CancelacionesService.MoverReservasAsync(idDesde, idHacia);
                if (!movOk) avisos.Add(AppResources.F(AppResources.EditTrip_MoveBookingsError, movError));
            }

            // 6) Borrar las unidades quitadas, pero SOLO las que ya no tienen reservas.
            //    Si alguna reserva no se pudo mover, esa unidad se queda y se avisa.
            //    Se borra con cancelar_asignaciones, que también limpia la ubicación en
            //    vivo (si no, Supabase tampoco deja borrar la asignación).
            if (aBorrar.Count > 0)
            {
                var conReservas = new HashSet<string>();
                foreach (var lote in EnLotes(aBorrar, 100))
                {
                    var resp = await client.From<Reserva>().Filter("id_asignacion", Operador.In, lote).Get();
                    foreach (var r in resp.Models ?? new List<Reserva>()) conReservas.Add(r.IdAsignacion);
                }

                var borrables = aBorrar.Where(id => !conReservas.Contains(id)).ToList();
                if (borrables.Count > 0)
                {
                    var (cancelOk, _, _, cancelError) = await CancelacionesService.CancelarAsignacionesAsync(borrables, AppResources.EditTrip_UnitRemovedReason);
                    if (!cancelOk) avisos.Add(AppResources.F(AppResources.EditTrip_RemoveUnitError, cancelError));
                }

                if (conReservas.Count > 0)
                    avisos.Add(AppResources.F(AppResources.EditTrip_DaysNotDeleted, conReservas.Count));
            }

            // 7) Datos del viaje (aplican a todos los días)
            try
            {
                var primeraMeta = metas.First();
                viaje.TipoViaje = string.IsNullOrWhiteSpace(SelectedTipoViaje) ? viaje.TipoViaje : AppResources.TripTypeToDb(SelectedTipoViaje);
                viaje.HoraInicioRecorrido = HoraInicioRecorrido;
                viaje.HoraLlegadaDestino = HoraLlegadaDestino;
                viaje.MetaTexto = primeraMeta.Alias;
                viaje.MetaLatitud = primeraMeta.Latitud;
                viaje.MetaLongitud = primeraMeta.Longitud;

                // Las fechas llegan ya en hora de El Salvador aunque digan "Utc"; si se
                // reenvían así, Supabase las guarda 6 h corridas cada vez que se edita.
                DateTime fechaPlantilla = viaje.HoraSalida.AHoraElSalvador().Date;
                TimeSpan horaPlantilla = esSerieCompleta ? hora : viaje.HoraSalida.AHoraElSalvador().TimeOfDay;
                viaje.HoraSalida = fechaPlantilla.Add(horaPlantilla).DesdeElSalvadorAUtc();
                viaje.CreadoEn = viaje.CreadoEn.DesdeElSalvadorAUtc();
                if (viaje.FechaInicio.HasValue) viaje.FechaInicio = viaje.FechaInicio.Value.DesdeElSalvadorAUtc();
                if (viaje.FechaFin.HasValue) viaje.FechaFin = viaje.FechaFin.Value.DesdeElSalvadorAUtc();

                await client.From<Viaje>().Where(x => x.Id == viaje.Id).Update(viaje);
            }
            catch (Exception ex)
            {
                CrashLogger.Guardar(ex, "EditTripViewModel.GuardarViaje");
                avisos.Add(AppResources.EditTrip_TripDataError);
            }

            int dias = porDia.Count;
            string resumen = dias == 1 ? AppResources.EditTrip_SavedOneDay : AppResources.F(AppResources.EditTrip_SavedManyDays, dias);
            if (movimientos.Count > 0) resumen += "\n" + AppResources.EditTrip_BookingsMoved;
            if (avisos.Count > 0) resumen += "\n\n⚠️ " + string.Join("\n⚠️ ", avisos);
            return (true, resumen);
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "EditTripViewModel.GuardarEnSupabaseAsync");
            return (false, AppResources.F(AppResources.EditTrip_SaveErrorDetail, ex.Message));
        }
    }

    // Copia lista para reenviar a Supabase con la fecha en UTC real
    private static Asignacion Normalizar(Asignacion fila, string idChofer, string idVehiculo, DateTime fechaUtc) => new()
    {
        Id = fila.Id,
        IdViaje = fila.IdViaje,
        IdChofer = idChofer,
        IdVehiculo = idVehiculo,
        Fecha = fechaUtc,
        Estado = fila.Estado,
        CreadoEn = fila.CreadoEn.DesdeElSalvadorAUtc()
    };

    private static IEnumerable<List<T>> EnLotes<T>(List<T> lista, int tamano)
    {
        for (int i = 0; i < lista.Count; i += tamano)
            yield return lista.Skip(i).Take(tamano).ToList();
    }

    // ───────────────────────── Eliminar / cancelar el horario completo ─────────────────────────

    [RelayCommand]
    public async Task EliminarViajeAsync()
    {
        if (_asignacionOriginal == null || _viajeOriginal == null || Shell.Current == null) return;
        if (!IsFleetManager)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Error_NotEmployee, AppResources.Global_Ok);
            return;
        }

        string opcionRango = await Shell.Current.DisplayActionSheet(
            AppResources.EditTrip_WhichDaysCancel,
            AppResources.Global_Cancel, null,
            AppResources.PassengerDash_ScopeOnlyThisDay,
            AppResources.PassengerDash_ScopeThisWeek,
            AppResources.PassengerDash_ScopeThisMonth,
            AppResources.PassengerDash_ScopeRestOfCycle);

        if (string.IsNullOrEmpty(opcionRango) || opcionRango == AppResources.Global_Cancel) return;

        bool todaLaSerie = opcionRango == AppResources.PassengerDash_ScopeRestOfCycle;
        DateTime inicioLocal = FechaInicioAplicacion.Date;
        DateTime finLocal = inicioLocal;
        if (opcionRango == AppResources.PassengerDash_ScopeThisWeek) finLocal = inicioLocal.AddDays(7);
        else if (opcionRango == AppResources.PassengerDash_ScopeThisMonth) finLocal = inicioLocal.AddDays(30);
        else if (todaLaSerie) finLocal = inicioLocal.AddYears(5);

        var viaje = _viajeOriginal;
        var idsFlota = ChoferesActivos.Select(c => c.Id).ToList();

        // 1) Qué se va a cancelar y cuántos pasajeros hay en esos días
        IsLoading = true;
        List<string> ids;
        int reservas;
        try
        {
            var enRango = await Task.Run(() => _supabaseService.GetAsignacionesPorRangoAsync(inicioLocal, finLocal.AddDays(1).AddSeconds(-1), idsFlota));
            ids = enRango.Where(a => a.IdViaje == viaje.Id).Select(a => a.Id).ToList();
            reservas = await Task.Run(() => CancelacionesService.ContarReservasActivasAsync(ids));
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "EditTripViewModel.EliminarViajeAsync.Contar");
            IsLoading = false;
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.EditTrip_CheckSlotError, AppResources.Global_Ok);
            return;
        }
        IsLoading = false;

        if (ids.Count == 0 && !todaLaSerie)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.EditTrip_NoTripsInRange, AppResources.Global_Ok);
            return;
        }

        // 2) Si hay pasajeros, el motivo es obligatorio: ellos lo van a ver
        string motivo = AppResources.EditTrip_DefaultCancelReason;
        if (reservas > 0)
        {
            string? elegido = await CancelacionesService.PedirMotivoAsync(reservas);
            if (elegido == null) return;
            motivo = elegido;
        }

        // 3) Confirmación con números reales
        string rangoTexto = todaLaSerie
            ? AppResources.F(AppResources.EditTrip_RangeFromOnward, inicioLocal.ToString(AppResources.Fmt_DayShortMonth, AppResources.Culture))
            : (finLocal == inicioLocal
                ? AppResources.F(AppResources.EditTrip_RangeSingle, inicioLocal.ToString(AppResources.Fmt_DayShortMonth, AppResources.Culture))
                : AppResources.F(AppResources.EditTrip_RangeBetween, inicioLocal.ToString(AppResources.Fmt_DayShortMonth, AppResources.Culture), finLocal.ToString(AppResources.Fmt_DayShortMonth, AppResources.Culture)));

        var confirmacion = new System.Text.StringBuilder();
        confirmacion.Append(AppResources.F(AppResources.EditTrip_WillCancel, rangoTexto));
        confirmacion.Append(ids.Count == 1 ? AppResources.EditTrip_OneTrip : AppResources.F(AppResources.EditTrip_ManyTrips, ids.Count));
        if (reservas > 0)
        {
            confirmacion.Append(reservas == 1
                ? AppResources.EditTrip_OneBookingCancelled
                : AppResources.F(AppResources.EditTrip_ManyBookingsCancelled, reservas));
            confirmacion.Append(AppResources.F(AppResources.EditTrip_ReasonTheyWillSee, motivo));
        }
        if (Unidades.Count > 1)
            confirmacion.Append(AppResources.EditTrip_RemoveOnlyOneHint);

        bool confirmar = await Shell.Current.DisplayAlert(AppResources.EditTrip_CancelTripsTitle, confirmacion.ToString(), AppResources.EditTrip_YesCancel, AppResources.Alert_GoBack);
        if (!confirmar) return;

        // 4) Cancelar (todo en una transacción en Supabase: o se hace completo, o no se hace nada)
        IsLoading = true;
        bool ok;
        string error;
        int pasajerosAvisados;
        if (todaLaSerie)
        {
            var r = await Task.Run(() => CancelacionesService.TerminarSerieAsync(viaje.Id, inicioLocal, motivo));
            ok = r.Ok; error = r.Error; pasajerosAvisados = r.Reservas;
        }
        else
        {
            var r = await Task.Run(() => CancelacionesService.CancelarAsignacionesAsync(ids, motivo));
            ok = r.Ok; error = r.Error; pasajerosAvisados = r.Reservas;
        }
        IsLoading = false;

        if (ok)
        {
            string fin = pasajerosAvisados > 0
                ? (pasajerosAvisados == 1 ? AppResources.EditTrip_CancelledOnePax : AppResources.F(AppResources.EditTrip_CancelledManyPax, pasajerosAvisados))
                : AppResources.EditTrip_Cancelled;
            await Shell.Current.DisplayAlert(AppResources.Schedule_Success, fin, AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.F(AppResources.EditTrip_NothingCancelled, error), AppResources.Global_Ok);
        }
    }
}