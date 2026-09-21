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
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Maps;

namespace PassGold.ViewModels;

public partial class ConfirmarReservaViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly List<OpcionViajePasajero> _seleccionesIniciales;

    private LocalDatabaseService GetLocalDb() =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    public bool EsIda { get; }
    public string TituloExtremoFijo => AppResources.PassengerDash_HomeLabel;
    public string TituloExtremoAlterno => EsIda ? AppResources.PassengerDash_WhereToDropPrompt : AppResources.PassengerDash_WherePickupPrompt;

    [ObservableProperty] private bool isLoading = true;
    [ObservableProperty] private bool isSaving = false;

    //  Vista satélite/híbrida — se me había olvidado traerla a esta pantalla nueva.
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType()
    {
        CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }

    //  Pestañas Recogida/Bajada — igual que en el mapa del chofer (TripDetailsPage),
    // para no mostrar los 2 pines mezclados y que quede claro cuál es cuál.
    [ObservableProperty] private bool viendoRecogida = true;

    [RelayCommand]
    public void VerRecogida()
    {
        ViendoRecogida = true;
        SincronizarPinActivo();
        RedibujarPines();
    }

    [RelayCommand]
    public void VerBajada()
    {
        ViendoRecogida = false;
        SincronizarPinActivo();
        RedibujarPines();
    }

    // En Ida: Recogida = Casa (fija), Bajada = Alterno. En Regreso: al revés.
    // TocandoCasa decide qué pin recibe el próximo toque del mapa.
    private void SincronizarPinActivo()
    {
        TocandoCasa = (ViendoRecogida == EsIda);
    }

    // Info del chofer/vehiculo
    [ObservableProperty] private string nombreChofer = "";
    [ObservableProperty] private string fotoChofer = "";
    [ObservableProperty] private string placaVehiculo = "";
    [ObservableProperty] private string fotoVehiculo = "";
    [ObservableProperty] private string telefonoChofer = "";
    [ObservableProperty] private bool hayTelefonoChofer = false;

    // Pin de casa
    [ObservableProperty] private double? latCasa;
    [ObservableProperty] private double? lngCasa;
    [ObservableProperty] private bool casaEsNueva = false;

    // Pin alterno
    [ObservableProperty] private string modoAlterno = "MetaOficial";
    [ObservableProperty] private double? latAlterno;
    [ObservableProperty] private double? lngAlterno;
    [ObservableProperty] private string textoAlterno = "";
    [ObservableProperty] private string nuevoAliasAlterno = "";
    [ObservableProperty] private bool guardarNuevoAlternoEnHistorial = true;

    [ObservableProperty] private bool tocandoCasa = false;

    [ObservableProperty] private bool hayMetaOficialDisponible = false;
    [ObservableProperty] private string textoBotonMetaOficial = "";
    private double? _metaOficialLat, _metaOficialLng;

    public ObservableCollection<UbicacionPasajero> MisUbicaciones { get; } = new();
    public ObservableCollection<ViajeParada> ParadasOficiales { get; } = new();
    public bool HayVariasParadasOficiales => ParadasOficiales.Count > 1;

    public ObservableCollection<Pin> PinesMapa { get; } = new();

    [ObservableProperty] private string alcanceSeleccionado = "";
    public ObservableCollection<string> OpcionesAlcance { get; } = new();

    // Rango de fecha libre: en vez de solo 4 botones fijos, se puede elegir
    // hasta qué fecha exacta repetir la reserva — acotado a lo que realmente
    // dure el ciclo del viaje (fecha_fin del Viaje), para no poder "reservar"
    // en un mes donde ese viaje ya no existe.
    [ObservableProperty] private bool usandoRangoPersonalizado = false;
    [ObservableProperty] private DateTime fechaHastaPersonalizada = DateTime.Today.AddDays(14);
    [ObservableProperty] private DateTime fechaMinimaRango = DateTime.Today;
    [ObservableProperty] private DateTime fechaMaximaRango = DateTime.Today.AddMonths(6);

    public ConfirmarReservaViewModel(List<OpcionViajePasajero> seleccionesIniciales)
    {
        _seleccionesIniciales = seleccionesIniciales;
        EsIda = seleccionesIniciales.FirstOrDefault()?.EsIda ?? true;

        int diasDistintos = seleccionesIniciales.Select(o => o.FechaAsignacion.DayOfWeek).Distinct().Count();
        if (diasDistintos == 1) OpcionesAlcance.Add(AppResources.PassengerDash_ScopeOnlyThisDay);
        OpcionesAlcance.Add(AppResources.PassengerDash_ScopeThisWeek);
        OpcionesAlcance.Add(AppResources.PassengerDash_ScopeThisMonth);
        OpcionesAlcance.Add(AppResources.PassengerDash_ScopeRestOfCycle);
        OpcionesAlcance.Add(AppResources.PassengerDash_ScopeCustomRange);
        AlcanceSeleccionado = OpcionesAlcance.First();

        _ = CargarDatosAsync();
    }

    [RelayCommand]
    public void SeleccionarAlcance(string opcion)
    {
        if (string.IsNullOrEmpty(opcion)) return;
        AlcanceSeleccionado = opcion;
        UsandoRangoPersonalizado = opcion == AppResources.PassengerDash_ScopeCustomRange;
    }

    private async Task CargarDatosAsync()
    {
        IsLoading = true;

        string currentUserId = "";
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var misUb = await _supabaseService.GetUbicacionesPasajeroAsync(currentUserId);
            MisUbicaciones.Clear();
            foreach (var u in misUb.OrderByDescending(x => x.CreadoEn)) MisUbicaciones.Add(u);

            var casaGuardada = MisUbicaciones.FirstOrDefault(u => u.Alias == AppResources.PassengerDash_HomeLabel);
            if (casaGuardada != null)
            {
                LatCasa = casaGuardada.Latitud;
                LngCasa = casaGuardada.Longitud;
                CasaEsNueva = false;
            }
            else
            {
                CasaEsNueva = true;
                ViendoRecogida = EsIda; // la pestaña donde vive la casa, según la dirección
                TocandoCasa = true;
            }
        }

        var primeraOpcion = _seleccionesIniciales.First();

        var asignacionInfo = await _supabaseService.GetAsignacionByIdAsync(primeraOpcion.IdAsignacion);
        if (asignacionInfo != null)
        {
            var chofer = await _supabaseService.GetUserDataAsync(asignacionInfo.IdChofer);
            if (chofer != null)
            {
                NombreChofer = chofer.Nombre;
                FotoChofer = chofer.FotoPerfil ?? "";
                TelefonoChofer = chofer.Telefono ?? "";
                HayTelefonoChofer = !string.IsNullOrWhiteSpace(TelefonoChofer);
            }

            var vehiculo = await _supabaseService.GetVehiculoByIdAsync(asignacionInfo.IdVehiculo);
            if (vehiculo != null)
            {
                PlacaVehiculo = vehiculo.Placa;
                FotoVehiculo = vehiculo.FotoUrl ?? "";
            }
        }

        var paradas = await _supabaseService.GetParadasByViajeAsync(primeraOpcion.IdViajeBase);
        ParadasOficiales.Clear();
        foreach (var p in paradas) ParadasOficiales.Add(p);
        OnPropertyChanged(nameof(HayVariasParadasOficiales));

        //  Siempre se trae el Viaje: además de servir de respaldo para la meta
        // oficial (si no hay paradas guardadas), da los límites reales del ciclo
        // (fecha_inicio/fecha_fin) para acotar el selector de rango personalizado.
        var viajeBase = await _supabaseService.GetViajeByIdAsync(primeraOpcion.IdViajeBase);
        if (viajeBase != null)
        {
            var hoy = DateTime.Today;
            FechaMinimaRango = primeraOpcion.FechaAsignacion.Date > hoy ? primeraOpcion.FechaAsignacion.Date : hoy;
            FechaMaximaRango = viajeBase.FechaFin ?? hoy.AddMonths(6);
            if (FechaMaximaRango < FechaMinimaRango) FechaMaximaRango = FechaMinimaRango;

            FechaHastaPersonalizada = FechaMinimaRango.AddDays(14) < FechaMaximaRango ? FechaMinimaRango.AddDays(14) : FechaMaximaRango;
        }

        if (ParadasOficiales.Count == 1)
        {
            _metaOficialLat = ParadasOficiales[0].Latitud;
            _metaOficialLng = ParadasOficiales[0].Longitud;
            TextoBotonMetaOficial = $"🏁 {ParadasOficiales[0].NombreLugar}";
            HayMetaOficialDisponible = _metaOficialLat != null;
        }
        else if (ParadasOficiales.Count == 0 && viajeBase?.MetaLatitud != null && viajeBase.MetaLongitud != null)
        {
            _metaOficialLat = viajeBase.MetaLatitud;
            _metaOficialLng = viajeBase.MetaLongitud;
            TextoBotonMetaOficial = string.IsNullOrWhiteSpace(viajeBase.MetaTexto) ? AppResources.PassengerDash_OfficialMetaOption : $"🏁 {viajeBase.MetaTexto}";
            HayMetaOficialDisponible = true;
        }

        if (HayMetaOficialDisponible && ParadasOficiales.Count <= 1)
        {
            SeleccionarMetaOficial();
        }

        IsLoading = false;
        RedibujarPines();
    }

    [RelayCommand]
    public void SeleccionarMetaOficial()
    {
        if (!HayMetaOficialDisponible) return;
        ModoAlterno = "MetaOficial";
        LatAlterno = _metaOficialLat;
        LngAlterno = _metaOficialLng;
        TextoAlterno = TextoBotonMetaOficial;
        ViendoRecogida = !EsIda;
        RedibujarPines();
    }

    [RelayCommand]
    public void SeleccionarParadaOficial(ViajeParada parada)
    {
        if (parada == null) return;
        ModoAlterno = "MetaOficial";
        LatAlterno = parada.Latitud;
        LngAlterno = parada.Longitud;
        TextoAlterno = $"🏁 {parada.NombreLugar}";
        ViendoRecogida = !EsIda;
        RedibujarPines();
    }

    [RelayCommand]
    public void SeleccionarUbicacionGuardada(UbicacionPasajero ubicacion)
    {
        if (ubicacion == null) return;
        ModoAlterno = "Guardada";
        LatAlterno = ubicacion.Latitud;
        LngAlterno = ubicacion.Longitud;
        TextoAlterno = $"⭐ {ubicacion.DireccionTexto}";
        ViendoRecogida = !EsIda;
        RedibujarPines();
    }

    [RelayCommand]
    public void ActivarModoMapaParaAlterno()
    {
        ModoAlterno = "Mapa";
        TocandoCasa = false;
        ViendoRecogida = !EsIda; // la pestaña donde vive el punto alterno
        RedibujarPines();
    }

    [RelayCommand]
    public void ActivarEdicionCasa()
    {
        TocandoCasa = true;
        ViendoRecogida = EsIda; // la pestaña donde vive la casa
        RedibujarPines();
    }

    // FIX PRINCIPAL: antes esto guardaba el texto del BOTON como si fuera la
    // direccion real. Ahora hace geocodificacion INVERSA (gratis, sin API key,
    // mismo Geocoding.Default del resto de la app) para convertir el punto
    // tocado en una direccion legible de verdad.
    public async void ProcesarToqueMapa(Location ubicacion)
    {
        if (TocandoCasa)
        {
            LatCasa = ubicacion.Latitude;
            LngCasa = ubicacion.Longitude;
            CasaEsNueva = true;
            RedibujarPines();
        }
        else
        {
            ModoAlterno = "Mapa";
            LatAlterno = ubicacion.Latitude;
            LngAlterno = ubicacion.Longitude;
            TextoAlterno = AppResources.ConfirmarReserva_Ubicando;
            RedibujarPines();

            TextoAlterno = await ObtenerDireccionLegibleAsync(ubicacion);
            RedibujarPines();
        }
    }

    private async Task<string> ObtenerDireccionLegibleAsync(Location ubicacion)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(ubicacion);
            var lugar = placemarks?.FirstOrDefault();
            if (lugar != null)
            {
                var partes = new List<string>();
                if (!string.IsNullOrWhiteSpace(lugar.Thoroughfare)) partes.Add(lugar.Thoroughfare);
                if (!string.IsNullOrWhiteSpace(lugar.SubLocality)) partes.Add(lugar.SubLocality);
                if (!string.IsNullOrWhiteSpace(lugar.Locality)) partes.Add(lugar.Locality);
                if (partes.Count > 0) return $"📍 {string.Join(", ", partes)}";
            }
        }
        catch { }
        return AppResources.ConfirmarReserva_PinSinDireccion;
    }

    private void RedibujarPines()
    {
        PinesMapa.Clear();

        // Solo se muestra en el mapa lo que pertenece a la pestaña activa.
        bool mostrarCasa = (ViendoRecogida == EsIda);
        bool mostrarAlterno = !mostrarCasa;

        if (mostrarCasa && LatCasa.HasValue && LngCasa.HasValue)
        {
            PinesMapa.Add(new Pin { Label = AppResources.PassengerDash_HomeLabel, Type = PinType.SavedPin, Location = new Location(LatCasa.Value, LngCasa.Value) });
        }

        if (mostrarAlterno)
        {
            if (HayVariasParadasOficiales)
            {
                foreach (var parada in ParadasOficiales)
                {
                    if (parada.Latitud == null || parada.Longitud == null) continue;
                    bool esLaElegida = ModoAlterno == "MetaOficial" && LatAlterno == parada.Latitud && LngAlterno == parada.Longitud;
                    PinesMapa.Add(new Pin { Label = esLaElegida ? $"✅ {parada.NombreLugar}" : $"🏁 {parada.NombreLugar}", Type = PinType.Place, Location = new Location(parada.Latitud.Value, parada.Longitud.Value) });
                }
            }
            else if (LatAlterno.HasValue && LngAlterno.HasValue)
            {
                PinesMapa.Add(new Pin { Label = TextoAlterno, Type = PinType.Place, Location = new Location(LatAlterno.Value, LngAlterno.Value) });
            }
        }
    }

    [RelayCommand]
    public async Task ConfirmarAsync()
    {
        if (Shell.Current == null) return;

        if (LatCasa == null || LngCasa == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_NeedHomeFirst, AppResources.Global_Ok);
            return;
        }
        if (LatAlterno == null || LngAlterno == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, EsIda ? AppResources.Map_TouchDestination : AppResources.Map_TouchOrigin, AppResources.Global_Ok);
            return;
        }
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_NeedInternetToBook, AppResources.Global_Ok);
            return;
        }

        IsSaving = true;

        string currentUserId = "";
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (string.IsNullOrEmpty(currentUserId))
        {
            IsSaving = false;
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_SessionExpired, AppResources.Global_Ok);
            return;
        }

        if (CasaEsNueva)
        {
            var casa = new UbicacionPasajero { Id = Guid.NewGuid().ToString(), IdPasajero = currentUserId, Alias = AppResources.PassengerDash_HomeLabel, DireccionTexto = AppResources.PassengerDash_HomeLabel, Latitud = LatCasa, Longitud = LngCasa };
            await _supabaseService.CreateUbicacionAsync(casa);
        }

        //  FIX: antes, si el usuario dejaba la casilla marcada pero no escribía
        // un alias, el guardado se saltaba EN SILENCIO — por eso parecía que el
        // historial "nunca guardaba nada". Ahora, si no hay alias, se usa la
        // dirección geocodificada como nombre por defecto (siempre que la casilla
        // esté marcada).
        if (ModoAlterno == "Mapa" && GuardarNuevoAlternoEnHistorial)
        {
            string aliasFinal = !string.IsNullOrWhiteSpace(NuevoAliasAlterno) ? NuevoAliasAlterno.Trim() : TextoAlterno;
            var nuevaUb = new UbicacionPasajero { Id = Guid.NewGuid().ToString(), IdPasajero = currentUserId, Alias = aliasFinal, DireccionTexto = TextoAlterno, Latitud = LatAlterno, Longitud = LngAlterno };
            await _supabaseService.CreateUbicacionAsync(nuevaUb);
        }

        var asignacionesAReservar = new List<OpcionViajePasajero>();
        var idsYaAgregados = new HashSet<string>();

        foreach (var opcionBase in _seleccionesIniciales)
        {
            if (idsYaAgregados.Add(opcionBase.IdAsignacion)) asignacionesAReservar.Add(opcionBase);

            if (AlcanceSeleccionado == AppResources.PassengerDash_ScopeOnlyThisDay) continue;

            var fechaBase = opcionBase.FechaAsignacion.Date;
            DateTime fechaLimite = AlcanceSeleccionado switch
            {
                _ when AlcanceSeleccionado == AppResources.PassengerDash_ScopeThisWeek => fechaBase.AddDays(7 - (int)fechaBase.DayOfWeek),
                _ when AlcanceSeleccionado == AppResources.PassengerDash_ScopeThisMonth => fechaBase.AddDays(30),
                _ when AlcanceSeleccionado == AppResources.PassengerDash_ScopeRestOfCycle => fechaBase.AddMonths(6),
                _ when AlcanceSeleccionado == AppResources.PassengerDash_ScopeCustomRange => FechaHastaPersonalizada,
                _ => fechaBase
            };
            //  Nunca reservar más allá del fin real del ciclo del viaje, sin importar
            // qué botón se haya elegido — evita "reservar" fechas donde ese viaje ya no existe.
            if (fechaLimite > FechaMaximaRango) fechaLimite = FechaMaximaRango;

            var asignacionesFuturas = await _supabaseService.GetAsignacionesPorRangoAsync(fechaBase.AddDays(1), fechaLimite, new List<string> { opcionBase.IdChofer });
            foreach (var a in asignacionesFuturas.Where(a => a.IdViaje == opcionBase.IdViajeBase))
            {
                if (!idsYaAgregados.Add(a.Id)) continue;
                asignacionesAReservar.Add(new OpcionViajePasajero { IdAsignacion = a.Id, IdViajeBase = a.IdViaje, IdChofer = a.IdChofer, FechaAsignacion = a.Fecha.ToLocalTime(), EsIda = opcionBase.EsIda });
            }
        }

        int exitosas = 0;
        foreach (var asig in asignacionesAReservar)
        {
            var reserva = new Reserva { Id = Guid.NewGuid().ToString(), IdAsignacion = asig.IdAsignacion, IdPasajero = currentUserId, Estado = "pendiente", CreadoEn = DateTime.UtcNow };

            if (asig.EsIda)
            {
                reserva.PuntoRecogidaTexto = AppResources.PassengerDash_HomeLabel;
                reserva.LatitudRecogida = LatCasa; reserva.LongitudRecogida = LngCasa;
                reserva.PuntoBajadaTexto = TextoAlterno;
                reserva.LatitudBajada = LatAlterno; reserva.LongitudBajada = LngAlterno;
            }
            else
            {
                reserva.PuntoRecogidaTexto = TextoAlterno;
                reserva.LatitudRecogida = LatAlterno; reserva.LongitudRecogida = LngAlterno;
                reserva.PuntoBajadaTexto = AppResources.PassengerDash_HomeLabel;
                reserva.LatitudBajada = LatCasa; reserva.LongitudBajada = LngCasa;
            }

            if (await _supabaseService.CreateReservaAsync(reserva)) exitosas++;
        }

        IsSaving = false;

        if (exitosas > 0)
        {
            string msg = asignacionesAReservar.Count == 1 ? AppResources.PassengerDash_BookingSuccess : string.Format(AppResources.PassengerDash_BulkBookingSuccess, exitosas, asignacionesAReservar.Count);
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, msg, AppResources.Global_Ok);
            await Shell.Current.Navigation.PopAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_BookingError, AppResources.Global_Ok);
        }
    }
}