using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;

namespace PassGold.ViewModels;

public partial class AddVehicleViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly CloudinaryService _cloudinaryService = CloudinaryService.Instance;

    [ObservableProperty] private string placa = "";
    [ObservableProperty] private string capacidad = "";

    [ObservableProperty] private string fotoUrl = "";
    [ObservableProperty] private bool hasPhoto = false;

    // spinner mientras sube la foto (distinto de IsLoading, que es para el guardado final)
    [ObservableProperty] private bool isUploadingPhoto = false;

    [ObservableProperty] private bool isLoading = false;

    partial void OnFotoUrlChanged(string value)
    {
        HasPhoto = !string.IsNullOrWhiteSpace(value);
    }

    public static string GetString(string key, string fallback)
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var val))
            return val?.ToString() ?? fallback;
        return fallback;
    }

    [RelayCommand]
    public async Task PickPhotoAsync()
    {
        if (Shell.Current == null) return;

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;

            IsUploadingPhoto = true;

            using var stream = await photo.OpenReadAsync();
            var (urlSubida, error) = await _cloudinaryService.SubirImagenAsync(stream, photo.FileName, "vehiculos");

            if (urlSubida != null)
            {
                FotoUrl = urlSubida; // guardamos la URL de Cloudinary, no la ruta local
            }
            else
            {
                //  DIAGNOSTICO TEMPORAL — borrar este mensaje detallado antes de lanzar
                // la app a producción y volver al genérico de una sola línea.
                await Shell.Current.DisplayAlert(
                    GetString("Global_Error", "Error"),
                    $"No se pudo subir la foto. Revisa tu conexión e intenta de nuevo.\n\nDIAGNÓSTICO: {error}",
                    GetString("Global_Ok", "OK"));
            }
        }
        catch (Exception)
        {
            // Usuario canceló o no dio permisos, ignoramos
        }
        finally
        {
            IsUploadingPhoto = false;
        }
    }

    [RelayCommand]
    public async Task SaveVehicle()
    {
        if (Shell.Current == null) return;

        if (string.IsNullOrWhiteSpace(Placa) || string.IsNullOrWhiteSpace(Capacidad))
        {
            await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), "Por favor llena todos los campos de texto.", GetString("Global_Ok", "OK"));
            return;
        }

        if (!int.TryParse(Capacidad, out int capacidadNum))
        {
            await Shell.Current.DisplayAlert(GetString("Global_Attention", "Atención"), "La capacidad debe ser un número válido.", GetString("Global_Ok", "OK"));
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
            FotoUrl = FotoUrl,
            CreadoEn = DateTime.UtcNow
        };

        bool exito = await _supabaseService.AddVehicleAsync(nuevoVehiculo);

        IsLoading = false;

        if (exito)
        {
            await Shell.Current.DisplayAlert(GetString("Schedule_Success", "¡Éxito!"), "El microbús ha sido guardado correctamente.", GetString("Global_Ok", "OK"));
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(GetString("Global_Error", "Error"), "No se pudo guardar el vehículo. Intenta de nuevo.", GetString("Global_Ok", "OK"));
        }
    }
}