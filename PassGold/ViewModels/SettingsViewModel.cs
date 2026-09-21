using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;
using System;
using PassGold.Helpers;

namespace PassGold.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly CloudinaryService _cloudinaryService = CloudinaryService.Instance;

    [ObservableProperty] private string userName = "Cargando...";
    [ObservableProperty] private string userRole = "";
    [ObservableProperty] private string userPhotoUrl = "";

    //  FIX: en vez de depender de StringToBoolConverter/InvertedStringToBoolConverter
    // en el XAML (que estaba mostrando la foto Y el ícono de silueta al mismo tiempo,
    // superpuestos), exponemos bools explícitos calculados acá — el mismo patrón
    // probado que ya usa AddVehicleViewModel.HasPhoto.
    [ObservableProperty] private bool hasUserPhoto = false;

    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private bool hasBoss = false;

    [ObservableProperty] private string myFleetCode = "Cargando...";
    [ObservableProperty] private string inputFleetCode = "";
    [ObservableProperty] private string currentBossName = "";

    [ObservableProperty] private bool isLoading = false;

    //  Edición de perfil — faltaba TODO este bloque, el XAML ya lo pedía pero
    // el ViewModel nunca tuvo estas propiedades ni comandos.
    [ObservableProperty] private bool isEditingProfile = false;
    [ObservableProperty] private string editableFotoUrl = "";
    [ObservableProperty] private bool hasEditableFoto = false;
    [ObservableProperty] private string editableNombre = "";
    [ObservableProperty] private string editableTelefono = "";
    [ObservableProperty] private bool isUploadingPhoto = false;
    [ObservableProperty] private bool isSavingProfile = false;

    partial void OnUserPhotoUrlChanged(string value)
    {
        HasUserPhoto = !string.IsNullOrWhiteSpace(value);
    }

    partial void OnEditableFotoUrlChanged(string value)
    {
        HasEditableFoto = !string.IsNullOrWhiteSpace(value);
    }

    private Usuario? _currentUserData;

    public SettingsViewModel()
    {
        _ = LoadProfileAsync();
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        IsLoading = true;
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
        {
            _currentUserData = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (_currentUserData != null)
            {
                UserName = _currentUserData.Nombre;
                UserRole = _currentUserData.Rol;
                UserPhotoUrl = _currentUserData.FotoPerfil ?? "";

                string rolLower = UserRole.ToLower();
                IsFleetManager = rolLower.Contains("jefe") || rolLower.Contains("owner") || rolLower.Contains("independiente");
                IsEmployee = rolLower.Contains("empleado");

                if (IsFleetManager)
                {
                    MyFleetCode = await _supabaseService.GetOrCreateFleetCodeAsync(_currentUserData.Id) ?? "Error generando código";
                }

                if (IsEmployee && !string.IsNullOrEmpty(_currentUserData.IdJefe))
                {
                    HasBoss = true;
                    var jefe = await _supabaseService.GetUserDataAsync(_currentUserData.IdJefe);
                    CurrentBossName = jefe?.Nombre ?? "Jefe Desconocido";
                }
            }
        }
        IsLoading = false;
    }

    [RelayCommand]
    public void ToggleEditProfile()
    {
        if (!IsEditingProfile && _currentUserData != null)
        {
            // Al entrar en modo edición, precargamos los campos editables con los
            // datos actuales — si el usuario cancela, no se pierde ni se pisa nada.
            EditableNombre = _currentUserData.Nombre ?? "";
            EditableTelefono = _currentUserData.Telefono ?? "";
            EditableFotoUrl = _currentUserData.FotoPerfil ?? "";
        }

        IsEditingProfile = !IsEditingProfile;
    }

    [RelayCommand]
    public async Task PickProfilePhotoAsync()
    {
        if (Shell.Current == null) return;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SettingsProfile_NeedInternet, AppResources.Global_Ok);
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;

            IsUploadingPhoto = true;

            using var stream = await photo.OpenReadAsync();
            var (urlSubida, error) = await _cloudinaryService.SubirImagenAsync(stream, photo.FileName, "perfiles");

            if (urlSubida != null)
            {
                EditableFotoUrl = urlSubida;
            }
            else
            {
                //  DIAGNOSTICO TEMPORAL — borrar este mensaje detallado antes de lanzar
                // la app a producción y volver a AppResources.SettingsProfile_UploadError.
                await Shell.Current.DisplayAlert(AppResources.Global_Error, $"{AppResources.SettingsProfile_UploadError}\n\nDIAGNÓSTICO: {error}", AppResources.Global_Ok);
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
    public async Task SaveProfileAsync()
    {
        if (Shell.Current == null || _currentUserData == null) return;

        if (string.IsNullOrWhiteSpace(EditableNombre))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SettingsProfile_NameEmptyError, AppResources.Global_Ok);
            return;
        }

        IsSavingProfile = true;

        _currentUserData.Nombre = EditableNombre.Trim();
        _currentUserData.Telefono = EditableTelefono?.Trim() ?? "";
        _currentUserData.FotoPerfil = EditableFotoUrl;

        bool exito = await _supabaseService.UpdateUserDataAsync(_currentUserData);
        IsSavingProfile = false;

        if (exito)
        {
            UserName = _currentUserData.Nombre;
            UserPhotoUrl = _currentUserData.FotoPerfil ?? "";
            IsEditingProfile = false;
            await Shell.Current.DisplayAlert(AppResources.SettingsProfile_SaveSuccessTitle, AppResources.SettingsProfile_SaveSuccessMsg, AppResources.Global_Ok);
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsProfile_SaveError, AppResources.Global_Ok);
        }
    }

    [RelayCommand]
    public async Task JoinFleetAsync()
    {
        if (string.IsNullOrWhiteSpace(InputFleetCode))
        {
            if (Shell.Current != null) await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SettingsEnterCodeError, AppResources.Global_Ok);
            return;
        }

        if (_currentUserData == null || Shell.Current == null) return;
        IsLoading = true;

        string codigoLimpio = InputFleetCode.Trim();
        var (success, message) = await _supabaseService.JoinFleetByCodeAsync(codigoLimpio, _currentUserData.Id);

        if (success)
        {
            InputFleetCode = string.Empty;
            await Shell.Current.DisplayAlert(AppResources.SettingsJoinWelcomeTitle, message, AppResources.Global_Ok);
            await LoadProfileAsync();
        }
        else { await Shell.Current.DisplayAlert(AppResources.SettingsJoinErrorTitle, message, AppResources.SettingsJoinErrorClose); }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        if (Shell.Current == null) return;

        bool confirm = await Shell.Current.DisplayAlert(AppResources.SettingsLogoutConfirmTitle, AppResources.SettingsLogoutConfirmMsg, AppResources.SettingsLogoutConfirmYes, AppResources.SettingsLogoutConfirmCancel);

        if (confirm)
        {
            IsLoading = true;
            await _supabaseService.LogoutAsync();
            IsLoading = false;

            if (Application.Current != null)
            {
                Application.Current.MainPage = new AppShell();
            }
        }
    }
}