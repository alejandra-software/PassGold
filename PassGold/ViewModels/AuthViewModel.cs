using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection; // Soluciona el error del GetService<Type>
using PassGold.Services;
using PassGold.Models;
using PassGold.Models.Local;

namespace PassGold.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    public static readonly AuthViewModel Instance = new();

    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    // Soluciona la referencia ambigua de Application
    private LocalDatabaseService GetLocalDb() =>
        Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    // --- PROPIEDADES LOGIN ---
    [ObservableProperty] private string loginEmail = string.Empty;
    [ObservableProperty] private string loginPassword = string.Empty;
    [ObservableProperty] private bool isLoginLoading = false;
    [ObservableProperty] private string loginErrorMessage = string.Empty;
    [ObservableProperty] private bool isLoginPasswordHidden = true;
    public string LoginPasswordEyeIcon => IsLoginPasswordHidden ? "???" : "??";

    [RelayCommand]
    public void ToggleLoginPassword()
    {
        IsLoginPasswordHidden = !IsLoginPasswordHidden;
        OnPropertyChanged(nameof(LoginPasswordEyeIcon));
    }

    // --- PROPIEDADES REGISTRO STEP 1 ---
    [ObservableProperty] private string registerEmail = string.Empty;
    [ObservableProperty] private string registerPassword = string.Empty;
    [ObservableProperty] private string registerConfirmPassword = string.Empty;
    [ObservableProperty] private bool isRegisterStep1Loading = false;
    [ObservableProperty] private string registerStep1ErrorMessage = string.Empty;
    [ObservableProperty] private bool isRegisterPasswordHidden = true;
    public string RegisterPasswordEyeIcon => IsRegisterPasswordHidden ? "???" : "??";
    [ObservableProperty] private bool isRegisterConfirmPasswordHidden = true;
    public string RegisterConfirmPasswordEyeIcon => IsRegisterConfirmPasswordHidden ? "???" : "??";

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

    // --- PROPIEDADES REGISTRO STEP 2 ---
    [ObservableProperty] private string registerName = string.Empty;
    [ObservableProperty] private string selectedRole = string.Empty;
    [ObservableProperty] private string registerPhotoPath = string.Empty;
    [ObservableProperty] private bool isRegisterStep2Loading = false;
    [ObservableProperty] private string registerStep2ErrorMessage = string.Empty;

    // --- COMANDOS NAVEGACIÓN ---
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

    // --- LÓGICA DE LOGIN CON CORREO ---
    [RelayCommand]
    public async Task Login()
    {
        // 🔧 FIX: sin esto, tocar "Iniciar Sesión" varias veces seguidas (algo muy
        // fácil de hacer cuando la app se siente lenta) disparaba varios logins en
        // paralelo — con mala suerte de tiempos, podía mezclar sesiones.
        if (IsLoginLoading) return;

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

    // --- LÓGICA DE LOGIN NATIVO DE GOOGLE ---
    [RelayCommand]
    public async Task LoginWithGoogle()
    {
        if (IsLoginLoading) return;

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
                    // GUARDADO OFFLINE
                    await GetLocalDb().GuardarSesionActivaAsync(new UsuarioLocal
                    {
                        Id = usuarioBaseDatos.Id,
                        Email = usuarioBaseDatos.Email,
                        Nombre = usuarioBaseDatos.Nombre,
                        Rol = usuarioBaseDatos.Rol,
                        FotoPerfil = usuarioBaseDatos.FotoPerfil,
                        IdJefe = usuarioBaseDatos.IdJefe
                    });

                    string rolFormateado = usuarioBaseDatos.Rol.ToLower();
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

    // --- REGISTRO STEP 1 ---
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

    // --- REGISTRO STEP 2 ---
    [RelayCommand]
    public async Task RegisterStep2Complete()
    {
        if (IsRegisterStep2Loading) return;

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
                bool success = await _supabaseService.SaveUserDataAsync(currentUser.Id, RegisterName, SelectedRole, RegisterPhotoPath ?? "");

                if (success)
                {
                    // GUARDADO OFFLINE
                    await GetLocalDb().GuardarSesionActivaAsync(new UsuarioLocal
                    {
                        Id = currentUser.Id,
                        Email = currentUser.Email ?? "",
                        Nombre = RegisterName,
                        Rol = SelectedRole,
                        FotoPerfil = RegisterPhotoPath
                    });

                    string rolElegido = SelectedRole.ToLower();

                    RegisterName = string.Empty;
                    SelectedRole = string.Empty;
                    RegisterPhotoPath = string.Empty;

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
                if (string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
                {
                    RegisterStep2ErrorMessage = "Los datos del paso 1 se perdieron. Por favor, dale Atrás y vuelve a ingresarlos.";
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
                    await Shell.Current.DisplayAlert("¡Revisa tu correo!", "Tu cuenta fue creada. Por favor confirma tu correo electrónico antes de iniciar sesión.", "Entendido");
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
        // FIX: si ya venís logueado con Google (o cualquier proveedor externo),
        // "volver al paso 1" no tiene sentido — ese paso es para poner correo y
        // contraseña, y vos ya te autenticaste sin eso. Antes te dejaba volver
        // igual, lo cual era ilógico (¿volver a qué, a escribir un correo que ya
        // no se va a usar?). Ahora, en ese caso, se cancela el registro social
        // (cierra esa sesión parcial) y te manda al Login — el único lugar desde
        // donde tiene sentido arrancar de nuevo.
        var currentUser = _supabaseService.GetCurrentUser();
        if (currentUser != null)
        {
            await _supabaseService.LogoutAsync();
            await GetLocalDb().CerrarSesionLocalAsync();
            await Shell.Current.GoToAsync("///login");
            return;
        }

        await Shell.Current.GoToAsync("///register-step1");
    }

    [RelayCommand]
    public async Task PickPhoto()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Foto de perfil"
            });
            if (photo == null) return;

            IsRegisterStep2Loading = true; // reusamos el mismo spinner del paso 2

            using var stream = await photo.OpenReadAsync();

            //  FIX: SubirImagenAsync ahora devuelve una tupla (string? Url, string Error)
            // en vez de solo string? — antes esto no compilaba (CS0019/CS0029) porque
            // intentaba comparar/asignar la tupla completa como si fuera un string.
            var (url, error) = await CloudinaryService.Instance.SubirImagenAsync(stream, photo.FileName, "perfiles");

            if (url != null)
            {
                RegisterPhotoPath = url; // la URL de Cloudinary, no la ruta local del FilePicker
            }
            else
            {
                //  DIAGNOSTICO TEMPORAL — borrar antes de producción y volver al mensaje simple.
                RegisterStep2ErrorMessage = $"No se pudo subir la foto. Puedes continuar sin foto por ahora.\n\nDIAGNÓSTICO: {error}";
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
        // No había NINGUNA bandera acá — doble-toque en "Cerrar Sesión" podía
        // disparar el logout y la navegación dos veces.
        if (_cerrandoSesion) return;
        _cerrandoSesion = true;

        try
        {
            await _supabaseService.LogoutAsync();
            await GetLocalDb().CerrarSesionLocalAsync();
            await Shell.Current.GoToAsync("///login");
        }
        finally
        {
            _cerrandoSesion = false;
        }
    }
}