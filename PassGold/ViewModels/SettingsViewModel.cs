// Evita el choque con Android.App.Application al compilar para Android
using Application = Microsoft.Maui.Controls.Application;
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using PassGold.Helpers;

namespace PassGold.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly CloudinaryService _cloudinaryService = CloudinaryService.Instance;

    [ObservableProperty] private string userName = AppResources.Global_Loading;
    [ObservableProperty] private string userRole = "";
    [ObservableProperty] private string userEmail = ""; // Añadido para mostrar el correo
    [ObservableProperty] private string userPhone = ""; // Añadido para mostrar el teléfono actual
    [ObservableProperty] private string userPhotoUrl = "";

    [ObservableProperty] private bool hasUserPhoto = false;

    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private bool isPassenger = false; // Identificador para pasajeros
    [ObservableProperty] private bool hasBoss = false;

    [ObservableProperty] private string myFleetCode = AppResources.Global_Loading;
    [ObservableProperty] private string inputFleetCode = "";
    [ObservableProperty] private string currentBossName = "";

    [ObservableProperty] private bool isLoading = false;

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
                UserRole = AppResources.RoleLabel(_currentUserData.Rol);
                UserEmail = authUser.Email ?? _currentUserData.Email ?? AppResources.Settings_NotRegistered;
                UserPhone = string.IsNullOrWhiteSpace(_currentUserData.Telefono) ? AppResources.Settings_NoPhone : _currentUserData.Telefono;
                UserPhotoUrl = _currentUserData.FotoPerfil ?? "";

                string rolLower = (_currentUserData.Rol ?? "").ToLower();
                IsFleetManager = rolLower.Contains("jefe") || rolLower.Contains("owner") || rolLower.Contains("independiente");
                IsEmployee = rolLower.Contains("empleado");
                IsPassenger = rolLower.Contains("pasajero") || rolLower.Contains("passenger");

                if (IsFleetManager)
                {
                    MyFleetCode = await _supabaseService.GetOrCreateFleetCodeAsync(_currentUserData.Id) ?? AppResources.Settings_CodeGenError;
                }

                if (IsEmployee && !string.IsNullOrEmpty(_currentUserData.IdJefe))
                {
                    HasBoss = true;
                    var jefe = await _supabaseService.GetUserDataAsync(_currentUserData.IdJefe);
                    CurrentBossName = jefe?.Nombre ?? AppResources.Settings_UnknownBoss;
                }

                // 🆕 Interruptores, tema e idioma con lo que el usuario tenía guardado
                await CargarPreferenciasAsync();
            }
        }
        IsLoading = false;
    }

    [RelayCommand]
    public void ToggleEditProfile()
    {
        if (!IsEditingProfile && _currentUserData != null)
        {
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
                await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsProfile_UploadError + AppResources.F(AppResources.Global_Diagnostic, error), AppResources.Global_Ok);
            }
        }
        catch
        {
            // Ignorar cancelación
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
            UserPhone = string.IsNullOrWhiteSpace(_currentUserData.Telefono) ? AppResources.Settings_NoPhone : _currentUserData.Telefono;
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
            // 🔧 Antes solo se cerraba la sesión de Supabase: la sesión LOCAL quedaba
            // guardada y al reabrir la app entraba al dashboard "fantasma". Ahora se usa
            // el mismo camino que el resto de la app (AuthViewModel.Logout): limpia la
            // sesión de Supabase Y la local, apaga el rastreo del chofer y desliga los
            // avisos push de este celular.
            await AuthViewModel.Instance.LogoutCommand.ExecuteAsync(null);
            IsLoading = false;
        }
    }

    // =====================================================================
    // 🆕 CAMBIAR DE ROL
    //    Todo lo delicado (qué se borra, qué pasa al jefe...) lo hace Supabase
    //    en una sola transacción: ver 04_cambio_de_rol_y_permisos.sql
    // =====================================================================
    [RelayCommand]
    public async Task CambiarRolAsync()
    {
        var page = PaginaActual();
        if (page == null || _currentUserData == null || IsLoading) return;

        string actual = RolesFlota.Normalizar(_currentUserData.Rol);
        var opciones = new[] { RolesFlota.Pasajero, RolesFlota.Independiente, RolesFlota.Jefe, RolesFlota.Empleado }
            .Where(r => r != actual)
            .ToList();
        var etiquetas = opciones.Select(r => AppResources.RoleLabel(r)).ToArray();

        // 1) Elegir el rol nuevo
        string? elegido = await page.DisplayActionSheet(AppResources.Role_ChooseTitle, AppResources.DeleteAcc_Cancel, null, etiquetas);
        int idx = Array.IndexOf(etiquetas, elegido);
        if (idx < 0) return;
        string nuevo = opciones[idx];
        string etiquetaNueva = etiquetas[idx];

        // 2) Para ser empleado hace falta el código de la flota del jefe
        string? codigo = null;
        if (nuevo == RolesFlota.Empleado)
        {
            codigo = await page.DisplayPromptAsync(AppResources.Role_CodeTitle, AppResources.Role_CodeMsg,
                                                   AppResources.Role_Continue, AppResources.DeleteAcc_Cancel, "ABC123", 6);
            if (string.IsNullOrWhiteSpace(codigo)) return;
        }

        // 3) Explicar qué va a pasar y confirmar
        bool seguir = await page.DisplayAlert(AppResources.F(AppResources.Role_ConfirmTitle, etiquetaNueva),
                                              DescribirCambio(actual, nuevo),
                                              AppResources.Role_ConfirmYes, AppResources.DeleteAcc_Cancel);
        if (!seguir) return;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Alerta(AppResources.Global_Attention, AppResources.SettingsProfile_NeedInternet);
            return;
        }

        IsLoading = true;
        try
        {
            var (ok, error, idJefe, nombreJefe, _) = await _supabaseService.CambiarRolAsync(nuevo, codigo);
            if (!ok)
            {
                string msg = error switch
                {
                    "codigo_invalido" => AppResources.Role_ErrInvalidCode,
                    "mismo_rol" => AppResources.Role_ErrSame,
                    _ => AppResources.Role_ErrGeneric
                };
                await Alerta(AppResources.Global_Error, msg);
                return;
            }

            // Si deja de ser chofer, se apaga el rastreo
            if (nuevo == RolesFlota.Pasajero)
            {
                LiveTrackingService.CompartirActivado = false;
                LiveTrackingService.Instance.Detener();
            }

            // Sesión guardada en el celular con el rol nuevo + caché limpia
            var localDb = Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(LocalDatabaseService)) as LocalDatabaseService ?? new LocalDatabaseService();
            var sesion = await localDb.ObtenerSesionActivaAsync();
            if (sesion != null)
            {
                sesion.Rol = nuevo;
                sesion.IdJefe = idJefe;
                await localDb.GuardarSesionActivaAsync(sesion);
            }
            await localDb.BorrarCacheAsync();

            string hecho = AppResources.F(AppResources.Role_DoneMsg, etiquetaNueva);
            if (!string.IsNullOrEmpty(nombreJefe)) hecho += AppResources.F(AppResources.Role_DoneJoined, nombreJefe);
            await Alerta(AppResources.Role_DoneTitle, hecho);

            // Menú y pantalla de inicio según el rol nuevo
            await AppShellViewModel.Instance.UpdateMenuStateAsync();
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
                window.Page = new AppShell();
                await Shell.Current.GoToAsync(nuevo == RolesFlota.Pasajero ? "//passenger-dashboard" : "//driver-dashboard");
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "SettingsViewModel.CambiarRolAsync");
            await Alerta(AppResources.Global_Error, AppResources.Role_ErrGeneric);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string DescribirCambio(string actual, string nuevo)
    {
        if (actual == RolesFlota.Pasajero) return AppResources.Role_FromPassenger + AppResources.Role_NoUndo;

        if (actual == RolesFlota.Independiente)
        {
            if (nuevo == RolesFlota.Jefe) return AppResources.Role_IndepToBoss;
            if (nuevo == RolesFlota.Empleado) return AppResources.Role_IndepToEmployee;
            return AppResources.Role_CloseFleetToPassenger + AppResources.Role_NoUndo;
        }

        if (actual == RolesFlota.Jefe)
        {
            if (nuevo == RolesFlota.Independiente) return AppResources.Role_BossToIndep;
            if (nuevo == RolesFlota.Empleado) return AppResources.Role_BossToEmployee + AppResources.Role_NoUndo;
            return AppResources.Role_CloseFleetToPassenger + AppResources.Role_NoUndo;
        }

        // Empleado
        return nuevo == RolesFlota.Pasajero ? AppResources.Role_EmployeeToPassenger : AppResources.Role_EmployeeToOwner;
    }

    // =====================================================================
    // 🆕 PRIVACIDAD, TÉRMINOS Y ELIMINAR CUENTA (lo exige Google Play)
    // =====================================================================
    [ObservableProperty] private bool isDeletingAccount = false;

    [RelayCommand]
    public Task AbrirPrivacidadAsync() => AbrirEnlaceAsync(LegalLinks.Privacidad);

    [RelayCommand]
    public Task AbrirTerminosAsync() => AbrirEnlaceAsync(LegalLinks.Terminos);

    private static async Task AbrirEnlaceAsync(string url)
    {
        try { await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred); }
        catch { await Alerta(AppResources.Global_Attention, AppResources.F(AppResources.Legal_OpenError, url)); }
    }

    [RelayCommand]
    public async Task EliminarCuentaAsync()
    {
        var page = PaginaActual();
        if (page == null || _currentUserData == null || IsDeletingAccount) return;

        // 1) Explicar qué se borra, según el rol
        string mensaje = IsFleetManager ? AppResources.DeleteAcc_MsgOwner
                       : IsEmployee ? AppResources.DeleteAcc_MsgEmployee
                       : AppResources.DeleteAcc_MsgPassenger;
        bool seguir = await page.DisplayAlert(AppResources.DeleteAcc_ConfirmTitle, mensaje,
                                              AppResources.DeleteAcc_Continue, AppResources.DeleteAcc_Cancel);
        if (!seguir) return;

        // 2) Segunda confirmación: escribir la palabra (evita borrados por un toque accidental)
        string? escrito = await page.DisplayPromptAsync(AppResources.DeleteAcc_TypeTitle, AppResources.DeleteAcc_TypeMsg,
                                                        AppResources.DeleteAcc_TypeBtn, AppResources.DeleteAcc_Cancel,
                                                        AppResources.DeleteAcc_TypeWord, 20);
        if (escrito == null) return;
        if (!string.Equals(escrito.Trim(), AppResources.DeleteAcc_TypeWord, StringComparison.OrdinalIgnoreCase))
        {
            await Alerta(AppResources.Global_Attention, AppResources.DeleteAcc_WrongWord);
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Alerta(AppResources.Global_Attention, AppResources.SettingsProfile_NeedInternet);
            return;
        }

        IsDeletingAccount = true;
        IsLoading = true;
        try
        {
            // Si el chofer estaba compartiendo su ubicación, se apaga antes de borrar
            LiveTrackingService.CompartirActivado = false;
            LiveTrackingService.Instance.Detener();

            var (ok, _) = await _supabaseService.EliminarCuentaAsync();
            if (!ok)
            {
                await Alerta(AppResources.Global_Error, AppResources.F(AppResources.DeleteAcc_Error, LegalLinks.CorreoSoporte));
                return;
            }

            // Borrar también lo guardado en este celular
            var localDb = Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(LocalDatabaseService)) as LocalDatabaseService ?? new LocalDatabaseService();
            await localDb.BorrarTodoAsync();

            await Alerta(AppResources.DeleteAcc_DoneTitle, AppResources.DeleteAcc_DoneMsg);

            // Mismo camino que "Cerrar sesión": desliga los avisos push y vuelve al inicio
            await AuthViewModel.Instance.LogoutCommand.ExecuteAsync(null);
        }
        finally
        {
            IsDeletingAccount = false;
            IsLoading = false;
        }
    }

    // =====================================================================
    // 🆕 AJUSTES DE LA APP
    //    · Chofer:   compartir mi ubicación
    //    · Pasajero: avisarme cuando mi chofer esté cerca (+ a cuántos metros)
    //    · Todos:    tema (claro/oscuro) e idioma — automáticos por defecto
    // =====================================================================
    private static readonly int[] MetrosOpciones = { 100, 200, 300, 500, 1000 };
    private static readonly string[] TemasValores = { AjustesAppService.Auto, AjustesAppService.Claro, AjustesAppService.Oscuro };
    private static readonly string[] IdiomasValores = { AjustesAppService.Auto, AjustesAppService.Es, AjustesAppService.En };

    // Mientras se cargan los valores guardados NO deben dispararse las acciones de los
    // interruptores (pedir permisos, guardar en Supabase, recargar la app...).
    private bool _suprimirCambios = false;

    private static bool EsEspanol => AppResources.IsSpanish;
    private static string Tx(string es, string en) => EsEspanol ? es : en;

    [ObservableProperty] private bool compartirUbicacion = false;
    [ObservableProperty] private bool avisosCercania = false;
    [ObservableProperty] private int distanciaIndex = 0;
    [ObservableProperty] private int temaIndex = 0;
    [ObservableProperty] private int idiomaIndex = 0;

    public List<string> OpcionesTema { get; } = new()
    {
        Tx("Automático (igual que el celular)", "Automatic (same as phone)"),
        Tx("Claro", "Light"),
        Tx("Oscuro", "Dark")
    };

    public List<string> OpcionesIdioma { get; } = new()
    {
        Tx("Automático (idioma del celular)", "Automatic (phone language)"),
        "Español",
        "English"
    };

    public List<string> OpcionesDistancia { get; } = new() { "100 m", "200 m", "300 m", "500 m", "1 km" };

    public string TxtApariencia => Tx("Apariencia e idioma", "Appearance & language");
    public string TxtTema => Tx("Tema de la app", "App theme");
    public string TxtIdioma => Tx("Idioma", "Language");
    public string TxtCompartirTitulo => Tx("Compartir mi ubicación en mis viajes", "Share my location during my trips");
    public string TxtCompartirDesc => Tx(
        "Tus pasajeros verán por dónde vienes, solo durante el horario de tu viaje y solo los que viajan contigo.",
        "Your passengers will see where you are, only during your trip hours and only those riding with you.");
    public string TxtAvisosTitulo => Tx("Avisarme cuando mi chofer esté cerca", "Notify me when my driver is near");
    public string TxtAvisosDesc => Tx(
        "Recibirás una notificación cuando tu chofer se acerque a tu punto de recogida.",
        "You will get a notification when your driver gets close to your pickup point.");
    public string TxtDistanciaAviso => Tx("Avisarme cuando esté a:", "Notify me when it is within:");
    public string TxtDistanciaAyuda => Tx(
        "Referencia: a 30 km/h, 100 m son unos 12 segundos y 300 m unos 36. Elige más distancia si necesitas tiempo para salir.",
        "Reference: at 30 km/h, 100 m is about 12 seconds and 300 m about 36. Pick a longer distance if you need time to get out.");

    private int MetrosSeleccionados => MetrosOpciones[Math.Clamp(DistanciaIndex, 0, MetrosOpciones.Length - 1)];

    private async Task CargarPreferenciasAsync()
    {
        PreferenciasAviso? prefs = null;
        if (IsPassenger && _currentUserData != null)
            prefs = await PreferenciasAvisoService.ObtenerAsync(_currentUserData.Id);

        // Desde acá NO hay más "await": los valores se cargan sin disparar acciones.
        _suprimirCambios = true;
        try
        {
            TemaIndex = Math.Max(0, Array.IndexOf(TemasValores, AjustesAppService.TemaGuardado));
            IdiomaIndex = Math.Max(0, Array.IndexOf(IdiomasValores, AjustesAppService.IdiomaGuardado));
            CompartirUbicacion = LiveTrackingService.CompartirActivado;

            if (prefs != null)
            {
                AvisosCercania = prefs.AvisosCercania;
                int idx = Array.IndexOf(MetrosOpciones, prefs.DistanciaAvisoMetros);
                DistanciaIndex = idx >= 0 ? idx : 0;
            }
        }
        finally { _suprimirCambios = false; }
    }

    private void PonerSinDisparar(Action cambio)
    {
        _suprimirCambios = true;
        try { cambio(); }
        finally { _suprimirCambios = false; }
    }

    // ---------- CHOFER: compartir ubicación ----------
    partial void OnCompartirUbicacionChanged(bool value)
    {
        if (_suprimirCambios) return;
        _ = AplicarCompartirUbicacionAsync(value);
    }

    private async Task AplicarCompartirUbicacionAsync(bool activar)
    {
        if (!activar)
        {
            LiveTrackingService.CompartirActivado = false;
            LiveTrackingService.Instance.Detener();
            return;
        }

        // El permiso se pide AHORA, en Ajustes y con el carro parado — nunca a mitad de
        // un viaje mientras maneja.
        PermissionStatus estado = PermissionStatus.Unknown;
        try
        {
            estado = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (estado != PermissionStatus.Granted)
                estado = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
        catch { }

        if (estado != PermissionStatus.Granted)
        {
            PonerSinDisparar(() => CompartirUbicacion = false);
            LiveTrackingService.CompartirActivado = false;
            await OfrecerAbrirAjustesDelCelularAsync(
                Tx("Permiso de ubicación", "Location permission"),
                Tx("Sin el permiso de ubicación no podemos compartir tu posición con tus pasajeros. Puedes activarlo en los ajustes del celular.",
                   "Without location permission we can't share your position with your passengers. You can enable it in the phone settings."));
            return;
        }

        LiveTrackingService.CompartirActivado = true;

        // 🆕 Permiso de notificaciones (Android 13+): para que el chofer VEA la
        // notificación "Compartiendo tu ubicación" mientras dura el viaje.
        try { await PushIdentityService.PedirPermisoAsync(); } catch { }
    }

    // ---------- PASAJERO: avisos de cercanía ----------
    partial void OnAvisosCercaniaChanged(bool value)
    {
        if (_suprimirCambios) return;
        _ = AplicarAvisosAsync(value);
    }

    private async Task AplicarAvisosAsync(bool activar)
    {
        string? userId = _currentUserData?.Id;
        if (string.IsNullOrEmpty(userId))
        {
            PonerSinDisparar(() => AvisosCercania = !activar);
            return;
        }

        if (activar)
        {
            // El permiso de notificaciones se pide ACÁ (antes se pedía al instalar la app)
            bool permiso = await PushIdentityService.PedirPermisoAsync();
            if (!permiso)
            {
                PonerSinDisparar(() => AvisosCercania = false);
                await OfrecerAbrirAjustesDelCelularAsync(
                    Tx("Notificaciones desactivadas", "Notifications are off"),
                    Tx("Para avisarte cuando tu chofer esté cerca necesitamos el permiso de notificaciones. Puedes activarlo en los ajustes del celular.",
                       "To tell you when your driver is near we need notification permission. You can enable it in the phone settings."));
                return;
            }
            PushIdentityService.IniciarSesion(userId);
        }

        bool guardado = await PreferenciasAvisoService.GuardarAsync(userId, activar, MetrosSeleccionados);
        if (!guardado)
        {
            PonerSinDisparar(() => AvisosCercania = !activar);
            await Alerta(
                Tx("Sin conexión", "No connection"),
                Tx("No se pudo guardar el cambio. Revisa tu internet e inténtalo de nuevo.",
                   "The change couldn't be saved. Check your internet and try again."));
        }
    }

    partial void OnDistanciaIndexChanged(int value)
    {
        if (_suprimirCambios || value < 0 || !AvisosCercania) return;
        _ = GuardarDistanciaAsync();
    }

    private async Task GuardarDistanciaAsync()
    {
        string? userId = _currentUserData?.Id;
        if (string.IsNullOrEmpty(userId)) return;

        bool ok = await PreferenciasAvisoService.GuardarAsync(userId, AvisosCercania, MetrosSeleccionados);
        if (!ok)
        {
            await Alerta(
                Tx("Sin conexión", "No connection"),
                Tx("No se pudo guardar la distancia. Revisa tu internet e inténtalo de nuevo.",
                   "The distance couldn't be saved. Check your internet and try again."));
        }
    }

    // ---------- TODOS: tema e idioma ----------
    partial void OnTemaIndexChanged(int value)
    {
        if (_suprimirCambios || value < 0 || value >= TemasValores.Length) return;
        AjustesAppService.AplicarTema(TemasValores[value]);   // se ve al instante
    }

    partial void OnIdiomaIndexChanged(int value)
    {
        if (_suprimirCambios || value < 0 || value >= IdiomasValores.Length) return;

        string nuevo = IdiomasValores[value];
        if (nuevo == AjustesAppService.IdiomaGuardado) return;

        AjustesAppService.AplicarIdioma(nuevo);
        _ = RecargarAppAsync();
    }

    // Los textos ya dibujados no cambian solos de idioma: se vuelve a armar la app
    // (mismo gesto que ya usa App.xaml.cs al abrir) y se regresa al inicio.
    private async Task RecargarAppAsync()
    {
        try
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window == null) return;

            window.Page = new AppShell();
            await Shell.Current.GoToAsync(IsPassenger ? "//passenger-dashboard" : "//driver-dashboard");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "SettingsViewModel.RecargarAppAsync");
        }
    }

    // ---------- ayudas ----------
    private static Page? PaginaActual() =>
        Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    private static async Task Alerta(string titulo, string mensaje)
    {
        var page = PaginaActual();
        if (page != null) await page.DisplayAlert(titulo, mensaje, "OK");
    }

    private static async Task OfrecerAbrirAjustesDelCelularAsync(string titulo, string mensaje)
    {
        var page = PaginaActual();
        if (page == null) return;

        bool abrir = await page.DisplayAlert(titulo, mensaje, Tx("Abrir ajustes", "Open settings"), Tx("Ahora no", "Not now"));
        if (abrir) AppInfo.Current.ShowSettingsUI();
    }
}