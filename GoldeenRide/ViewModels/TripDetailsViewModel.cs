using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GoldeenRide.ViewModels;

public class PasajeroItem
{
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string FotoUrl { get; set; } = "";
}

public class ChoferOpcion
{
    public string IdAsignacion { get; set; } = "";
    public string IdChofer { get; set; } = "";
    public string NombreChofer { get; set; } = "";
    public string PlacaVehiculo { get; set; } = "";
    public int CapacidadVehiculo { get; set; } = 0;
    public string DisplayName => $"🚐 {NombreChofer} ({PlacaVehiculo})";
}

[QueryProperty(nameof(IdAsignacion), "idAsignacion")]
public partial class TripDetailsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool _hayMultiplesChoferes = false;
    [ObservableProperty] private string _nombreChofer = "Cargando datos reales...";
    [ObservableProperty] private string _placaVehiculo = "Cargando...";
    [ObservableProperty] private int _capacidadVehiculo = 0;
    [ObservableProperty] private bool _listaVacia = true;

    [ObservableProperty] private int _tabIndex = 0;
    [ObservableProperty] private bool _isPasajerosTab = true;
    [ObservableProperty] private bool _isMapaTab = false;
    [ObservableProperty] private bool _isOperadorTab = false;

    private string _idAsignacion = "";
    public string IdAsignacion
    {
        get => _idAsignacion;
        set
        {
            if (SetProperty(ref _idAsignacion, value) && !string.IsNullOrEmpty(value))
            {
                _ = CargarDatosRealesDeSupabaseAsync();
            }
        }
    }

    private ChoferOpcion? _choferSeleccionado;
    public ChoferOpcion? ChoferSeleccionado
    {
        get => _choferSeleccionado;
        set
        {
            if (SetProperty(ref _choferSeleccionado, value) && value != null)
            {
                NombreChofer = value.NombreChofer;
                PlacaVehiculo = value.PlacaVehiculo;
                CapacidadVehiculo = value.CapacidadVehiculo; // Actualiza directo la variable visual
            }
        }
    }

    public ObservableCollection<ChoferOpcion> ChoferesAsignados { get; } = new();
    public ObservableCollection<PasajeroItem> Pasajeros { get; } = new();

    [RelayCommand]
    public void CambiarPestana(string tab)
    {
        if (tab == "Pasajeros") SetTab(0);
        else if (tab == "Mapa") SetTab(1);
        else if (tab == "Operador") SetTab(2);
    }

    [RelayCommand]
    public void SwipeLeft()
    {
        if (TabIndex < 2) SetTab(TabIndex + 1);
    }

    [RelayCommand]
    public void SwipeRight()
    {
        if (TabIndex > 0) SetTab(TabIndex - 1);
    }

    private void SetTab(int index)
    {
        TabIndex = index;
        IsPasajerosTab = index == 0;
        IsMapaTab = index == 1;
        IsOperadorTab = index == 2;
    }

    private async Task CargarDatosRealesDeSupabaseAsync()
    {
        try
        {
            var asignacionReal = await _supabaseService.GetAsignacionByIdAsync(IdAsignacion);
            if (asignacionReal == null) return;

            var todasAsignacionesMes = await _supabaseService.GetAsignacionesPorRangoAsync(asignacionReal.Fecha.AddDays(-1), asignacionReal.Fecha.AddDays(1));
            var asignacionesMismoViaje = todasAsignacionesMes
                .Where(a => a.IdViaje == asignacionReal.IdViaje && a.Fecha.Date == asignacionReal.Fecha.Date)
                .ToList();

            ChoferesAsignados.Clear();
            foreach (var asig in asignacionesMismoViaje)
            {
                var chInfo = await _supabaseService.GetUserDataAsync(asig.IdChofer);
                var vInfo = await _supabaseService.GetVehiculoByIdAsync(asig.IdVehiculo);

                ChoferesAsignados.Add(new ChoferOpcion
                {
                    IdAsignacion = asig.Id,
                    IdChofer = asig.IdChofer,
                    NombreChofer = chInfo?.Nombre ?? "Desconocido",
                    PlacaVehiculo = vInfo?.Placa ?? "Sin Placa",
                    CapacidadVehiculo = vInfo != null ? vInfo.Capacidad : 0
                });
            }

            HayMultiplesChoferes = ChoferesAsignados.Count > 1;
            ChoferSeleccionado = ChoferesAsignados.FirstOrDefault(c => c.IdAsignacion == IdAsignacion) ?? ChoferesAsignados.FirstOrDefault();

            var reservasReales = await _supabaseService.GetReservasByAsignacionAsync(IdAsignacion);
            Pasajeros.Clear();
            foreach (var reserva in reservasReales)
            {
                var pasajeroInfo = await _supabaseService.GetUserDataAsync(reserva.IdPasajero);
                Pasajeros.Add(new PasajeroItem { Nombre = pasajeroInfo?.Nombre ?? "Usuario Desconocido", Ubicacion = reserva.PuntoRecogidaTexto });
            }

            ListaVacia = Pasajeros.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Fallo al cargar datos: {ex.Message}", "OK");
        }
    }
}