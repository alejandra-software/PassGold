using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using GoldeenRide.Services;
using GoldeenRide.Models;

namespace GoldeenRide.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    public static readonly AuthViewModel Instance = new();

    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    // ─── PROPIEDADES LOGIN ───────────────────────────────────
    [ObservableProperty] private string loginEmail = string.Empty;
    [ObservableProperty] private string loginPassword = string.Empty;
    [ObservableProperty] private bool isLoginLoading = false;
    [ObservableProperty] private string loginErrorMessage = string.Empty;
    [ObservableProperty] private bool isLoginPasswordHidden = true;
    public string LoginPasswordEyeIcon => IsLoginPasswordHidden ? "👁️" : "🙈";

    [RelayCommand]
    public void ToggleLoginPassword()
    {
        IsLoginPasswordHidden = !IsLoginPasswordHidden;
        OnPropertyChanged(nameof(LoginPasswordEyeIcon));
    }

    // ─── PROPIEDADES REGISTRO STEP 1 ─────────────────────────
    [ObservableProperty] private string registerEmail = string.Empty;
    [ObservableProperty] private string registerPassword = string.Empty;
    [ObservableProperty] private string registerConfirmPassword = string.Empty;
    [ObservableProperty] private bool isRegisterStep1Loading = false;
    [ObservableProperty] private string registerStep1ErrorMessage = string.Empty;
    [ObservableProperty] private bool isRegisterPasswordHidden = true;
    public string RegisterPasswordEyeIcon => IsRegisterPasswordHidden ? "👁️" : "🙈";
    [ObservableProperty] private bool isRegisterConfirmPasswordHidden = true;
    public string RegisterConfirmPasswordEyeIcon => IsRegisterConfirmPasswordHidden ? "👁️" : "🙈";

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

    // ─── PROPIEDADES REGISTRO STEP 2 ─────────────────────────
    [ObservableProperty] private string registerName = string.Empty;
    [ObservableProperty] private string selectedRole = string.Empty;
    [ObservableProperty] private string registerPhotoPath = string.Empty;
    [ObservableProperty] private bool isRegisterStep2Loading = false;
    [ObservableProperty] private string registerStep2ErrorMessage = string.Empty;

    // ─── COMANDOS NAVEGACIÓN ─────────────────────────
    [RelayCommand]
    public async Task GoToRegister()
    {
        RegisterStep1ErrorMessage = string.Empty;
        await Shell.Current.GoToAsync("///register-step1");
    }

    [RelayCommand]
    public async Task GoToLogin()
    {
        LoginErrorMessage = string.Empty;
        await Shell.Current.GoToAsync("///login");
    }

    // ─── LÓGICA DE LOGIN CON CORREO ──────────────────
    [RelayCommand]
    public async Task Login()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            LoginErrorMessage = "Llena todos los campos";
            return;
        }

        IsLoginLoading = true;
        LoginErrorMessage = string.Empty;

        var (success, message, session) = await _supabaseService.LoginAsync(LoginEmail, LoginPassword);
        await ProcessLoginResult(success, message, session);

        IsLoginLoading = false;
    }

    // ─── LÓGICA DE LOGIN NATIVO DE GOOGLE ──────────────────
    [RelayCommand]
    public async Task LoginWithGoogle()
    {
        IsLoginLoading = true;
        LoginErrorMessage = string.Empty;

        var (success, message, session) = await _supabaseService.LoginWithGoogleNativeAsync();
        await ProcessLoginResult(success, message, session);

        IsLoginLoading = false;
    }

    private async Task ProcessLoginResult(bool success, string message, Supabase.Gotrue.Session? session)
    {
        try
        {
            if (success && session?.User != null)
            {
                var usuarioBaseDatos = await _supabaseService.GetUserDataAsync(session.User.Id);

                LoginEmail = string.Empty;
                LoginPassword = string.Empty;

                if (usuarioBaseDatos != null && !string.IsNullOrEmpty(usuarioBaseDatos.Rol))
                {
                    string rolFormateado = usuarioBaseDatos.Rol.ToLower();

                    // 🔥 LA MAGIA: Encendemos y actualizamos el menú de 3 rayas ANTES de cambiar de pantalla
                    await AppShellViewModel.Instance.UpdateMenuStateAsync();

                    if (rolFormateado.Contains("chofer") || rolFormateado.Contains("driver"))
                        await Shell.Current.GoToAsync("///driver-dashboard");
                    else
                        await Shell.Current.GoToAsync("///passenger-dashboard");
                }
                else
                {
                    LoginErrorMessage = "Cuenta nueva. Por favor completa tu registro.";
                    await Shell.Current.GoToAsync("///register-step2");
                }
            }
            else
            {
                LoginErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            LoginErrorMessage = $"Error: {ex.Message}";
        }
    }

    // ─── REGISTRO STEP 1 ─────────────────────────────────────
    [RelayCommand]
    public async Task RegisterStep1Next()
    {
        if (string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterStep1ErrorMessage = "Llena todos los campos.";
            return;
        }
        if (RegisterPassword != RegisterConfirmPassword)
        {
            RegisterStep1ErrorMessage = "Las contraseñas no coinciden.";
            return;
        }

        IsRegisterStep1Loading = true;
        RegisterStep1ErrorMessage = string.Empty;

        await Task.Delay(300);
        await Shell.Current.GoToAsync("///register-step2");

        IsRegisterStep1Loading = false;
    }

    // ─── REGISTRO STEP 2 ─────────────────────────────────────
    [RelayCommand]
    public async Task RegisterStep2Complete()
    {
        if (string.IsNullOrWhiteSpace(RegisterName) || string.IsNullOrWhiteSpace(SelectedRole))
        {
            RegisterStep2ErrorMessage = "Por favor selecciona un rol y escribe tu nombre.";
            return;
        }

        IsRegisterStep2Loading = true;
        RegisterStep2ErrorMessage = string.Empty;

        try
        {
            var currentUser = _supabaseService.GetCurrentUser();

            if (currentUser != null)
            {
                // CASO A: USUARIO DE GOOGLE
                bool success = await _supabaseService.SaveUserDataAsync(currentUser.Id, RegisterName, SelectedRole, RegisterPhotoPath ?? "");

                if (success)
                {
                    string rolElegido = SelectedRole.ToLower();

                    RegisterName = string.Empty;
                    SelectedRole = string.Empty;
                    RegisterPhotoPath = string.Empty;

                    // 🔥 LA MAGIA: Encendemos el menú de 3 rayas al terminar el registro
                    await AppShellViewModel.Instance.UpdateMenuStateAsync();

                    if (rolElegido.Contains("chofer"))
                        await Shell.Current.GoToAsync("///driver-dashboard");
                    else
                        await Shell.Current.GoToAsync("///passenger-dashboard");

                    return;
                }
                else
                {
                    RegisterStep2ErrorMessage = "No se pudo guardar tu perfil. Intenta de nuevo.";
                    return;
                }
            }
            else
            {
                // CASO B: REGISTRO NORMAL (CORREO)
                if (string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
                {
                    RegisterStep2ErrorMessage = "Los datos del paso 1 se perdieron. Por favor, dale Atrás y vuelve a ingresarlos.";
                    return;
                }

                var (authSuccess, authMessage) = await _supabaseService.RegisterAsync(
                    RegisterEmail,
                    RegisterPassword,
                    RegisterName,
                    SelectedRole,
                    RegisterPhotoPath ?? ""
                );

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
                    await Shell.Current.DisplayAlert(
                        "✅ ¡Revisa tu correo!",
                        "Tu cuenta fue creada. Por favor confirma tu correo electrónico antes de iniciar sesión.",
                        "Entendido"
                    );
                }

                await Shell.Current.GoToAsync("///login");
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
        await Shell.Current.GoToAsync("///register-step1");
    }

    [RelayCommand]
    public async Task PickPhoto()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Foto de perfil" });
            if (result != null) RegisterPhotoPath = result.FullPath;
        }
        catch { }
    }

    [RelayCommand]
    public async Task Logout()
    {
        await _supabaseService.LogoutAsync();
        await Shell.Current.GoToAsync("///login");
    }
}