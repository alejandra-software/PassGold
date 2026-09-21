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
using Microsoft.Maui.Networking;
using Microsoft.Maui.Maps;

namespace PassGold.ViewModels;

[QueryProperty(nameof(AsignacionSeleccionada), "AsignacionSeleccionada")]
public partial class PasajeroReservaViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    private OpcionViajePasajero? _opcionSeleccionada;
    public OpcionViajePasajero? AsignacionSeleccionada
    {
        get => _opcionSeleccionada;
        set
        {
            if (SetProperty(ref _opcionSeleccionada, value) && value != null)
            {
                _ = CargarViajeAsync(value);
            }
        }
    }

    [ObservableProperty] private Viaje? viajeActual;
    [ObservableProperty] private bool isLoading = false;

    //  Vista satélite/híbrida
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapType()
    {
        CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;
    }

    // detecta automaticamente si es Ida o Regreso, para saber que
    // pestaña le corresponde a la meta del chofer y para textos correctos.
    [ObservableProperty] private bool esIda = true;

    partial void OnEsIdaChanged(bool value)
    {
        OnPropertyChanged(nameof(TituloRecogida));
        OnPropertyChanged(nameof(TituloBajada));
    }

    // Textos que cambian segun la direccion del viaje
    public string TituloRecogida => EsIda ? AppResources.Map_PromptPickupIda : AppResources.Map_PromptPickupRegreso;
    public string TituloBajada => EsIda ? AppResources.Map_PromptDropoffIda : AppResources.Map_PromptDropoffRegreso;

    [ObservableProperty] private double? latitudRecogida;
    [ObservableProperty] private double? longitudRecogida;
    [ObservableProperty] private string textoRecogida = AppResources.Map_TouchOrigin;

    [ObservableProperty] private double? latitudBajada;
    [ObservableProperty] private double? longitudBajada;
    [ObservableProperty] private string textoBajada = AppResources.Map_TouchDestination;

    [ObservableProperty] private bool seleccionandoOrigen = true;

    public ObservableCollection<Microsoft.Maui.Controls.Maps.Pin> PinesMapa { get; } = new();

    private Microsoft.Maui.Controls.Maps.Pin? _pinOficial;
    private Microsoft.Maui.Controls.Maps.Pin? _pinOrigen;
    private Microsoft.Maui.Controls.Maps.Pin? _pinDestino;

    private async Task CargarViajeAsync(OpcionViajePasajero opcion)
    {
        IsLoading = true;
        ViajeActual = await _supabaseService.GetViajeByIdAsync(opcion.IdViajeBase);
        EsIda = ViajeActual?.TipoViaje?.Contains("Ida", StringComparison.OrdinalIgnoreCase) ?? true;
        RedibujarSegunSubTab();
        IsLoading = false;
    }

    [RelayCommand]
    public void ActivarModoOrigen()
    {
        SeleccionandoOrigen = true;
        RedibujarSegunSubTab();
    }

    [RelayCommand]
    public void ActivarModoDestino()
    {
        SeleccionandoOrigen = false;
        RedibujarSegunSubTab();
    }

    //  solo muestra en el mapa lo que pertenece a la pestaña activa
    // (Recogida o Bajada) — igual que en TripDetailsPage del chofer.
    // La meta oficial del chofer aparece en la pestaña que le corresponda:
    // - IDA: la meta es el punto de LLEGADA -> aparece en "Bajada"
    // - REGRESO: la meta es el punto de SALIDA -> aparece en "Recogida"
    private void RedibujarSegunSubTab()
    {
        PinesMapa.Clear();
        _pinOficial = null;

        bool metaVaEnRecogida = !EsIda;
        bool mostrarMeta = (SeleccionandoOrigen && metaVaEnRecogida) || (!SeleccionandoOrigen && !metaVaEnRecogida);

        if (mostrarMeta && ViajeActual?.MetaLatitud != null && ViajeActual?.MetaLongitud != null)
        {
            _pinOficial = new Microsoft.Maui.Controls.Maps.Pin
            {
                Label = $"🚩 {AppResources.Map_OfficialMeta} {ViajeActual.MetaTexto}",
                Type = Microsoft.Maui.Controls.Maps.PinType.Place,
                Location = new Microsoft.Maui.Devices.Sensors.Location(ViajeActual.MetaLatitud.Value, ViajeActual.MetaLongitud.Value)
            };
            PinesMapa.Add(_pinOficial);
        }

        if (SeleccionandoOrigen && _pinOrigen != null) PinesMapa.Add(_pinOrigen);
        if (!SeleccionandoOrigen && _pinDestino != null) PinesMapa.Add(_pinDestino);
    }

    public void ProcesarToqueMapa(Microsoft.Maui.Devices.Sensors.Location ubicacion)
    {
        var nuevoPin = new Microsoft.Maui.Controls.Maps.Pin
        {
            Label = SeleccionandoOrigen ? $"🟢 {AppResources.Map_PickUpLabel}" : $"🔵 {AppResources.Map_DropOffLabel}",
            Type = Microsoft.Maui.Controls.Maps.PinType.SavedPin,
            Location = ubicacion
        };

        if (SeleccionandoOrigen)
        {
            LatitudRecogida = ubicacion.Latitude;
            LongitudRecogida = ubicacion.Longitude;
            TextoRecogida = AppResources.Map_OriginSet;
            _pinOrigen = nuevoPin;
        }
        else
        {
            LatitudBajada = ubicacion.Latitude;
            LongitudBajada = ubicacion.Longitude;
            TextoBajada = AppResources.Map_DestinationSet;
            _pinDestino = nuevoPin;
        }

        RedibujarSegunSubTab();
    }

    [RelayCommand]
    public async Task ConfirmarReservaDesdeMapaAsync()
    {
        if (AsignacionSeleccionada == null || Shell.Current == null) return;

        if (LatitudRecogida == null || LongitudRecogida == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Map_TouchOrigin, AppResources.Global_Ok);
            return;
        }

        // FIX: antes solo se exigia el pin verde (recogida) — se podia confirmar
        // "con exito" sin haber puesto el pin azul (bajada) para nada.
        if (LatitudBajada == null || LongitudBajada == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Map_TouchDestination, AppResources.Global_Ok);
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Global_NoInternetSearch, AppResources.Global_Ok);
            return;
        }

        IsLoading = true;
        string currentUserId = "";
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id)) currentUserId = authUser.Id;
        else { var sesionLocal = await GetLocalDb().ObtenerSesionActivaAsync(); if (sesionLocal != null) currentUserId = sesionLocal.Id; }

        if (string.IsNullOrEmpty(currentUserId))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_SessionExpired, AppResources.Global_Ok);
            IsLoading = false;
            return;
        }

        var reserva = new Reserva
        {
            Id = Guid.NewGuid().ToString(),
            IdAsignacion = AsignacionSeleccionada.IdAsignacion,
            IdPasajero = currentUserId,
            PuntoRecogidaTexto = TextoRecogida,
            LatitudRecogida = LatitudRecogida,
            LongitudRecogida = LongitudRecogida,
            PuntoBajadaTexto = LatitudBajada != null ? TextoBajada : null,
            LatitudBajada = LatitudBajada,
            LongitudBajada = LongitudBajada,
            Estado = "pendiente",
            CreadoEn = DateTime.UtcNow
        };

        bool ok = await _supabaseService.CreateReservaAsync(reserva);
        IsLoading = false;

        if (ok)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_BookingSuccess, AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_BookingError, AppResources.Global_Ok);
        }
    }
}