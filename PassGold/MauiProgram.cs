using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;

namespace PassGold
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

            // 🆕 Red de seguridad global: cualquier excepción que se escape de todos
            // los try/catch que ya existen en la app (algo que no debería pasar, pero
            // pasa) se guarda en un archivo del celular en vez de perderse. No se
            // puede mostrar un cartel en este momento exacto porque el sistema ya
            // está cerrando la app — por eso App.xaml.cs revisa este archivo la
            // PRÓXIMA vez que se abre y, si encuentra algo, lo muestra ahí.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    PassGold.Services.CrashLogger.Guardar(ex, "AppDomain.UnhandledException (crash real, no manejado por ningún try/catch)");
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                PassGold.Services.CrashLogger.Guardar(e.Exception, "TaskScheduler.UnobservedTaskException (una Task async que falló sin que nadie la esperara)");
                e.SetObserved();
            };

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // 🚀 INICIALIZAR NOTIFICACIONES PUSH CON ONESIGNAL
            // (Reemplaza "TU_APP_ID_DE_ONESIGNAL_AQUI" cuando crees tu app gratis en su plataforma)
            OneSignal.Initialize("3b047102-aad6-4e24-8f3b-06309a90334d");

            // 🔧 Antes acá se pedía el permiso de notificaciones apenas se instalaba la app.
            // Ahora se pide cuando el pasajero ACTIVA "Avisarme cuando mi chofer esté cerca"
            // en Ajustes (ver SettingsViewModel / PushIdentityService.PedirPermisoAsync).

            // 🎯 EVENTO: ¿Qué pasa cuando el pasajero toca la notificación?
            OneSignal.Notifications.Clicked += (sender, e) =>
            {
                if (e.Notification.AdditionalData != null &&
                    e.Notification.AdditionalData.TryGetValue("id_asignacion", out var idViajeObj))
                {
                    string idViaje = idViajeObj?.ToString() ?? "";

                    // 🔧 Antes se navegaba directo con Shell.Current, que NO existe todavía
                    // si la app estaba cerrada cuando se tocó la notificación (el caso más
                    // común). PushRouting guarda el destino y lo abre apenas la app está lista.
                    PassGold.Services.PushRouting.Recibir(idViaje);
                }
            };

            // --- SERVICIOS BASE ---
            builder.Services.AddSingleton<PassGold.Services.SupabaseService>();
            builder.Services.AddSingleton<PassGold.Services.LocalizationService>();
            builder.Services.AddSingleton<PassGold.Services.LocalDatabaseService>();
            builder.Services.AddSingleton<PassGold.ViewModels.AuthViewModel>();

            // --- VISTAS GLOBALES ---
            builder.Services.AddTransient<PassGold.Views.LoginPage>();
            builder.Services.AddTransient<PassGold.Views.RegisterStep1Page>();
            builder.Services.AddTransient<PassGold.Views.RegisterStep2Page>();
            builder.Services.AddSingleton<MainPage>();

            // --- DASHBOARDS Y PERFILES ---
            builder.Services.AddTransient<PassGold.Views.PassengerDashboardPage>();
            builder.Services.AddTransient<PassGold.ViewModels.PassengerDashboardViewModel>();

            builder.Services.AddTransient<PassGold.Views.DriverDashboardPage>();
            builder.Services.AddTransient<PassGold.ViewModels.DriverDashboardViewModel>();

            builder.Services.AddTransient<PassGold.Views.FleetProfilePage>();
            builder.Services.AddTransient<PassGold.ViewModels.FleetProfileViewModel>();

            // --- TODAS LAS DEMÁS VISTAS (Mayúsculas corregidas) ---
            builder.Services.AddTransient<PassGold.Views.ScheduleTripPage>();

            // 🔥 LÍNEA FALTANTE AGREGADA AQUÍ:
            builder.Services.AddTransient<PassGold.ViewModels.ScheduleTripViewModel>();

            builder.Services.AddTransient<PassGold.Views.ActiveTripPage>();
            builder.Services.AddTransient<PassGold.ViewModels.ActiveTripViewModel>();

            builder.Services.AddTransient<PassGold.Views.AddVehiclePage>();
            // 🔥 Agrego preventivamente estos ViewModels para evitar que el mismo error te pase en estas otras ventanas al compilar:
            builder.Services.AddTransient<PassGold.ViewModels.AdminFleetViewModel>();
            builder.Services.AddTransient<PassGold.ViewModels.FleetDirectoryViewModel>();

            builder.Services.AddTransient<PassGold.Views.AdminFleetPage>();
            builder.Services.AddTransient<PassGold.Views.ConfirmarReservaPage>();
            builder.Services.AddTransient<PassGold.Views.EditTripPage>();
            // 🔧 FIX: faltaba esta línea — sin ella, App.Services.GetService<EditTripViewModel>()
            // devolvía null y le pisaba el BindingContext bueno que ponía el XAML de
            // EditTripPage, dejando toda la pantalla de "Editar Viaje" sin datos.
            builder.Services.AddTransient<PassGold.ViewModels.EditTripViewModel>();
            builder.Services.AddTransient<PassGold.Views.EditVehiclePage>();
            builder.Services.AddTransient<PassGold.Views.FleetDirectoryPage>();
            builder.Services.AddTransient<PassGold.Views.FleetSchedulePage>();

            // CORREGIDO A PASCAL-CASE:
            builder.Services.AddTransient<PassGold.Views.PasajeroDetallePage>();
            builder.Services.AddTransient<PassGold.Views.PasajeroReservaPage>();
            builder.Services.AddTransient<PassGold.Views.PassengerRulesPage>();
            builder.Services.AddTransient<PassGold.Views.PhotoViewerPage>();
            builder.Services.AddTransient<PassGold.Views.SetHomeLocationPage>();

            builder.Services.AddTransient<PassGold.Views.SettingsPage>();
            builder.Services.AddTransient<PassGold.Views.TripDetailsPage>();

            return builder.Build();
        }
    }
}