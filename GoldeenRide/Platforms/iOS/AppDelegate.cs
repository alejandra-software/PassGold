using Foundation;
using UIKit;
using Microsoft.Maui.ApplicationModel; // Requerido para usar Platform.OpenUrl

namespace GoldeenRide
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // Este método escucha cuando Safari intenta regresar a la app con el enlace "goldeenride://"
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            // Platform.OpenUrl se encarga de enviarle la información de la URL al WebAuthenticator
            if (Platform.OpenUrl(app, url, options))
                return true;

            return base.OpenUrl(app, url, options);
        }
    }
}