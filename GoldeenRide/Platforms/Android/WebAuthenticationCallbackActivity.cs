using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace GoldeenRide; 

//  clase le dice a Android que despierte la aplicación cuando el navegador intente abrir la URL "goldeenride://"
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
              Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
              DataScheme = "goldeenride")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}