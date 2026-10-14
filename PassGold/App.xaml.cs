using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;
using PassGold.Models.Local;
using PassGold.Services;
using PassGold.ViewModels;
using System;
using System.Threading.Tasks;

namespace PassGold;

public partial class App : Microsoft.Maui.Controls.Application
{
    // Inyector Global Seguro para evitar crashes al navegar
    public static IServiceProvider Services { get; private set; }

    private readonly LocalDatabaseService _localDb;

    public App(IServiceProvider services, LocalDatabaseService localDb)
    {
        InitializeComponent();
        Services = services;
        _localDb = localDb;

        // 🆕 Tema (claro/oscuro) e idioma guardados por el usuario. Si nunca tocó nada,
        // todo sigue automático según el celular. Va ANTES de crear cualquier pantalla.
        AjustesAppService.AplicarAlArrancar(this);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new Views.LoginPage()));

        window.Created += (s, e) =>
        {
            Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await InicializarSesionAsync(window);
            });
        };

        return window;
    }

    private async Task InicializarSesionAsync(Window window)
    {
        try
        {
            var sesionLocal = await _localDb.ObtenerSesionActivaAsync();

            if (sesionLocal != null && !string.IsNullOrEmpty(sesionLocal.Rol))
            {
                // 🔧 Al cerrar sesión se borra la sesión de Supabase del celular, pero la
                // sesión LOCAL se quedaba guardada: al reabrir, la app creía que seguías
                // dentro y te llevaba al dashboard sin sesión real (listas en blanco).
                // Si la sesión de Supabase ya no está guardada, la local sobra.
                // (No se usa el resultado de IsSessionActiveAsync a propósito: sin internet
                // devuelve false aunque la sesión sea válida, y la app debe abrir con caché.)
                bool hayTokenGuardado = true;
                try { hayTokenGuardado = !string.IsNullOrEmpty(await SecureStorage.Default.GetAsync("supabase_session")); }
                catch { /* si no se puede leer, no cerramos la sesión por las dudas */ }

                if (!hayTokenGuardado)
                {
                    await _localDb.CerrarSesionLocalAsync();
                    Preferences.Default.Remove("IsLoggedIn");
                    return;
                }

                bool sesionValida = await SupabaseService.Instance.IsSessionActiveAsync();

                // 🆕 Con internet y aun así sin sesión = Supabase rechazó la sesión guardada
                // (venció o ya se había renovado en otro lado). Antes la app entraba igual
                // al panel con la caché, pero por debajo sin sesión: listas vacías y errores
                // de permisos. Ahora se pide iniciar sesión de nuevo.
                // Sin internet sí se entra con la caché, como antes.
                if (!sesionValida && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    await _localDb.CerrarSesionLocalAsync();
                    Preferences.Default.Remove("IsLoggedIn");
                    try { SecureStorage.Default.Remove("supabase_session"); } catch { }
                    return; // se queda en la pantalla de login
                }

                // 🆕 Desde ahora, cada renovación de la sesión queda guardada
                SesionPersistente.Enganchar();
                Preferences.Default.Set("IsLoggedIn", true);

                // 🆕 Liga este celular con el usuario para que le lleguen SUS avisos
                PushIdentityService.IniciarSesion(sesionLocal.Id);

                await AppShellViewModel.Instance.UpdateMenuStateAsync();

                string rol = sesionLocal.Rol.ToLower();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    window.Page = new AppShell();

                    if (rol.Contains("chofer") || rol.Contains("driver"))
                        await Shell.Current.GoToAsync("//driver-dashboard");
                    else
                        await Shell.Current.GoToAsync("//passenger-dashboard");

                    // 🆕 Si la app se cerró de golpe la vez anterior por una excepción
                    // sin manejar, acá queda guardado el detalle — se muestra una
                    // sola vez y se borra, para no repetirlo en cada apertura.
                    await MostrarUltimoErrorSiExisteAsync();

                    // 🆕 Si la app se abrió tocando el aviso "tu chofer está cerca",
                    // ahora que ya está lista se abre directo el mapa de rastreo.
                    await PushRouting.IntentarNavegarAsync();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error de navegación en inicio: {ex.Message}");
        }
    }

    // 🆕 Ver CrashLogger.cs — acá se revisa y se muestra lo que haya quedado
    // guardado de un cierre inesperado anterior.
    private async Task MostrarUltimoErrorSiExisteAsync()
    {
        try
        {
            var ultimoError = PassGold.Services.CrashLogger.LeerUltimoError();
            if (!string.IsNullOrEmpty(ultimoError) && Shell.Current != null)
            {
                PassGold.Services.CrashLogger.BorrarUltimoError();
                await Shell.Current.DisplayAlert(
                    PassGold.Helpers.AppResources.App_UnexpectedCloseTitle,
#if DEBUG
                    ultimoError,   // en Debug se ve el error completo para depurar
#else
                    PassGold.Helpers.AppResources.App_UnexpectedCloseBody,   // en Release, mensaje genérico
#endif
                    PassGold.Helpers.AppResources.Global_Ok);
            }
        }
        catch { }
    }
}