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

namespace PassGold.ViewModels;

public class PasajeroItem
{
    public string IdPasajero { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    // Para la ficha de detalle — el chofer necesita poder llamar al pasajero
    // cuando pierde la hora (pasa casi todos los días, según reporta la usuaria).
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
    // Nunca se traían — la pestaña "Operador" mostraba un 👤/🚐 fijo porque
    // no había de dónde sacar la foto real. Se llenan en CargarDatosDeFlotaAsync.
    public string FotoChofer { get; set; } = "";
    public string FotoVehiculo { get; set; } = "";
    public string DisplayName => $"🚐 {NombreChofer} ({PlacaVehiculo})";
}

[QueryProperty(nameof(IdAsignacion), "idAsignacion")]
public partial class TripDetailsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private bool _hayMultiplesChoferes = false;
    [ObservableProperty] private string _nombreChofer = AppResources.Dashboard_Loading;
    [ObservableProperty] private string _placaVehiculo = AppResources.Dashboard_Loading;
    [ObservableProperty] private int _capacidadVehiculo = 0;
    [ObservableProperty] private bool _listaVacia = true;
    //  Fotos del chofer/vehículo para la pestaña "Operador" — antes no existían.
    [ObservableProperty] private string _fotoChofer = "";
    [ObservableProperty] private string _fotoVehiculo = "";

    [ObservableProperty] private double? metaLatitud;
    [ObservableProperty] private double? metaLongitud;
    [ObservableProperty] private string metaTexto = "";

    //  Vista satélite/híbrida — se usa en el mapa chico y en el de pantalla
    // completa, comparten el mismo estado para que no se desincronicen.
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType()
    {
        CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }

    //  NUEVO: saber si es viaje de Ida o Regreso, para saber a que pestaña
    // del mapa pertenece la meta del chofer.
    [ObservableProperty] private bool esIda = true;

    [ObservableProperty] private int _tabIndex = 0;
    [ObservableProperty] private bool _isPasajerosTab = true;
    [ObservableProperty] private bool _isMapaTab = false;
    [ObservableProperty] private bool _isOperadorTab = false;

    //  NUEVO: sub-pestañas dentro de "Mapa" — Recogida vs Bajada, en vez de
    // un solo mapa con todos los pines revueltos.
    [ObservableProperty] private bool isMapaSubTabRecogida = true;

    //  NUEVO: pantalla completa del mapa
    [ObservableProperty] private bool isMapaPantallaCompleta = false;

    [RelayCommand]
    public void CambiarSubTabMapa(string tab)
    {
        IsMapaSubTabRecogida = tab == "Recogida";
    }

    private string _idAsignacion = "";
    public string IdAsignacion
    {
        get => _idAsignacion;
        set
        {
            if (SetProperty(ref _idAsignacion, value) && !string.IsNullOrEmpty(value))
            {
                _ = CargarDatosDeFlotaAsync();
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
                CapacidadVehiculo = value.CapacidadVehiculo;
                FotoChofer = value.FotoChofer;
                FotoVehiculo = value.FotoVehiculo;

                _ = CargarPasajerosPorMicrobusAsync(value.IdAsignacion);
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
    public void SwipeLeft() { if (TabIndex < 2) SetTab(TabIndex + 1); }

    [RelayCommand]
    public void SwipeRight() { if (TabIndex > 0) SetTab(TabIndex - 1); }

    private void SetTab(int index)
    {
        TabIndex = index;
        IsPasajerosTab = index == 0;
        IsMapaTab = index == 1;
        IsOperadorTab = index == 2;
    }

    private async Task CargarDatosDeFlotaAsync()
    {
        var localDb = GetLocalDb();
        string cacheKey = $"TripDetails_Flota_{IdAsignacion}";

        string? cacheFlota = await localDb.LeerCacheAsync(cacheKey);
        if (!string.IsNullOrEmpty(cacheFlota))
        {
            var listaFlota = JsonConvert.DeserializeObject<List<ChoferOpcion>>(cacheFlota);
            if (listaFlota != null && listaFlota.Count > 0)
            {
                PintarChoferes(listaFlota);
            }
        }

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var asignacionReal = await _supabaseService.GetAsignacionByIdAsync(IdAsignacion);
                    if (asignacionReal == null) return;

                    var viajesActivos = await _supabaseService.GetAllActiveTripsAsync();
                    var viajeReal = viajesActivos.FirstOrDefault(v => v.Id == asignacionReal.IdViaje);

                    var todasAsignacionesMes = await _supabaseService.GetAsignacionesPorRangoAsync(asignacionReal.Fecha.AddDays(-1), asignacionReal.Fecha.AddDays(1));
                    var asignacionesMismoViaje = todasAsignacionesMes
                        .Where(a => a.IdViaje == asignacionReal.IdViaje && a.Fecha.ToLocalTime().Date == asignacionReal.Fecha.ToLocalTime().Date)
                        .ToList();

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

                    Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        MetaLatitud = viajeReal?.MetaLatitud;
                        MetaLongitud = viajeReal?.MetaLongitud;
                        MetaTexto = viajeReal?.MetaTexto ?? "";
                        EsIda = viajeReal?.TipoViaje?.Contains("Ida", StringComparison.OrdinalIgnoreCase) ?? true;
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
        {
            ChoferSeleccionado = ChoferesAsignados.FirstOrDefault(c => c.IdAsignacion == IdAsignacion) ?? ChoferesAsignados.FirstOrDefault();
        }
        else
        {
            //  FIX: sigue siendo el mismo chofer (mismo Id), pero la instancia es
            // nueva — puede venir de la red después de mostrar primero la caché, y
            // traer datos que la caché vieja no tenía (ej. FotoChofer/FotoVehiculo,
            // que no existían antes de este cambio). Antes esto se ignoraba del
            // todo, y la pantalla se quedaba con los valores viejos hasta cerrar y
            // reabrir la app. No reasignamos ChoferSeleccionado en sí (para no
            // disparar de nuevo la carga de pasajeros, que sí sigue siendo cara),
            // solo refrescamos los campos que se muestran en pantalla.
            var actualizado = ChoferesAsignados.First(c => c.IdAsignacion == ChoferSeleccionado.IdAsignacion);
            NombreChofer = actualizado.NombreChofer;
            PlacaVehiculo = actualizado.PlacaVehiculo;
            CapacidadVehiculo = actualizado.CapacidadVehiculo;
            FotoChofer = actualizado.FotoChofer;
            FotoVehiculo = actualizado.FotoVehiculo;
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
            if (listaPasajeros != null)
            {
                PintarPasajeros(listaPasajeros);
            }
        }

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
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
                            //  FIX: pasajeroInfo YA traía la foto (FotoPerfil), pero nunca se
                            // copiaba a PasajeroItem.FotoUrl — por eso esta lista mostraba el
                            // mismo 🎓 fijo para todos, con foto o sin foto.
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

                    Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        PintarPasajeros(nuevaListaPasajeros);
                    });
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
}