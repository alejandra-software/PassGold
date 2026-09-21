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

namespace PassGold.ViewModels;

public partial class EditTripViewModel : ObservableObject, IQueryAttributable
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private string clasificacionViaje = "";

    [ObservableProperty] private TimeSpan nuevaHora = new(6, 30, 0);
    [ObservableProperty] private TimeSpan horaInicioRecorrido = new(4, 50, 0);
    [ObservableProperty] private TimeSpan horaLlegadaDestino = new(6, 20, 0);

    public ObservableCollection<string> TiposViaje { get; } = new();
    [ObservableProperty] private string selectedTipoViaje = "";

    public ObservableCollection<string> OpcionesAlcance { get; } = new();
    [ObservableProperty] private string alcanceSeleccionado = "";

    public ObservableCollection<Usuario> ChoferesActivos { get; } = new();
    public ObservableCollection<Vehiculo> VehiculosActivos { get; } = new();

    [ObservableProperty] private Usuario? nuevoChofer;
    [ObservableProperty] private Vehiculo? nuevoVehiculo;

    public ObservableCollection<AsignacionTemporal> MicrobusesDeApoyo { get; } = new();

    public ObservableCollection<WaypointTemporal> MetasDelViaje { get; } = new();
    public bool HasMetas => MetasDelViaje.Count > 0;

    public ObservableCollection<WaypointTemporal> DestinosFrecuentes { get; } = new();
    public bool HasDestinosFrecuentes => DestinosFrecuentes.Count > 0;

    [ObservableProperty] private WaypointTemporal? destinoFrecuenteSeleccionado;

    [ObservableProperty] private bool isMapModalVisible = false;

    //  Vista satélite/híbrida
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType()
    {
        CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }
    [ObservableProperty] private string nuevoAliasPunto = "";
    [ObservableProperty] private double latitudTemporal = 13.9946;
    [ObservableProperty] private double longitudTemporal = -89.5597;

    private Asignacion? _asignacionOriginal;
    private Viaje? _viajeOriginal;

    // ?? NUEVO: Memoria para guardar todas las unidades (principal y apoyos) al cargar
    private List<Asignacion> _asignacionesOriginalesDelViaje = new();

    public EditTripViewModel()
    {
        TiposViaje.Add("Ida (Hacia Universidad)");
        TiposViaje.Add("Regreso (Hacia Casa)");
        TiposViaje.Add("Viaje Especial");

        OpcionesAlcance.Add("Solo este viaje (Hoy)");
        OpcionesAlcance.Add("Aplicar por 1 Semana (Pr�ximos 7 d�as)");
        OpcionesAlcance.Add("Aplicar por 2 Semanas (Pr�ximos 14 d�as)");
        OpcionesAlcance.Add("Aplicar por 1 Mes (Pr�ximos 30 d�as)");
        OpcionesAlcance.Add("Toda la serie (Hoy y Futuro infinito)");
        AlcanceSeleccionado = OpcionesAlcance[0];

        MetasDelViaje.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMetas));
        DestinosFrecuentes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasDestinosFrecuentes));
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


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("AsignacionSeleccionada", out var asignacionObj) && asignacionObj is Asignacion asig)
        {
            _asignacionOriginal = asig;
            NuevaHora = asig.Fecha.AHoraElSalvador().TimeOfDay;
            CargarDestinosFrecuentes();
            await CargarDatosFlotaAsync();
        }
    }

    private async Task CargarDatosFlotaAsync()
    {
        IsLoading = true;
        var authUser = _supabaseService.GetCurrentUser();

        if (authUser != null && _asignacionOriginal != null)
        {
            var usuarioActual = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (usuarioActual != null)
            {
                IsFleetManager = usuarioActual.Rol.Contains("jefe", StringComparison.OrdinalIgnoreCase) ||
                                 usuarioActual.Rol.Contains("independiente", StringComparison.OrdinalIgnoreCase);
            }

            var viajesActivos = await _supabaseService.GetAllActiveTripsAsync();
            _viajeOriginal = viajesActivos.FirstOrDefault(v => v.Id == _asignacionOriginal.IdViaje);

            if (_viajeOriginal != null)
            {
                SelectedTipoViaje = TiposViaje.FirstOrDefault(t => t.Equals(_viajeOriginal.TipoViaje, StringComparison.OrdinalIgnoreCase)) ?? _viajeOriginal.TipoViaje;
                HoraInicioRecorrido = _viajeOriginal.HoraInicioRecorrido ?? NuevaHora.Subtract(TimeSpan.FromMinutes(100));
                HoraLlegadaDestino = _viajeOriginal.HoraLlegadaDestino ?? NuevaHora.Subtract(TimeSpan.FromMinutes(10));

                MetasDelViaje.Clear();
                if (!string.IsNullOrWhiteSpace(_viajeOriginal.MetaTexto) && _viajeOriginal.MetaLatitud.HasValue && _viajeOriginal.MetaLongitud.HasValue)
                {
                    MetasDelViaje.Add(new WaypointTemporal
                    {
                        Alias = _viajeOriginal.MetaTexto,
                        Latitud = _viajeOriginal.MetaLatitud.Value,
                        Longitud = _viajeOriginal.MetaLongitud.Value
                    });
                }
            }

            string ownerId = IsFleetManager ? authUser.Id : (usuarioActual?.IdJefe ?? authUser.Id);

            var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(ownerId);
            VehiculosActivos.Clear();
            foreach (var v in vehiculos.Where(v => v.Estado == "activo")) VehiculosActivos.Add(v);

            ChoferesActivos.Clear();
            if (usuarioActual != null && IsFleetManager) ChoferesActivos.Add(usuarioActual);

            var empleados = await _supabaseService.GetEmployeesByBossAsync(ownerId);
            foreach (var emp in empleados) ChoferesActivos.Add(emp);

            // ?? SOLUCI�N: Cargar todas las asignaciones para este mismo viaje y hora
            var inicioConsulta = _asignacionOriginal.Fecha.AddDays(-1);
            var finConsulta = _asignacionOriginal.Fecha.AddDays(1);
            var idsFlota = ChoferesActivos.Select(c => c.Id).ToList();

            var asignacionesSemana = await _supabaseService.GetAsignacionesPorRangoAsync(inicioConsulta, finConsulta, idsFlota);

            _asignacionesOriginalesDelViaje = asignacionesSemana
                .Where(a => a.IdViaje == _viajeOriginal?.Id && a.Fecha == _asignacionOriginal.Fecha)
                .ToList();

            // Configurar el Chofer y Veh�culo Principal
            NuevoChofer = ChoferesActivos.FirstOrDefault(c => c.Id == _asignacionOriginal.IdChofer);
            NuevoVehiculo = VehiculosActivos.FirstOrDefault(v => v.Id == _asignacionOriginal.IdVehiculo);

            // Poblar los Microbuses de Apoyo detectados
            MicrobusesDeApoyo.Clear();
            foreach (var asigApoyo in _asignacionesOriginalesDelViaje.Where(a => a.Id != _asignacionOriginal.Id))
            {
                var choferApoyo = ChoferesActivos.FirstOrDefault(c => c.Id == asigApoyo.IdChofer) ?? new Usuario();
                var vehiculoApoyo = VehiculosActivos.FirstOrDefault(v => v.Id == asigApoyo.IdVehiculo) ?? new Vehiculo();

                MicrobusesDeApoyo.Add(new AsignacionTemporal
                {
                    Chofer = choferApoyo,
                    Vehiculo = vehiculoApoyo
                });
            }
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
        if (Shell.Current == null) return;

        if (!IsFleetManager)
        {
            await Shell.Current.DisplayAlert("Acceso Restringido", "Solo el Chofer Jefe o Independiente puede modificar la programaci�n de flotilla.", "OK");
            return;
        }

        if (MetasDelViaje.Count == 0)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Schedule_MapErrorNoWaypoints, AppResources.Global_Ok);
            return;
        }

        if (NuevoChofer == null || NuevoVehiculo == null)
        {
            await Shell.Current.DisplayAlert("Atenci�n", "Selecciona un chofer principal y un microb�s.", "OK");
            return;
        }

        foreach (var apoyo in MicrobusesDeApoyo)
        {
            if (apoyo.Chofer == null || string.IsNullOrEmpty(apoyo.Chofer.Id) ||
                apoyo.Vehiculo == null || string.IsNullOrEmpty(apoyo.Vehiculo.Id))
            {
                await Shell.Current.DisplayAlert("Atenci�n", "Tienes un bloque de 'Microb�s de Apoyo' incompleto. Selecciona un chofer y unidad, o elim�nalo presionando la X.", "OK");
                return;
            }
        }

        if (_asignacionOriginal == null || _viajeOriginal == null) return;

        IsLoading = true;

        DateTime fechaInicio = _asignacionOriginal.Fecha.AHoraElSalvador().Date;
        DateTime? fechaLimite = null;

        if (AlcanceSeleccionado == OpcionesAlcance[1]) fechaLimite = fechaInicio.AddDays(7);
        else if (AlcanceSeleccionado == OpcionesAlcance[2]) fechaLimite = fechaInicio.AddDays(14);
        else if (AlcanceSeleccionado == OpcionesAlcance[3]) fechaLimite = fechaInicio.AddDays(30);
        else if (AlcanceSeleccionado == OpcionesAlcance[4]) fechaLimite = null; // Toda la serie

        bool esEdicionUnica = AlcanceSeleccionado == OpcionesAlcance[0];
        bool exito = false;

        if (!esEdicionUnica)
        {
            // Actualizaci�n global / rango
            exito = await _supabaseService.UpdateRutaCompletaAsync(_viajeOriginal.Id, _asignacionOriginal.Fecha, NuevoChofer.Id, NuevoVehiculo.Id, NuevaHora);
        }
        else
        {
            // ?? SOLUCI�N: Limpieza y Reasignaci�n para un d�a espec�fico
            DateTime fechaAjustada = _asignacionOriginal.Fecha.AHoraElSalvador().Date.Add(NuevaHora);
            exito = await _supabaseService.UpdateAsignacionIndividualAsync(_asignacionOriginal.Id, NuevoChofer.Id, NuevoVehiculo.Id, fechaAjustada);

            if (exito)
            {
                // 1. Borramos todos los microbuses de apoyo antiguos para limpiar el terreno
                foreach (var oldAsig in _asignacionesOriginalesDelViaje.Where(a => a.Id != _asignacionOriginal.Id))
                {
                    await _supabaseService.DeleteAsignacionIndividualAsync(oldAsig.Id);
                }

                // 2. Insertamos la configuraci�n de apoyos actual que el usuario dej� en pantalla
                if (MicrobusesDeApoyo.Count > 0)
                {
                    var asignacionesApoyo = new List<Asignacion>();
                    DateTime fechaBase = fechaAjustada.DesdeElSalvadorAUtc();

                    foreach (var apoyo in MicrobusesDeApoyo)
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
                    await _supabaseService.CreateAssignmentsBulkAsync(asignacionesApoyo);
                }
            }
        }

        IsLoading = false;

        if (exito)
        {
            await Shell.Current.DisplayAlert("�Listo!", "Los cambios han sido guardados y publicados exitosamente.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "Ocurri� un error al intentar guardar los cambios.", "OK");
        }
    }

    [RelayCommand]
    public async Task EliminarViajeAsync()
    {
        if (_asignacionOriginal == null || _viajeOriginal == null || Shell.Current == null) return;

        if (!IsFleetManager)
        {
            await Shell.Current.DisplayAlert("Acceso Restringido", "Solo el Chofer Jefe o Independiente puede cancelar viajes.", "OK");
            return;
        }

        bool confirmar = await Shell.Current.DisplayAlert("?? Confirmar Eliminaci�n",
            "�Est�s seguro de que deseas eliminar este viaje? Esta acci�n no se puede deshacer.", "S�, Eliminar", "Cancelar");

        if (!confirmar) return;

        IsLoading = true;

        bool esEliminacionUnica = AlcanceSeleccionado == OpcionesAlcance[0];
        bool exito = false;

        if (!esEliminacionUnica)
        {
            exito = await _supabaseService.DeleteRutaCompletaAsync(_viajeOriginal.Id);
        }
        else
        {
            // SOLUCION: Eliminar principal Y todos los de apoyo al cancelar el d�a completo
            foreach (var oldAsig in _asignacionesOriginalesDelViaje)
            {
                exito = await _supabaseService.DeleteAsignacionIndividualAsync(oldAsig.Id);
            }
        }

        IsLoading = false;

        if (exito)
        {
            await Shell.Current.DisplayAlert("�Listo!", "El viaje ha sido cancelado y eliminado del calendario.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "Ocurri� un problema al intentar eliminar el viaje.", "OK");
        }
    }
}