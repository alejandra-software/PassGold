using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoldeenRide.ViewModels;

public class AsignacionTemporal
{
    public Usuario Chofer { get; set; } = new();
    public Vehiculo Vehiculo { get; set; } = new();
    public string Detalles => $"🚐 {Vehiculo.Placa}  |  👤 {Chofer.Nombre}";
}

public partial class ScheduleTripViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    public ObservableCollection<string> TiposViaje { get; } = [];
    public ObservableCollection<Vehiculo> VehiculosDisponibles { get; } = [];
    public ObservableCollection<Usuario> ChoferesDisponibles { get; } = [];
    public ObservableCollection<AsignacionTemporal> Asignaciones { get; } = [];

    [ObservableProperty] private string selectedTipoViaje = "";
    [ObservableProperty] private TimeSpan horaSalida = new(6, 30, 0);

    [ObservableProperty] private TimeSpan horaInicioRecorrido = new(4, 50, 0);
    [ObservableProperty] private TimeSpan horaLlegadaDestino = new(6, 20, 0);

    [ObservableProperty] private string rutaTexto = "";
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

    private Usuario? _jefeActual;

    partial void OnSelectedTipoViajeChanged(string value)
    {
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

    partial void OnHoraSalidaChanged(TimeSpan value)
    {
        OnSelectedTipoViajeChanged(SelectedTipoViaje);
    }

    public static string GetString(string key, string fallback)
    {
        if (Microsoft.Maui.Controls.Application.Current != null && Microsoft.Maui.Controls.Application.Current.Resources.TryGetValue(key, out var val))
            return val?.ToString() ?? fallback;
        return fallback;
    }

    public ScheduleTripViewModel()
    {
        TiposViaje.Add(GetString("TripType_Inbound", "Ida (Hacia Universidad)"));
        TiposViaje.Add(GetString("TripType_Outbound", "Regreso (Hacia Casa)"));
        TiposViaje.Add(GetString("TripType_Special", "Especial"));

        if (TiposViaje.Count > 0) SelectedTipoViaje = TiposViaje[0];

        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
        {
            _jefeActual = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (_jefeActual != null)
            {
                IsFleetManager = _jefeActual.Rol.Contains("jefe", StringComparison.OrdinalIgnoreCase);
                var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(_jefeActual.Id);
                VehiculosDisponibles.Clear();
                foreach (var v in vehiculos) VehiculosDisponibles.Add(v);

                ChoferesDisponibles.Clear();
                ChoferesDisponibles.Add(_jefeActual);
                if (IsFleetManager)
                {
                    var empleados = await _supabaseService.GetEmployeesByBossAsync(_jefeActual.Id);
                    foreach (var emp in empleados) ChoferesDisponibles.Add(emp);
                }
            }
        }
        IsLoading = false;
    }

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
        if (SelectedChofer == null || SelectedVehiculo == null)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), GetString("Schedule_SelectDriverBus", "Por favor selecciona un Chofer y un Microbús."), GetString("Global_Ok", "OK"));
            return;
        }

        if (Asignaciones.Any(a => a.Chofer.Id == SelectedChofer.Id || a.Vehiculo.Id == SelectedVehiculo.Id))
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), GetString("Schedule_DriverAlreadyInList", "Ese chofer o microbús ya está en la lista."), GetString("Global_Ok", "OK"));
            return;
        }

        Asignaciones.Add(new AsignacionTemporal { Chofer = SelectedChofer, Vehiculo = SelectedVehiculo });
        SelectedChofer = null;
        SelectedVehiculo = null;
    }

    [RelayCommand]
    public void RemoveAsignacion(AsignacionTemporal asignacion)
    {
        if (asignacion != null) Asignaciones.Remove(asignacion);
    }

    [RelayCommand]
    public async Task SaveTripAsync()
    {
        if (Shell.Current == null) return;
        if (Asignaciones.Count == 0)
        {
            await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), GetString("Schedule_AddAtLeastOne", "Añade al menos un Chofer y un Microbús."), GetString("Global_Ok", "OK"));
            return;
        }

        var diasSeleccionados = GetDiasSeleccionados();
        if (diasSeleccionados.Count == 0)
        {
            await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), GetString("Schedule_SelectOneDay", "Selecciona al menos un día."), GetString("Global_Ok", "OK"));
            return;
        }

        IsLoading = true;

        var viajesActivos = await _supabaseService.GetAllActiveTripsAsync();
        var asignacionesExistentes = await _supabaseService.GetAsignacionesPorRangoAsync(FechaInicio, FechaFin);
        var fechasAfectadas = GetFechasAfectadas(diasSeleccionados);

        foreach (var date in fechasAfectadas)
        {
            var asigsEnEsteDia = asignacionesExistentes.Where(a => a.Fecha.ToLocalTime().Date == date.Date);
            foreach (var asigDB in asigsEnEsteDia)
            {
                var viajeDB = viajesActivos.FirstOrDefault(v => v.Id == asigDB.IdViaje);

                if (viajeDB != null && viajeDB.HoraSalida.ToLocalTime().TimeOfDay == HoraSalida)
                {
                    var conflicto = Asignaciones.FirstOrDefault(a => a.Chofer.Id == asigDB.IdChofer || a.Vehiculo.Id == asigDB.IdVehiculo);
                    if (conflicto != null)
                    {
                        IsLoading = false;
                        string template = GetString("Schedule_ClashMessage", "Choque con {0} y vehículo {1}");
                        string alertMessage = string.Format(template, conflicto.Chofer.Nombre, conflicto.Vehiculo.Placa, date.ToString("dd/MM/yyyy"), HoraSalida.ToString());
                        await Shell.Current.DisplayAlert(GetString("Schedule_ScheduleClash", "Choque de Horario"), alertMessage, GetString("Schedule_Fix", "Corregir"));
                        return;
                    }
                }
            }
        }

        DateTime horaSalidaLocal = DateTime.SpecifyKind(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, HoraSalida.Hours, HoraSalida.Minutes, 0), DateTimeKind.Local);
        var currentUser = _supabaseService.GetCurrentUser();

        var viajeBase = new Viaje
        {
            Id = Guid.NewGuid().ToString(),
            IdCreador = _jefeActual?.Id ?? currentUser?.Id, // 🔥 FIX: Nunca se guardará nulo
            TipoViaje = SelectedTipoViaje,
            RutaGeneral = RutaTexto,
            HoraSalida = horaSalidaLocal.ToUniversalTime(),
            HoraInicioRecorrido = HoraInicioRecorrido,
            HoraLlegadaDestino = HoraLlegadaDestino,
            Estado = "programado", // 🔥 FIX: Obligamos a que inicie programado
            DiasSemana = string.Join(", ", diasSeleccionados.Select(d => d.ToString()[..2])),
            FechaInicio = FechaInicio.ToUniversalTime(),
            FechaFin = FechaFin.ToUniversalTime()
        };

        var resultado = await _supabaseService.CreateTripAndReturnAsync(viajeBase);

        if (resultado.Viaje == null)
        {
            await Shell.Current.DisplayAlert(GetString("Schedule_DBError", "Error de BD"), string.Format(GetString("Schedule_RouteNotSaved", "No se guardó: {0}"), resultado.Error), GetString("Global_Ok", "OK"));
            IsLoading = false; return;
        }

        var asignacionesAGuardar = new List<Asignacion>();
        foreach (var date in fechasAfectadas)
        {
            DateTime fechaLocalCombinada = DateTime.SpecifyKind(new DateTime(date.Year, date.Month, date.Day, HoraSalida.Hours, HoraSalida.Minutes, 0), DateTimeKind.Local);

            foreach (var item in Asignaciones)
            {
                asignacionesAGuardar.Add(new Asignacion
                {
                    Id = Guid.NewGuid().ToString(),
                    IdViaje = resultado.Viaje.Id,
                    IdChofer = item.Chofer.Id,
                    IdVehiculo = item.Vehiculo.Id,
                    Fecha = fechaLocalCombinada.ToUniversalTime()
                });
            }
        }

        var resultBulk = await _supabaseService.CreateAssignmentsBulkAsync(asignacionesAGuardar);
        IsLoading = false;

        if (resultBulk.Success)
        {
            await Shell.Current.DisplayAlert(GetString("Schedule_Success", "¡Éxito!"), string.Format(GetString("Schedule_SuccessMessage", "Programados: {0} viajes"), asignacionesAGuardar.Count), GetString("Global_Ok", "OK"));
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(GetString("Global_Error", "Error"), resultBulk.Error, GetString("Global_Ok", "OK"));
        }
    }

    private List<DayOfWeek> GetDiasSeleccionados()
    {
        List<DayOfWeek> d = [];
        if (DiaL) d.Add(DayOfWeek.Monday);
        if (DiaM) d.Add(DayOfWeek.Tuesday);
        if (DiaMi) d.Add(DayOfWeek.Wednesday);
        if (DiaJ) d.Add(DayOfWeek.Thursday);
        if (DiaV) d.Add(DayOfWeek.Friday);
        if (DiaS) d.Add(DayOfWeek.Saturday);
        if (DiaD) d.Add(DayOfWeek.Sunday);
        return d;
    }

    private List<DateTime> GetFechasAfectadas(List<DayOfWeek> dias)
    {
        var fechas = new List<DateTime>();
        for (DateTime date = FechaInicio.Date; date <= FechaFin.Date; date = date.AddDays(1))
        {
            if (dias.Contains(date.DayOfWeek)) fechas.Add(date);
        }
        return fechas;
    }
}