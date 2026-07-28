using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GoldeenRide.ViewModels;

public partial class AddVehicleViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private string placa = "";
    [ObservableProperty] private string capacidad = "";
    [ObservableProperty] private bool isLoading = false;

    private string GetTranslation(string key)
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var value))
            return value?.ToString() ?? key;
        return key;
    }

    [RelayCommand]
    public async Task SaveVehicle()
    {
        if (string.IsNullOrWhiteSpace(Placa) || string.IsNullOrWhiteSpace(Capacidad))
        {
            // ✅ Actualizado a DisplayAlertAsync
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(GetTranslation("AlertAttention"), GetTranslation("AlertEmptyFields"), GetTranslation("AlertOk"));
            return;
        }

        if (!int.TryParse(Capacidad, out int capacidadNum))
        {
            // ✅ Actualizado a DisplayAlertAsync
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(GetTranslation("AlertAttention"), GetTranslation("AlertInvalidCapacity"), GetTranslation("AlertOk"));
            return;
        }

        IsLoading = true;

        var currentUser = _supabaseService.GetCurrentUser();
        if (currentUser == null || string.IsNullOrEmpty(currentUser.Id))
        {
            IsLoading = false;
            return;
        }

        var nuevoVehiculo = new Vehiculo
        {
            IdPropietario = currentUser.Id,
            Placa = Placa.Trim().ToUpper(),
            Capacidad = capacidadNum,
            Estado = "activo",
            CreadoEn = DateTime.UtcNow
        };

        bool exito = await _supabaseService.AddVehicleAsync(nuevoVehiculo);

        IsLoading = false;

        if (exito)
        {
            if (Shell.Current != null)
            {
                // ✅ Actualizado a DisplayAlertAsync
                await Shell.Current.DisplayAlertAsync(GetTranslation("AlertSuccess"), GetTranslation("AlertVehicleSaved"), GetTranslation("AlertOk"));
                await Shell.Current.GoToAsync("..");
            }
        }
        else
        {
            // ✅ Actualizado a DisplayAlertAsync
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(GetTranslation("AlertError"), GetTranslation("AlertVehicleSaveError"), GetTranslation("AlertOk"));
        }
    }
}