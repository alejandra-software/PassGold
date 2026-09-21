using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;

namespace PassGold.ViewModels;

public partial class SetHomeLocationViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private double? latitudSeleccionada;
    [ObservableProperty] private double? longitudSeleccionada;
    [ObservableProperty] private bool hayPinPuesto = false;
    [ObservableProperty] private bool isSaving = false;

    public ObservableCollection<Pin> PinesMapa { get; } = new();

    public void ProcesarToqueMapa(Location ubicacion)
    {
        LatitudSeleccionada = ubicacion.Latitude;
        LongitudSeleccionada = ubicacion.Longitude;
        HayPinPuesto = true;

        PinesMapa.Clear();
        PinesMapa.Add(new Pin
        {
            Label = AppResources.PassengerDash_HomeLabel,
            Type = PinType.SavedPin,
            Location = ubicacion
        });
    }

    [RelayCommand]
    public async Task GuardarCasaAsync()
    {
        if (Shell.Current == null) return;

        if (LatitudSeleccionada == null || LongitudSeleccionada == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SetHome_NoPinError, AppResources.Global_Ok);
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

        // El Alias exacto "🏠 Mi Casa" (AppResources.PassengerDash_HomeLabel) es lo
        // que CrearReservasBulkAsync busca para saber si ya existe — tiene que
        // coincidir carácter por carácter.
        var casa = new UbicacionPasajero
        {
            Id = Guid.NewGuid().ToString(),
            IdPasajero = currentUserId,
            Alias = AppResources.PassengerDash_HomeLabel,
            DireccionTexto = AppResources.PassengerDash_HomeLabel,
            Latitud = LatitudSeleccionada,
            Longitud = LongitudSeleccionada
        };

        bool ok = await _supabaseService.CreateUbicacionAsync(casa);
        IsSaving = false;

        if (ok)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SetHome_SavedSuccess, AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_BookingError, AppResources.Global_Ok);
        }
    }
}