// Evita el choque con Android.App.Application al compilar para Android
using Application = Microsoft.Maui.Controls.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using PassGold.Helpers;
using PassGold.Models;
using PassGold.Services;
using System;
using Microsoft.Maui.Maps;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace PassGold.ViewModels;

public class AsignacionTemporal
{
    public Usuario Chofer { get; set; } = new();
    public Vehiculo Vehiculo { get; set; } = new();
    public string Detalles => $"🚐 {Vehiculo.Placa}  |  👤 {Chofer.Nombre}";
}

public class WaypointTemporal
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Alias { get; set; } = "";
    public string Detalle { get; set; } = "";
}

public partial class ScheduleTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    public ObservableCollection<string> TiposViaje { get; } = new();
    public ObservableCollection<Vehiculo> VehiculosDisponibles { get; } = new();
    public ObservableCollection<Usuario> ChoferesDisponibles { get; } = new();
    public ObservableCollection<AsignacionTemporal> Asignaciones { get; } = new();

    public ObservableCollection<WaypointTemporal> MetasDelViaje { get; } = new();
    public bool HasMetas => MetasDelViaje.Count > 0;

    public ObservableCollection<WaypointTemporal> DestinosFrecuentes { get; } = new();
    public bool HasDestinosFrecuentes => DestinosFrecuentes.Count > 0;

    [ObservableProperty] private WaypointTemporal? destinoFrecuenteSeleccionado;

    [ObservableProperty] private string selectedTipoViaje = "";
    [ObservableProperty] private TimeSpan horaSalida = new(6, 30, 0);
    [ObservableProperty] private TimeSpan horaInicioRecorrido = new(4, 50, 0);
    [ObservableProperty] private TimeSpan horaLlegadaDestino = new(6, 20, 0);

    // Default range logic fixed to support future planning
    [ObservableProperty] private DateTime fechaInicio = DateTime.Today;
    [ObservableProperty] private DateTime fechaFin = DateTime.Today.AddMonths(1);

    [ObservableProperty] private Vehiculo? selectedVehiculo;
    [ObservableProperty] private Usuario? selectedChofer;

    [ObservableProperty] private bool diaL = true;
    [ObservableProperty] private bool diaM = true;
    [ObservableProperty] private bool diaMi = true;
    [ObservableProperty] private bool diaJ = true;
    [ObservableProperty] private bool diaV = true;
    [ObservableProperty] private bool diaS = false;
    [ObservableProperty] private bool diaD = false;

    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isLoading = false;

    [ObservableProperty] private bool isMapModalVisible = false;

    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType()
    {
        CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }
    [ObservableProperty] private string nuevoAliasPunto = "";

    [ObservableProperty] private double latitudTemporal = 13.9946;
    [ObservableProperty] private double longitudTemporal = -89.5597;

    private Usuario? _jefeActual;
    private string? _idDuenoFlota;

    partial void OnSelectedTipoViajeChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        bool esIda = AppResources.IsInboundTrip(value);
        if (esIda)
        {
            HoraInicioRecorrido = HoraSalida.Subtract(TimeSpan.FromMinutes(100));
            HoraLlegadaDestino = HoraSalida.Subtract(TimeSpan.FromMinutes(10));
        }
        else
        {
            HoraInicioRecorrido = HoraSalida;
            HoraLlegadaDestino = HoraSalida.Add(TimeSpan.FromMinutes(90));
        }
    }

    partial void OnHoraSalidaChanged(TimeSpan value) { OnSelectedTipoViajeChanged(SelectedTipoViaje); }

    partial void OnDestinoFrecuenteSeleccionadoChanged(WaypointTemporal? value)
    {
        if (value != null)
        {
            if (!MetasDelViaje.Any(m => m.Alias.Equals(value.Alias, StringComparison.OrdinalIgnoreCase)))
            {
                MetasDelViaje.Add(new WaypointTemporal { Alias = value.Alias, Latitud = value.Latitud, Longitud = value.Longitud });
            }
            MainThread.BeginInvokeOnMainThread(() => DestinoFrecuenteSeleccionado = null);
        }
    }

    public ScheduleTripViewModel()
    {
        TiposViaje.Add(AppResources.TripType_Inbound);
        TiposViaje.Add(AppResources.TripType_Outbound);
        TiposViaje.Add(AppResources.TripType_Special);

        if (TiposViaje.Count > 0) SelectedTipoViaje = TiposViaje[0];

        MetasDelViaje.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMetas));
        DestinosFrecuentes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasDestinosFrecuentes));

        _ = LoadDataAsync();
    }

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
                        {
                            unicos[d.Alias.ToLowerInvariant()] = d;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Error leyendo preferencias locales: {ex.Message}");
        }

        try
        {
            // 🔧 Filtra del lado de Supabase en vez de traer todos los viajes de
            // todas las flotas y filtrar acá en el celular.
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
            Debug.WriteLine($"⚠️ Error conectando con Supabase para destinos: {ex.Message}");
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            DestinosFrecuentes.Clear();
            foreach (var item in unicos.Values.OrderBy(x => x.Alias))
            {
                DestinosFrecuentes.Add(item);
            }
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
                if (!DestinosFrecuentes.Any(x => x.Alias.Equals(meta.Alias, StringComparison.OrdinalIgnoreCase)))
                {
                    DestinosFrecuentes.Add(meta);
                }
            });
        }
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        // 1. Avisar al hilo principal que encienda el spinner de carga
        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        try
        {
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            {
                _jefeActual = await _supabaseService.GetUserDataAsync(authUser.Id);
                if (_jefeActual != null)
                {
                    // 🆕 También puede programar un empleado con permiso (sub-jefe):
                    // los viajes quedan a nombre del dueño de la flota.
                    IsFleetManager = RolesFlota.PuedeGestionarViajes(_jefeActual);
                    string ownerId = RolesFlota.IdFlota(_jefeActual);
                    _idDuenoFlota = ownerId;
                    Usuario? duenoFlota = ownerId == _jefeActual.Id ? _jefeActual : await _supabaseService.GetUserDataAsync(ownerId);

                    await CargarDestinosFrecuentesFlotaAsync(ownerId);

                    // 2. Descargar los datos en segundo plano (Correcto)
                    var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(ownerId);
                    var empleados = new List<Usuario>();

                    if (IsFleetManager)
                    {
                        empleados = await _supabaseService.GetEmployeesByBossAsync(ownerId);
                    }

                    // 3. 🔥 SOLUCIÓN: Empujar los datos a la interfaz estrictamente en el Hilo Principal
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        VehiculosDisponibles.Clear();
                        foreach (var v in vehiculos.Where(veh => veh.Estado == "activo"))
                            VehiculosDisponibles.Add(v);

                        ChoferesDisponibles.Clear();
                        if (IsFleetManager)
                        {
                            if (duenoFlota != null) ChoferesDisponibles.Add(duenoFlota);
                            foreach (var emp in empleados)
                                ChoferesDisponibles.Add(emp);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cargando datos de programación: {ex.Message}");
            // 🆕 Antes esto se perdía en silencio (solo Debug.WriteLine, invisible sin
            // Visual Studio conectado). Ahora se guarda Y se muestra al toque.
            PassGold.Services.CrashLogger.Guardar(ex, "ScheduleTripViewModel.LoadDataAsync");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Application.Current?.MainPage != null)
                {
#if DEBUG
                    await Application.Current.MainPage.DisplayAlert(AppResources.Schedule_LoadScreenError, ex.ToString(), AppResources.Global_Ok);
#else
                    await Application.Current.MainPage.DisplayAlert(AppResources.Schedule_LoadScreenError, AppResources.App_UnexpectedCloseBody, AppResources.Global_Ok);
#endif
                }
            });
        }
        finally
        {
            // 4. Apagar el spinner de carga de forma segura
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public void AbrirMapaMetas()
    {
        NuevoAliasPunto = "";
        IsMapModalVisible = true;
    }

    [RelayCommand]
    public void CerrarMapa()
    {
        IsMapModalVisible = false;
    }

    [RelayCommand]
    public async Task ConfirmarPuntoMapaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoAliasPunto))
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoName, AppResources.Global_Ok);
            return;
        }

        IsLoading = true;
        string detalleUbicacion = AppResources.Schedule_MapDefaultLocation;

        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(LatitudTemporal, LongitudTemporal);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                detalleUbicacion = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}".Trim(new char[] { ' ', ',' });
                if (string.IsNullOrWhiteSpace(detalleUbicacion)) detalleUbicacion = placemark.FeatureName ?? AppResources.Schedule_MapDetectedLocation;
            }
        }
        catch { /* Ignorar silenciosamente si no hay internet o el GPS falla */ }

        var nuevaMeta = new WaypointTemporal
        {
            Alias = NuevoAliasPunto,
            Detalle = detalleUbicacion,
            Latitud = LatitudTemporal,
            Longitud = LongitudTemporal
        };

        MetasDelViaje.Add(nuevaMeta);
        GuardarDestinoFrecuente(nuevaMeta);

        IsMapModalVisible = false;
        IsLoading = false;
    }

    [RelayCommand]
    public void EliminarMeta(WaypointTemporal meta) { if (meta != null) MetasDelViaje.Remove(meta); }

    partial void OnSelectedChoferChanged(Usuario? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.IdVehiculoDefault))
        {
            var vehiculoPorDefecto = VehiculosDisponibles.FirstOrDefault(v => v.Id == value.IdVehiculoDefault);
            if (vehiculoPorDefecto != null) SelectedVehiculo = vehiculoPorDefecto;
        }
        else SelectedVehiculo = null;
    }

    [RelayCommand]
    public void AddAsignacion()
    {
        if (SelectedChofer == null || SelectedVehiculo == null) return;
        if (Asignaciones.Any(a => a.Chofer.Id == SelectedChofer.Id || a.Vehiculo.Id == SelectedVehiculo.Id)) return;
        Asignaciones.Add(new AsignacionTemporal { Chofer = SelectedChofer, Vehiculo = SelectedVehiculo });
        SelectedChofer = null; SelectedVehiculo = null;
    }

    [RelayCommand] public void RemoveAsignacion(AsignacionTemporal asignacion) { if (asignacion != null) Asignaciones.Remove(asignacion); }

    [RelayCommand]
    public async Task SaveTripAsync()
    {
        if (Application.Current?.MainPage == null) return;

        if (MetasDelViaje.Count == 0)
        {
            await Application.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoWaypoints, AppResources.Global_Ok);
            return;
        }
        if (Asignaciones.Count == 0)
        {
            await Application.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_AddAtLeastOne, AppResources.Global_Ok);
            return;
        }
        if (_jefeActual == null) return;

        IsLoading = true;

        try
        {
            var primeraMeta = MetasDelViaje.First();

            var viajeBase = new Viaje
            {
                Id = Guid.NewGuid().ToString(),
                TipoViaje = AppResources.TripTypeToDb(SelectedTipoViaje),
                // Align to the specific Date selected by the user, rather than forcing today's date
                HoraSalida = FechaInicio.Date.Add(HoraSalida).DesdeElSalvadorAUtc(),
                HoraInicioRecorrido = HoraInicioRecorrido,
                HoraLlegadaDestino = HoraLlegadaDestino,
                Estado = "activo",
                DiasSemana = ConstruirDiasSemanaTexto(),
                FechaInicio = FechaInicio,
                FechaFin = FechaFin,
                IdCreador = _idDuenoFlota ?? _jefeActual.Id,
                MetaTexto = primeraMeta.Alias,
                MetaLatitud = primeraMeta.Latitud,
                MetaLongitud = primeraMeta.Longitud
            };

            var (viajeCreado, _) = await _supabaseService.CreateTripAndReturnAsync(viajeBase);
            if (viajeCreado == null)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    IsLoading = false;
                    await Application.Current.MainPage.DisplayAlert(AppResources.Global_Error, AppResources.Schedule_SaveError, AppResources.Global_Ok);
                });
                return;
            }

            var paradasNuevas = MetasDelViaje.Select((meta, index) => new ViajeParada
            {
                Id = Guid.NewGuid().ToString(),
                IdViaje = viajeCreado.Id,
                Orden = index + 1,
                NombreLugar = meta.Alias,
                Latitud = meta.Latitud,
                Longitud = meta.Longitud
            }).ToList();

            await _supabaseService.CreateParadasBulkAsync(paradasNuevas);

            var fechas = ConstruirFechasDelRango();
            var asignacionesNuevas = new List<Asignacion>();
            foreach (var fecha in fechas)
            {
                foreach (var item in Asignaciones)
                {
                    asignacionesNuevas.Add(new Asignacion
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdViaje = viajeCreado.Id,
                        IdChofer = item.Chofer.Id,
                        IdVehiculo = item.Vehiculo.Id,
                        Fecha = fecha.Add(HoraSalida).DesdeElSalvadorAUtc(),
                        Estado = "activo"
                    });
                }
            }

            var (exito, _) = await _supabaseService.CreateAssignmentsBulkAsync(asignacionesNuevas);

            MainThread.BeginInvokeOnMainThread(async () => {
                IsLoading = false;

                if (!exito)
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.Global_Error, AppResources.Schedule_SaveError, AppResources.Global_Ok);
                    return;
                }

                await Application.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_TripSaved, AppResources.Global_Ok);
                await Shell.Current.GoToAsync("..");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            MainThread.BeginInvokeOnMainThread(async () => {
                IsLoading = false;
                await Application.Current.MainPage.DisplayAlert(AppResources.Global_Error, AppResources.Schedule_SaveError, AppResources.Global_Ok);
            });
        }
    }

    private string ConstruirDiasSemanaTexto()
    {
        var dias = new List<string>();
        if (DiaL) dias.Add("Mo");
        if (DiaM) dias.Add("Tu");
        if (DiaMi) dias.Add("We");
        if (DiaJ) dias.Add("Th");
        if (DiaV) dias.Add("Fr");
        if (DiaS) dias.Add("Sa");
        if (DiaD) dias.Add("Su");
        return string.Join(", ", dias);
    }

    private List<DateTime> ConstruirFechasDelRango()
    {
        var diasActivos = new HashSet<DayOfWeek>();
        if (DiaL) diasActivos.Add(DayOfWeek.Monday);
        if (DiaM) diasActivos.Add(DayOfWeek.Tuesday);
        if (DiaMi) diasActivos.Add(DayOfWeek.Wednesday);
        if (DiaJ) diasActivos.Add(DayOfWeek.Thursday);
        if (DiaV) diasActivos.Add(DayOfWeek.Friday);
        if (DiaS) diasActivos.Add(DayOfWeek.Saturday);
        if (DiaD) diasActivos.Add(DayOfWeek.Sunday);

        var fechas = new List<DateTime>();
        for (var fecha = FechaInicio.Date; fecha <= FechaFin.Date; fecha = fecha.AddDays(1))
        {
            if (diasActivos.Contains(fecha.DayOfWeek)) fechas.Add(fecha);
        }
        return fechas;
    }
}