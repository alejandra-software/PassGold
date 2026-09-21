using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace PassGold; 

//  clase le dice a Android que despierte la aplicaci�n cuando el navegador intente abrir la URL "PassGold://"
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
              Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
              DataScheme = "PassGold")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
