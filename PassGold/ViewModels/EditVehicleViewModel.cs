using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;

namespace PassGold.ViewModels;

[QueryProperty(nameof(VehiculoSeleccionado), "VehiculoSeleccionado")]
public partial class EditVehicleViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly CloudinaryService _cloudinaryService = CloudinaryService.Instance;

    [ObservableProperty] private Vehiculo? vehiculoSeleccionado;

    [ObservableProperty] private string placa = "";
    [ObservableProperty] private string capacidad = "";
    [ObservableProperty] private string fotoUrl = "";
    [ObservableProperty] private bool hasPhoto = false;

    // Spinner mientras sube la foto (igual que en AddVehicleViewModel)
    [ObservableProperty] private bool isUploadingPhoto = false;

    [ObservableProperty] private bool isLoading = false;

    partial void OnVehiculoSeleccionadoChanged(Vehiculo? value)
    {
        if (value != null)
        {
            Placa = value.Placa;
            Capacidad = value.Capacidad.ToString();
            FotoUrl = value.FotoUrl ?? "";
            HasPhoto = !string.IsNullOrWhiteSpace(FotoUrl);
        }
    }

    partial void OnFotoUrlChanged(string value)
    {
        HasPhoto = !string.IsNullOrWhiteSpace(value);
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

            //  FIX: antes esto hacía "FotoUrl = photo.FullPath" (ruta local del
            // celular, nunca se subía nada a Cloudinary). Ahora sube el stream igual
            // que AddVehicleViewModel y guarda la URL real de Cloudinary.
            using var stream = await photo.OpenReadAsync();
            var (urlSubida, error) = await _cloudinaryService.SubirImagenAsync(stream, photo.FileName, "vehiculos");

            if (urlSubida != null)
            {
                FotoUrl = urlSubida;
            }
            else
            {
                //  DIAGNOSTICO TEMPORAL — borrar este mensaje detallado antes de lanzar
                // la app a producción y volver al genérico. Por ahora mostramos el error
                // real de Cloudinary/red para poder depurar más rápido.
                await Shell.Current.DisplayAlert(
                    AppResources.Global_Error,
                    $"No se pudo subir la foto.\n\nDIAGNÓSTICO: {error}",
                    AppResources.Global_Ok);
            }
        }
        catch
        {
            // Usuario canceló o no dio permisos, ignoramos
        }
        finally
        {
            IsUploadingPhoto = false;
        }
    }

    [RelayCommand]
    public async Task UpdateVehicleAsync()
    {
        if (Shell.Current == null || VehiculoSeleccionado == null) return;

        if (string.IsNullOrWhiteSpace(Placa) || string.IsNullOrWhiteSpace(Capacidad))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.AlertEmptyFields, AppResources.Global_Ok);
            return;
        }

        if (!int.TryParse(Capacidad, out int capacidadNum))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.AlertInvalidCapacity, AppResources.Global_Ok);
            return;
        }

        IsLoading = true;

        VehiculoSeleccionado.Placa = Placa.Trim().ToUpper();
        VehiculoSeleccionado.Capacidad = capacidadNum;
        VehiculoSeleccionado.FotoUrl = FotoUrl;

        bool exito = await _supabaseService.UpdateVehicleAsync(VehiculoSeleccionado);
        IsLoading = false;

        if (exito)
        {
            await Shell.Current.DisplayAlert(AppResources.Schedule_Success, AppResources.AlertVehicleUpdated, AppResources.Global_Ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.AlertVehicleUpdateError, AppResources.Global_Ok);
        }
    }
}