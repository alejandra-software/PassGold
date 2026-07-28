using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using GoldeenRide.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GoldeenRide.ViewModels;

public partial class EditTripViewModel : ObservableObject, IQueryAttributable
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private string rutaActualInfo = "";
    [ObservableProperty] private TimeSpan nuevaHora;

    public ObservableCollection<string> OpcionesAlcance { get; } = new();
    [ObservableProperty] private string alcanceSeleccionado = "";

    public ObservableCollection<Usuario> ChoferesActivos { get; } = new();
    public ObservableCollection<Vehiculo> VehiculosActivos { get; } = new();

    [ObservableProperty] private Usuario? nuevoChofer;
    [ObservableProperty] private Vehiculo? nuevoVehiculo;

    // Lista para añadir microbuses extra a la misma ruta y hora
    public ObservableCollection<AsignacionTemporal> MicrobusesDeApoyo { get; } = new();

    private Asignacion? _asignacionOriginal;
    private Viaje? _viajeOriginal;

    public EditTripViewModel()
    {
        OpcionesAlcance.Add("Solo este viaje (Hoy)");
        OpcionesAlcance.Add("Toda la ruta (Hoy y Futuro)");
        AlcanceSeleccionado = OpcionesAlcance[0];
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("AsignacionSeleccionada", out var asignacionObj) && asignacionObj is Asignacion asig)
        {
            _asignacionOriginal = asig;
            NuevaHora = asig.Fecha.ToLocalTime().TimeOfDay;
            await CargarDatosFlotaAsync();
        }
    }

    private async Task CargarDatosFlotaAsync()
    {
        IsLoading = true;
        var authUser = _supabaseService.GetCurrentUser();

        if (authUser != null && _asignacionOriginal != null)
        {
            // Cargar datos del viaje base
            var viajesActivos = await _supabaseService.GetAllActiveTripsAsync();
            _viajeOriginal = viajesActivos.FirstOrDefault(v => v.Id == _asignacionOriginal.IdViaje);

            if (_viajeOriginal != null)
                RutaActualInfo = $"{_viajeOriginal.RutaGeneral} ({_viajeOriginal.TipoViaje})";

            // Cargar choferes y vehículos del jefe
            var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(authUser.Id);
            VehiculosActivos.Clear();
            foreach (var v in vehiculos) VehiculosActivos.Add(v);

            var jefe = await _supabaseService.GetUserDataAsync(authUser.Id);
            ChoferesActivos.Clear();
            if (jefe != null) ChoferesActivos.Add(jefe);

            var empleados = await _supabaseService.GetEmployeesByBossAsync(authUser.Id);
            foreach (var emp in empleados) ChoferesActivos.Add(emp);

            // Pre-seleccionar los valores actuales
            NuevoChofer = ChoferesActivos.FirstOrDefault(c => c.Id == _asignacionOriginal.IdChofer);
            NuevoVehiculo = VehiculosActivos.FirstOrDefault(v => v.Id == _asignacionOriginal.IdVehiculo);
        }
        IsLoading = false;
    }

    [RelayCommand]
    public void AgregarMicrobusApoyo()
    {
        MicrobusesDeApoyo.Add(new AsignacionTemporal { Chofer = new Usuario(), Vehiculo = new Vehiculo() });
    }

    [RelayCommand]
    public void QuitarMicrobusApoyo(AsignacionTemporal apoyo)
    {
        if (apoyo != null) MicrobusesDeApoyo.Remove(apoyo);
    }

    [RelayCommand]
    public async Task GuardarCambiosAsync()
    {
        if (NuevoChofer == null || NuevoVehiculo == null)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_SelectDriverBus, AppResources.Global_Ok);
            return;
        }

        if (_asignacionOriginal == null || _viajeOriginal == null) return;

        IsLoading = true;
        bool esActualizacionGlobal = AlcanceSeleccionado == OpcionesAlcance[1];
        bool exito = false;

        // 1. Actualizar el viaje principal
        if (esActualizacionGlobal)
        {
            exito = await _supabaseService.UpdateRutaCompletaAsync(_viajeOriginal.Id, _asignacionOriginal.Fecha, NuevoChofer.Id, NuevoVehiculo.Id, NuevaHora);
        }
        else
        {
            DateTime fechaAjustada = _asignacionOriginal.Fecha.ToLocalTime().Date.Add(NuevaHora);
            exito = await _supabaseService.UpdateAsignacionIndividualAsync(_asignacionOriginal.Id, NuevoChofer.Id, NuevoVehiculo.Id, fechaAjustada);
        }

        // 2. Guardar los microbuses de apoyo (se crean como nuevas asignaciones en paralelo)
        if (exito && MicrobusesDeApoyo.Count > 0)
        {
            var asignacionesApoyo = new List<Asignacion>();
            DateTime fechaBase = _asignacionOriginal.Fecha.ToLocalTime().Date.Add(NuevaHora).ToUniversalTime();

            foreach (var apoyo in MicrobusesDeApoyo)
            {
                if (!string.IsNullOrEmpty(apoyo.Chofer?.Id) && !string.IsNullOrEmpty(apoyo.Vehiculo?.Id))
                {
                    asignacionesApoyo.Add(new Asignacion
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdViaje = _viajeOriginal.Id,
                        IdChofer = apoyo.Chofer.Id,
                        IdVehiculo = apoyo.Vehiculo.Id,
                        Fecha = fechaBase
                    });
                }
            }

            if (asignacionesApoyo.Count > 0)
                await _supabaseService.CreateAssignmentsBulkAsync(asignacionesApoyo);
        }

        IsLoading = false;

        if (exito && Shell.Current != null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Ok, "Los cambios han sido guardados y publicados exitosamente.", AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, "Ocurrió un error al intentar guardar los cambios.", AppResources.Global_Ok);
        }
    }

    [RelayCommand]
    public async Task EliminarViajeAsync()
    {
        if (_asignacionOriginal == null || _viajeOriginal == null || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert("⚠️ Confirmar Eliminación",
            "¿Estás seguro de que deseas eliminar este viaje? Esta acción no se puede deshacer.", "Sí, Eliminar", "Cancelar");

        if (!confirmar) return;

        IsLoading = true;
        bool esEliminacionGlobal = AlcanceSeleccionado == OpcionesAlcance[1];
        bool exito = false;

        if (esEliminacionGlobal)
        {
            exito = await _supabaseService.DeleteRutaCompletaAsync(_viajeOriginal.Id);
        }
        else
        {
            exito = await _supabaseService.DeleteAsignacionIndividualAsync(_asignacionOriginal.Id);
        }

        IsLoading = false;

        if (exito)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Ok, "El viaje ha sido cancelado y eliminado del calendario.", AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, "Ocurrió un problema al intentar eliminar el viaje.", AppResources.Global_Ok);
        }
    }
}