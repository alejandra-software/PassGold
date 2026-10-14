using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Services;
using PassGold.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Maps;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace PassGold.ViewModels;

public class PasajeroItem
{
    public string IdPasajero { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public string Telefono { get; set; } = "";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public string? PuntoBajada { get; set; }
    public double? LatitudBajada { get; set; }
    public double? LongitudBajada { get; set; }
}

public class ChoferOpcion
{
    public string IdAsignacion { get; set; } = "";
    public string IdChofer { get; set; } = "";
    public string NombreChofer { get; set; } = "";
    public string PlacaVehiculo { get; set; } = "";
    public int CapacidadVehiculo { get; set; } = 0;
    public string FotoChofer { get; set; } = "";
    public string FotoVehiculo { get; set; } = "";
    public string DisplayName => $"🚐 {NombreChofer} ({PlacaVehiculo})";
}

[QueryProperty(nameof(IdAsignacion), "idAsignacion")]
public partial class TripDetailsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private LocalDatabaseService GetLocalDb() => Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    // Evento para comunicar a la vista que debe centrar el mapa
    public event Action<double, double>? OnCentrarMapa;

    [ObservableProperty] private bool _hayMultiplesChoferes = false;
    [ObservableProperty] private string _nombreChofer = AppResources.Dashboard_Loading;
    [ObservableProperty] private string _placaVehiculo = AppResources.Dashboard_Loading;
    [ObservableProperty] private int _capacidadVehiculo = 0;
    [ObservableProperty] private bool _listaVacia = true;
    [ObservableProperty] private string _fotoChofer = "";
    [ObservableProperty] private string _fotoVehiculo = "";

    [ObservableProperty] private double? metaLatitud;
    [ObservableProperty] private double? metaLongitud;
    [ObservableProperty] private string metaTexto = "";
    [ObservableProperty] private MapType currentMapType = MapType.Street;
    [ObservableProperty] private bool esIda = true;
    [ObservableProperty] private bool hayConexion = true; // NUEVO: Control de red

    [ObservableProperty] private int _tabIndex = 0;
    [ObservableProperty] private bool _isPasajerosTab = true;
    [ObservableProperty] private bool _isMapaTab = false;
    [ObservableProperty] private bool _isOperadorTab = false;
    [ObservableProperty] private bool isMapaSubTabRecogida = true;
    [ObservableProperty] private bool isMapaPantallaCompleta = false;

    [RelayCommand] public void ToggleMapType() => CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    [RelayCommand] public void CambiarSubTabMapa(string tab) => IsMapaSubTabRecogida = tab == "Recogida";

    [RelayCommand]
    public void CambiarPestana(string tab)
    {
        TabIndex = tab == "Pasajeros" ? 0 : tab == "Mapa" ? 1 : 2;
        IsPasajerosTab = TabIndex == 0;
        IsMapaTab = TabIndex == 1;
        IsOperadorTab = TabIndex == 2;
    }

    [RelayCommand] public void SwipeLeft() { if (TabIndex < 2) CambiarPestana(TabIndex == 0 ? "Mapa" : "Operador"); }
    [RelayCommand] public void SwipeRight() { if (TabIndex > 0) CambiarPestana(TabIndex == 2 ? "Mapa" : "Pasajeros"); }

    private string _idAsignacion = "";
    public string IdAsignacion
    {
        get => _idAsignacion;
        set { if (SetProperty(ref _idAsignacion, value) && !string.IsNullOrEmpty(value)) _ = CargarDatosDeFlotaAsync(); }
    }

    private ChoferOpcion? _choferSeleccionado;
    public ChoferOpcion? ChoferSeleccionado
    {
        get => _choferSeleccionado;
        set
        {
            if (SetProperty(ref _choferSeleccionado, value) && value != null)
            {
                NombreChofer = value.NombreChofer; PlacaVehiculo = value.PlacaVehiculo;
                CapacidadVehiculo = value.CapacidadVehiculo; FotoChofer = value.FotoChofer; FotoVehiculo = value.FotoVehiculo;
                _ = CargarPasajerosPorMicrobusAsync(value.IdAsignacion);
            }
        }
    }

    public ObservableCollection<ChoferOpcion> ChoferesAsignados { get; } = new();
    public ObservableCollection<PasajeroItem> Pasajeros { get; } = new();

    private async Task CargarDatosDeFlotaAsync()
    {
        HayConexion = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        var localDb = GetLocalDb();
        string cacheKey = $"TripDetails_Flota_{IdAsignacion}";

        string? cacheFlota = await localDb.LeerCacheAsync(cacheKey);
        if (!string.IsNullOrEmpty(cacheFlota))
        {
            var listaFlota = JsonConvert.DeserializeObject<List<ChoferOpcion>>(cacheFlota);
            if (listaFlota != null && listaFlota.Count > 0) MainThread.BeginInvokeOnMainThread(() => PintarChoferes(listaFlota));
        }

        if (HayConexion)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var asignacionReal = await _supabaseService.GetAsignacionByIdAsync(IdAsignacion);
                    if (asignacionReal == null) return;

                    // 🔧 FIX: antes traía TODOS los viajes activos de TODAS las flotas
                    // solo para buscar este uno con FirstOrDefault. Ahora pide
                    // directamente el único viaje que necesita.
                    var viajeReal = await _supabaseService.GetTripByIdAsync(asignacionReal.IdViaje);

                    var todasAsignacionesMes = await _supabaseService.GetAsignacionesPorRangoAsync(asignacionReal.Fecha.AddDays(-1), asignacionReal.Fecha.AddDays(1));
                    var asignacionesMismoViaje = todasAsignacionesMes.Where(a => a.IdViaje == asignacionReal.IdViaje && a.Fecha.ToLocalTime().Date == asignacionReal.Fecha.ToLocalTime().Date).ToList();

                    var nuevaListaChoferes = new List<ChoferOpcion>();
                    foreach (var asig in asignacionesMismoViaje)
                    {
                        var chInfo = await _supabaseService.GetUserDataAsync(asig.IdChofer);
                        var vInfo = await _supabaseService.GetVehiculoByIdAsync(asig.IdVehiculo);

                        nuevaListaChoferes.Add(new ChoferOpcion
                        {
                            IdAsignacion = asig.Id,
                            IdChofer = asig.IdChofer,
                            NombreChofer = chInfo?.Nombre ?? AppResources.Dashboard_Unknown,
                            PlacaVehiculo = vInfo?.Placa ?? AppResources.TripDetails_NoPlate,
                            CapacidadVehiculo = vInfo != null ? vInfo.Capacidad : 0,
                            FotoChofer = chInfo?.FotoPerfil ?? "",
                            FotoVehiculo = vInfo?.FotoUrl ?? ""
                        });
                    }

                    await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevaListaChoferes));

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MetaLatitud = viajeReal?.MetaLatitud; MetaLongitud = viajeReal?.MetaLongitud;
                        MetaTexto = viajeReal?.MetaTexto ?? "";
                        EsIda = (viajeReal == null || AppResources.IsInboundTrip(viajeReal.TipoViaje));
                        PintarChoferes(nuevaListaChoferes);
                    });
                }
                catch { }
            });
        }
    }

    private void PintarChoferes(List<ChoferOpcion> lista)
    {
        ChoferesAsignados.Clear();
        foreach (var c in lista) ChoferesAsignados.Add(c);
        HayMultiplesChoferes = ChoferesAsignados.Count > 1;

        if (ChoferSeleccionado == null || !ChoferesAsignados.Any(c => c.IdAsignacion == ChoferSeleccionado.IdAsignacion))
            ChoferSeleccionado = ChoferesAsignados.FirstOrDefault(c => c.IdAsignacion == IdAsignacion) ?? ChoferesAsignados.FirstOrDefault();
        else
        {
            var actualizado = ChoferesAsignados.First(c => c.IdAsignacion == ChoferSeleccionado.IdAsignacion);
            NombreChofer = actualizado.NombreChofer; PlacaVehiculo = actualizado.PlacaVehiculo;
            CapacidadVehiculo = actualizado.CapacidadVehiculo; FotoChofer = actualizado.FotoChofer; FotoVehiculo = actualizado.FotoVehiculo;
        }
    }

    private async Task CargarPasajerosPorMicrobusAsync(string idAsignacionEspecifica)
    {
        var localDb = GetLocalDb();
        string cacheKey = $"TripDetails_Pasajeros_{idAsignacionEspecifica}";

        string? cachePasajeros = await localDb.LeerCacheAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachePasajeros))
        {
            var listaPasajeros = JsonConvert.DeserializeObject<List<PasajeroItem>>(cachePasajeros);
            if (listaPasajeros != null) MainThread.BeginInvokeOnMainThread(() => PintarPasajeros(listaPasajeros));
        }

        if (HayConexion)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var reservasReales = await _supabaseService.GetReservasByAsignacionAsync(idAsignacionEspecifica);
                    var nuevaListaPasajeros = new List<PasajeroItem>();

                    foreach (var reserva in reservasReales)
                    {
                        var pasajeroInfo = await _supabaseService.GetUserDataAsync(reserva.IdPasajero);
                        nuevaListaPasajeros.Add(new PasajeroItem
                        {
                            IdPasajero = reserva.IdPasajero,
                            Nombre = pasajeroInfo?.Nombre ?? AppResources.Dashboard_Unknown,
                            FotoUrl = pasajeroInfo?.FotoPerfil ?? "",
                            Telefono = pasajeroInfo?.Telefono ?? "",
                            Ubicacion = reserva.PuntoRecogidaTexto,
                            Latitud = reserva.LatitudRecogida,
                            Longitud = reserva.LongitudRecogida,
                            PuntoBajada = reserva.PuntoBajadaTexto,
                            LatitudBajada = reserva.LatitudBajada,
                            LongitudBajada = reserva.LongitudBajada
                        });
                    }

                    await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevaListaPasajeros));
                    MainThread.BeginInvokeOnMainThread(() => PintarPasajeros(nuevaListaPasajeros));
                }
                catch { }
            });
        }
    }

    private void PintarPasajeros(List<PasajeroItem> lista)
    {
        Pasajeros.Clear();
        foreach (var p in lista) Pasajeros.Add(p);
        ListaVacia = Pasajeros.Count == 0;
    }

    // ==========================================
    // NUEVOS COMANDOS: CONTACTAR Y UBICAR
    // ==========================================

    [RelayCommand]
    public async Task ContactarPasajeroAsync(PasajeroItem pasajero)
    {
        if (pasajero == null) return;

        // Validar si el teléfono está vacío o es muy corto para ser real
        if (string.IsNullOrWhiteSpace(pasajero.Telefono) || pasajero.Telefono.Length < 6)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDetail_NoPhone, AppResources.Global_Ok);
            return;
        }

        string optionWhatsapp = "📱 WhatsApp";
        string optionCall = AppResources.PassengerDetail_Call;

        string action = await Shell.Current.DisplayActionSheet(
            $"{AppResources.PassengerDetail_Call} {pasajero.Nombre}",
            AppResources.Global_Cancel,
            null,
            optionWhatsapp,
            optionCall);

        try
        {
            if (action == optionWhatsapp)
                await Launcher.OpenAsync($"whatsapp://send?phone={pasajero.Telefono}");
            else if (action == optionCall && PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(pasajero.Telefono);
        }
        catch
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDetail_CallError, AppResources.Global_Ok);
        }
    }

    [RelayCommand]
    public void UbicarEnMapa(PasajeroItem pasajero)
    {
        if (pasajero == null) return;

        CambiarPestana("Mapa");

        double? lat = IsMapaSubTabRecogida ? pasajero.Latitud : pasajero.LatitudBajada;
        double? lng = IsMapaSubTabRecogida ? pasajero.Longitud : pasajero.LongitudBajada;

        if (lat.HasValue && lng.HasValue)
        {
            // Dispara el evento para que el Code-Behind centre la cámara
            OnCentrarMapa?.Invoke(lat.Value, lng.Value);
        }
        else
        {
            Shell.Current?.DisplayAlert(AppResources.TripDetails_PointNotSetTitle, AppResources.TripDetails_PointNotSetMsg, AppResources.Global_Ok);
        }
    }
}