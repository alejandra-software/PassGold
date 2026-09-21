using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting; // Obligatorio para los mapas

namespace PassGold
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            //  FIX: sqlite-net-pcl necesita que el motor nativo de SQLitePCLRaw se
            // registre explícitamente. En Android esto pasaba solo (el linker no
            // recorta el código de auto-registro), pero en iOS, con compilación AOT
            // + recorte agresivo, ese registro automático se puede perder — la
            // primera llamada a la base local (SQLite) se cuelga sin tirar ninguna
            // excepción capturable, nunca vuelve, y como PassengerDashboardViewModel
            // usa la base local apenas entra a la pantalla, TODA la UI queda
            // bloqueada detrás del overlay de "cargando" que nunca se apaga — eso
            // explica que ni la barra lateral responda, no es un tema de esa pantalla
            // en particular, es que el hilo/tarea que debía continuar nunca continúa.
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps() // Enciende el motor nativo de Google/Apple Maps

                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Servicios
            builder.Services.AddSingleton<PassGold.Services.SupabaseService>();
            builder.Services.AddSingleton<PassGold.Services.LocalizationService>();
            builder.Services.AddSingleton<PassGold.Services.LocalDatabaseService>();
            // ViewModels
            builder.Services.AddSingleton<PassGold.ViewModels.AuthViewModel>();

            // Vistas
            builder.Services.AddSingleton<PassGold.Views.LoginPage>();
            builder.Services.AddSingleton<PassGold.Views.RegisterStep1Page>();
            builder.Services.AddSingleton<PassGold.Views.RegisterStep2Page>();
            builder.Services.AddTransient<PassGold.Views.FleetProfilePage>();
            builder.Services.AddTransient<PassGold.ViewModels.FleetProfileViewModel>();
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}