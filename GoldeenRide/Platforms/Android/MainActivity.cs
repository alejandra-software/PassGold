using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Auth.Api.SignIn;
using Android.Runtime;
using GoldeenRide.Services;
using System;

namespace GoldeenRide
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 9001)
            {
                try
                {
                    var task = GoogleSignIn.GetSignedInAccountFromIntent(data);

                    if (task.IsSuccessful && task.Result != null)
                    {
                        
                        var account = task.Result.JavaCast<GoogleSignInAccount>();
                        SupabaseService.GoogleSignInTcs?.TrySetResult((account?.IdToken, string.Empty));
                    }
                    else
                    {
                        // Si Google lo bloquea, extraemos el error nativo (que incluye el código 10 o 12500)
                        string errorMsg = task.Exception?.Message ?? "Cancelado por el usuario o error desconocido.";
                        SupabaseService.GoogleSignInTcs?.TrySetResult((null, errorMsg));
                    }
                }
                catch (Exception ex)
                {
                    // Cualquier otro error de la aplicación
                    SupabaseService.GoogleSignInTcs?.TrySetResult((null, ex.Message));
                }
            }
        }
    }
}