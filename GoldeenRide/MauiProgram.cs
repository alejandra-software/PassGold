using Microsoft.Extensions.Logging;

namespace GoldeenRide
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Servicios
            builder.Services.AddSingleton<GoldeenRide.Services.SupabaseService>();
            builder.Services.AddSingleton<GoldeenRide.Services.LocalizationService>();

            // ViewModels
            builder.Services.AddSingleton<GoldeenRide.ViewModels.AuthViewModel>();

            // Vistas
            builder.Services.AddSingleton<GoldeenRide.Views.LoginPage>();
            builder.Services.AddSingleton<GoldeenRide.Views.RegisterStep1Page>();
            builder.Services.AddSingleton<GoldeenRide.Views.RegisterStep2Page>();
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}