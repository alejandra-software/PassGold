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
}

public partial class ScheduleTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    public ObservableCollection<string> TiposViaje { get; } = [];
    public ObservableCollection<Vehiculo> VehiculosDisponibles { get; } = [];
    public ObservableCollection<Usuario> ChoferesDisponibles { get; } = [];
    public ObservableCollection<AsignacionTemporal> Asignaciones { get; } = [];

    public ObservableCollection<WaypointTemporal> MetasDelViaje { get; } = [];
    public bool HasMetas => MetasDelViaje.Count > 0;

    public ObservableCollection<WaypointTemporal> DestinosFrecuentes { get; } = [];
    public bool HasDestinosFrecuentes => DestinosFrecuentes.Count > 0;

    [ObservableProperty] private WaypointTemporal? destinoFrecuenteSeleccionado;

    [ObservableProperty] private string selectedTipoViaje = "";
    [ObservableProperty] private TimeSpan horaSalida = new(6, 30, 0);
    [ObservableProperty] private TimeSpan horaInicioRecorrido = new(4, 50, 0);
    [ObservableProperty] private TimeSpan horaLlegadaDestino = new(6, 20, 0);
    [ObservableProperty] private DateTime fechaInicio = DateTime.Today;
    [ObservableProperty] private DateTime fechaFin = DateTime.Today.AddMonths(5);
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

    //  Vista satélite/híbrida — gratis, es el mismo Maps SDK nativo que ya usa
    // toda la app, solo cambia el tipo de mosaico que se renderiza.
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

    partial void OnSelectedTipoViajeChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        bool esIda = value.Contains("Ida", StringComparison.OrdinalIgnoreCase) || value.Contains("Inbound", StringComparison.OrdinalIgnoreCase);
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

    private void CargarDestinosFrecuentes()
    {
        DestinosFrecuentes.Clear();
        string json = Preferences.Default.Get("MisDestinosFrecuentes", "");
        if (!string.IsNullOrEmpty(json))
        {
            var lista = JsonConvert.DeserializeObject<List<WaypointTemporal>>(json);
            if (lista != null)
            {
                foreach (var d in lista) DestinosFrecuentes.Add(d);
            }
        }
    }

    private void GuardarDestinoFrecuente(WaypointTemporal meta)
    {
        string json = Preferences.Default.Get("MisDestinosFrecuentes", "[]");
        var lista = JsonConvert.DeserializeObject<List<WaypointTemporal>>(json) ?? new List<WaypointTemporal>();

        if (!lista.Any(x => x.Alias.Equals(meta.Alias, StringComparison.OrdinalIgnoreCase)))
        {
            lista.Add(meta);
            Preferences.Default.Set("MisDestinosFrecuentes", JsonConvert.SerializeObject(lista));
            DestinosFrecuentes.Add(meta);
        }
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;

        //  FIX: Actualizamos la caché de pines cada vez que la página carga datos
        CargarDestinosFrecuentes();

        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
        {
            _jefeActual = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (_jefeActual != null)
            {
                IsFleetManager = _jefeActual.Rol.Contains("jefe", StringComparison.OrdinalIgnoreCase) || _jefeActual.Rol.Contains("independiente", StringComparison.OrdinalIgnoreCase);
                string ownerId = IsFleetManager ? _jefeActual.Id : (_jefeActual.IdJefe ?? _jefeActual.Id);

                var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(ownerId);
                VehiculosDisponibles.Clear();
                foreach (var v in vehiculos.Where(veh => veh.Estado == "activo")) VehiculosDisponibles.Add(v);

                ChoferesDisponibles.Clear();
                if (IsFleetManager)
                {
                    ChoferesDisponibles.Add(_jefeActual);
                    var empleados = await _supabaseService.GetEmployeesByBossAsync(ownerId);
                    foreach (var emp in empleados) ChoferesDisponibles.Add(emp);
                }
            }
        }
        IsLoading = false;
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
    public void ConfirmarPuntoMapa()
    {
        if (string.IsNullOrWhiteSpace(NuevoAliasPunto))
        {
            App.Current?.MainPage?.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoName, AppResources.Global_Ok);
            return;
        }

        var nuevaMeta = new WaypointTemporal
        {
            Alias = NuevoAliasPunto,
            Latitud = LatitudTemporal,
            Longitud = LongitudTemporal
        };

        MetasDelViaje.Add(nuevaMeta);
        GuardarDestinoFrecuente(nuevaMeta);

        IsMapModalVisible = false;
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
    public async Task AddAsignacionAsync()
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
        if (App.Current?.MainPage == null) return;
        if (MetasDelViaje.Count == 0) { await App.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoWaypoints, AppResources.Global_Ok); return; }
        if (Asignaciones.Count == 0) { await App.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_AddAtLeastOne, AppResources.Global_Ok); return; }
        if (_jefeActual == null) return;

        IsLoading = true;

        var primeraMeta = MetasDelViaje.First();

        var viajeBase = new Viaje
        {
            Id = Guid.NewGuid().ToString(),
            TipoViaje = SelectedTipoViaje,
            HoraSalida = DateTime.Today.Add(HoraSalida).DesdeElSalvadorAUtc(),
            HoraInicioRecorrido = HoraInicioRecorrido,
            HoraLlegadaDestino = HoraLlegadaDestino,
            Estado = "activo",
            DiasSemana = ConstruirDiasSemanaTexto(),
            FechaInicio = FechaInicio,
            FechaFin = FechaFin,
            IdCreador = _jefeActual.Id,
            MetaTexto = primeraMeta.Alias,
            MetaLatitud = primeraMeta.Latitud,
            MetaLongitud = primeraMeta.Longitud
        };

        var (viajeCreado, _) = await _supabaseService.CreateTripAndReturnAsync(viajeBase);
        if (viajeCreado == null)
        {
            IsLoading = false;
            await App.Current.MainPage.DisplayAlert(AppResources.Global_Error, AppResources.Schedule_SaveError, AppResources.Global_Ok);
            return;
        }

        //  Guardar TODAS las metas en viaje_paradas (antes solo se guardaba
        // MetasDelViaje.First() en viajes.meta_texto/meta_latitud/meta_longitud,
        // y el resto se perdía). MetaTexto/MetaLatitud/MetaLongitud del viaje
        // se mantienen igual que antes para no romper pantallas que ya dependen
        // de ese campo (ej. la "meta oficial" que ve el pasajero en el mapa).
        var paradasNuevas = MetasDelViaje.Select((meta, index) => new ViajeParada
        {
            Id = Guid.NewGuid().ToString(),
            IdViaje = viajeCreado.Id,
            Orden = index + 1,
            NombreLugar = meta.Alias,
            Latitud = meta.Latitud,
            Longitud = meta.Longitud
        }).ToList();

        var (paradasExito, paradasError) = await _supabaseService.CreateParadasBulkAsync(paradasNuevas);
        if (!paradasExito)
        {
            Debug.WriteLine($"⚠️ No se guardaron las paradas adicionales en viaje_paradas: {paradasError}");
            // No cortamos el flujo — el viaje y la meta principal ya se guardaron correctamente.
        }

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
        IsLoading = false;

        if (!exito)
        {
            await App.Current.MainPage.DisplayAlert(AppResources.Global_Error, AppResources.Schedule_SaveError, AppResources.Global_Ok);
            return;
        }

        await App.Current.MainPage.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_TripSaved, AppResources.Global_Ok);
        await Shell.Current.GoToAsync("..");
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