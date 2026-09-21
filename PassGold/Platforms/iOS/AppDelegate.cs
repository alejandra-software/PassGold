using Foundation;
using UIKit;
using Microsoft.Maui.ApplicationModel;

namespace PassGold
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // Este m�todo escucha cuando Safari intenta regresar a la app con el enlace "PassGold://"
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (Platform.OpenUrl(app, url, options))
                return true;

            return base.OpenUrl(app, url, options);
        }
    }
}
