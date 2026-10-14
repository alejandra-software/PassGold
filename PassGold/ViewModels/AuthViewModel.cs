using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using PassGold.Services;
using PassGold.Models;
using PassGold.Models.Local;
using Microsoft.Maui.Media;
using PassGold.Helpers;
using Microsoft.Maui.ApplicationModel;

namespace PassGold.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    public static readonly AuthViewModel Instance = new();
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    [ObservableProperty] private string loginEmail = string.Empty;
    [ObservableProperty] private string loginPassword = string.Empty;
    [ObservableProperty] private bool isLoginLoading = false;
    [ObservableProperty] private string loginErrorMessage = string.Empty;
    [ObservableProperty] private bool isLoginPasswordHidden = true;
    public string LoginPasswordEyeIcon => IsLoginPasswordHidden ? "🙈" : "👁";

    [RelayCommand]
    public void ToggleLoginPassword()
    {
        IsLoginPasswordHidden = !IsLoginPasswordHidden;
        OnPropertyChanged(nameof(LoginPasswordEyeIcon));
    }

    [ObservableProperty] private string registerEmail = string.Empty;
    [ObservableProperty] private string registerPassword = string.Empty;
    [ObservableProperty] private string registerConfirmPassword = string.Empty;
    [ObservableProperty] private bool isRegisterStep1Loading = false;
    [ObservableProperty] private string registerStep1ErrorMessage = string.Empty;
    [ObservableProperty] private bool isRegisterPasswordHidden = true;
    public string RegisterPasswordEyeIcon => IsRegisterPasswordHidden ? "🙈" : "👁";
    [ObservableProperty] private bool isRegisterConfirmPasswordHidden = true;
    public string RegisterConfirmPasswordEyeIcon => IsRegisterConfirmPasswordHidden ? "🙈" : "👁";

    [RelayCommand]
    public void ToggleRegisterPassword()
    {
        IsRegisterPasswordHidden = !IsRegisterPasswordHidden;
        OnPropertyChanged(nameof(RegisterPasswordEyeIcon));
    }

    [RelayCommand]
    public void ToggleRegisterConfirmPassword()
    {
        IsRegisterConfirmPasswordHidden = !IsRegisterConfirmPasswordHidden;
        OnPropertyChanged(nameof(RegisterConfirmPasswordEyeIcon));
    }

    [ObservableProperty] private string registerName = string.Empty;
    [ObservableProperty] private string selectedRole = string.Empty;
    [ObservableProperty] private string registerPhotoPath = string.Empty;
    [ObservableProperty] private bool isRegisterStep2Loading = false;
    [ObservableProperty] private string registerStep2ErrorMessage = string.Empty;

    // 🔧 La pantalla de login a veces vive dentro de una NavigationPage (al abrir la
    // app sin sesión, o después de cerrar sesión) y ahí Shell.Current es null. Antes
    // eso hacía fallar "Regístrate" y el login con Google de una cuenta nueva con
    // "Object reference not set to an instance of an object". Ahora, si no hay Shell,
    // primero se monta y después se navega.
    private static async Task NavegarAsync(string ruta)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current == null)
            {
                var ventana = Application.Current?.Windows.FirstOrDefault();
                if (ventana == null) return;
                ventana.Page = new AppShell();
            }
            if (Shell.Current != null) await Shell.Current.GoToAsync(ruta);
        });
    }

    [RelayCommand]
    public async Task GoToRegister()
    {
        RegisterStep1ErrorMessage = string.Empty;
        await NavegarAsync("//register-step1");
    }

    [RelayCommand]
    public async Task GoToLogin()
    {
        LoginErrorMessage = string.Empty;
        await NavegarAsync("//login");
    }

    [RelayCommand]
    public async Task Login()
    {
        if (IsLoginLoading) return;

        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            LoginErrorMessage = AppResources.AlertEmptyFields;
            return;
        }

        IsLoginLoading = true;
        LoginErrorMessage = string.Empty;

        var (success, message, session) = await _supabaseService.LoginAsync(LoginEmail, LoginPassword);
        await ProcessLoginResult(success, message, session);

        IsLoginLoading = false;
    }

    [RelayCommand]
    public async Task LoginWithGoogle()
    {
        if (IsLoginLoading) return;

        IsLoginLoading = true;
        LoginErrorMessage = string.Empty;

        (bool success, string message, Supabase.Gotrue.Session? session) result;

#if ANDROID
        // Android usa el SDK nativo de Google Play
        result = await _supabaseService.LoginWithGoogleNativeAsync();
#else
        // iPhone usa el navegador Safari de forma segura
        result = await _supabaseService.LoginWithGoogleUniversalAsync();
#endif

        await ProcessLoginResult(result.success, result.message, result.session);

        IsLoginLoading = false;
    }

    private async Task ProcessLoginResult(bool success, string message, Supabase.Gotrue.Session? session)
    {
        try
        {
            if (success && session?.User != null)
            {
                // 🆕 Desde ahora, cada vez que Supabase renueve la sesión se guarda la nueva
                SesionPersistente.Enganchar();

                var usuarioBaseDatos = await _supabaseService.GetUserDataAsync(session.User.Id);

                LoginEmail = string.Empty;
                LoginPassword = string.Empty;

                if (usuarioBaseDatos != null && !string.IsNullOrEmpty(usuarioBaseDatos.Rol))
                {
                    await GetLocalDb().GuardarSesionActivaAsync(new UsuarioLocal
                    {
                        Id = usuarioBaseDatos.Id,
                        Email = usuarioBaseDatos.Email,
                        Nombre = usuarioBaseDatos.Nombre,
                        Rol = usuarioBaseDatos.Rol,
                        FotoPerfil = usuarioBaseDatos.FotoPerfil,
                        IdJefe = usuarioBaseDatos.IdJefe
                    });

                    Preferences.Default.Set("IsLoggedIn", true);

                    // 🆕 Liga este celular con el usuario para que le lleguen SUS avisos push
                    PushIdentityService.IniciarSesion(usuarioBaseDatos.Id);

                    string rolFormateado = usuarioBaseDatos.Rol.ToLower();
                    await AppShellViewModel.Instance.UpdateMenuStateAsync();

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Application.Current!.MainPage = new AppShell();

                        if (rolFormateado.Contains("chofer") || rolFormateado.Contains("driver"))
                            await Shell.Current.GoToAsync("//driver-dashboard");
                        else
                            await Shell.Current.GoToAsync("//passenger-dashboard");
                    });
                }
                else
                {
                    LoginErrorMessage = AppResources.Auth_NewAccountComplete;
                    await NavegarAsync("//register-step2");
                }
            }
            else
            {
                LoginErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            LoginErrorMessage = $"{AppResources.Global_Error}: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task RegisterStep1Next()
    {
        if (string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterStep1ErrorMessage = AppResources.AlertEmptyFields;
            return;
        }
        if (RegisterPassword != RegisterConfirmPassword)
        {
            RegisterStep1ErrorMessage = AppResources.Auth_PasswordsDontMatch;
            return;
        }

        IsRegisterStep1Loading = true;
        RegisterStep1ErrorMessage = string.Empty;

        await Task.Delay(300);
        await NavegarAsync("//register-step2");

        IsRegisterStep1Loading = false;
    }

    [RelayCommand]
    public async Task RegisterStep2Complete()
    {
        if (IsRegisterStep2Loading) return;

        if (string.IsNullOrWhiteSpace(RegisterName) || string.IsNullOrWhiteSpace(SelectedRole))
        {
            RegisterStep2ErrorMessage = AppResources.Auth_SelectRoleAndName;
            return;
        }

        IsRegisterStep2Loading = true;
        RegisterStep2ErrorMessage = string.Empty;

        try
        {
            var currentUser = _supabaseService.GetCurrentUser();

            if (currentUser != null)
            {
                bool success = await _supabaseService.SaveUserDataAsync(currentUser.Id, RegisterName, SelectedRole, RegisterPhotoPath ?? "");

                if (success)
                {
                    await GetLocalDb().GuardarSesionActivaAsync(new UsuarioLocal
                    {
                        Id = currentUser.Id,
                        Email = currentUser.Email ?? "",
                        Nombre = RegisterName,
                        Rol = SelectedRole,
                        FotoPerfil = RegisterPhotoPath
                    });

                    Preferences.Default.Set("IsLoggedIn", true);
                    PushIdentityService.IniciarSesion(currentUser.Id);

                    string rolElegido = SelectedRole.ToLower();

                    RegisterName = string.Empty;
                    SelectedRole = string.Empty;
                    RegisterPhotoPath = string.Empty;

                    await AppShellViewModel.Instance.UpdateMenuStateAsync();

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Application.Current!.MainPage = new AppShell();

                        if (rolElegido.Contains("chofer"))
                            await Shell.Current.GoToAsync("//driver-dashboard");
                        else
                            await Shell.Current.GoToAsync("//passenger-dashboard");
                    });

                    return;
                }
                else
                {
                    RegisterStep2ErrorMessage = AppResources.SettingsProfile_SaveError;
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
                {
                    RegisterStep2ErrorMessage = AppResources.Auth_Step1DataLost;
                    return;
                }

                var (authSuccess, authMessage) = await _supabaseService.RegisterAsync(RegisterEmail, RegisterPassword, RegisterName, SelectedRole, RegisterPhotoPath ?? "");

                if (!authSuccess)
                {
                    RegisterStep2ErrorMessage = authMessage;
                    return;
                }

                RegisterEmail = string.Empty;
                RegisterPassword = string.Empty;
                RegisterConfirmPassword = string.Empty;
                RegisterName = string.Empty;
                SelectedRole = string.Empty;
                RegisterPhotoPath = string.Empty;

                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(AppResources.Auth_CheckEmailTitle, AppResources.Auth_CheckEmailMsg, AppResources.Global_Ok);
                }

                await NavegarAsync("//login");
            }
        }
        catch (Exception ex)
        {
            RegisterStep2ErrorMessage = ex.Message;
        }
        finally
        {
            IsRegisterStep2Loading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterStep2Back()
    {
        var currentUser = _supabaseService.GetCurrentUser();
        if (currentUser != null)
        {
            await _supabaseService.LogoutAsync();
            await GetLocalDb().CerrarSesionLocalAsync();
            Preferences.Default.Remove("IsLoggedIn");
            PushIdentityService.CerrarSesion();
            await NavegarAsync("//login");
            return;
        }

        await NavegarAsync("//register-step1");
    }

    [RelayCommand]
    public async Task PickPhoto()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = AppResources.Auth_ProfilePhotoPicker
            });
            if (photo == null) return;

            IsRegisterStep2Loading = true;

            using var stream = await photo.OpenReadAsync();
            var (url, error) = await CloudinaryService.Instance.SubirImagenAsync(stream, photo.FileName, "perfiles");

            if (url != null)
            {
                RegisterPhotoPath = url;
            }
            else
            {
                RegisterStep2ErrorMessage = AppResources.SettingsProfile_UploadError + AppResources.F(AppResources.Global_Diagnostic, error);
            }
        }
        catch { }
        finally
        {
            IsRegisterStep2Loading = false;
        }
    }

    private bool _cerrandoSesion = false;

    [RelayCommand]
    public async Task Logout()
    {
        if (_cerrandoSesion) return;
        _cerrandoSesion = true;

        try
        {
            // 🆕 Si el chofer estaba compartiendo su ubicación, se apaga ya mismo
            LiveTrackingService.Instance.Detener();
            // 🆕 Este celular deja de recibir los avisos de esta cuenta
            PushIdentityService.CerrarSesion();

            await _supabaseService.LogoutAsync();
            await GetLocalDb().CerrarSesionLocalAsync();
            Preferences.Default.Remove("IsLoggedIn");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.MainPage = new NavigationPage(new Views.LoginPage());
            });
        }
        finally
        {
            _cerrandoSesion = false;
        }
    }
}