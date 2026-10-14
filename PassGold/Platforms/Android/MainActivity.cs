using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis; // Corrección: Apis en plural para evitar CS0234
using PassGold.Services;

namespace PassGold;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Esta línea fuerza la barra superior a color negro para que combine con la app
        Window.SetStatusBarColor(Android.Graphics.Color.Black);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == 9001)
        {
            try
            {
                var task = GoogleSignIn.GetSignedInAccountFromIntent(data);

                // Corrección: Forzamos la conversión explícita para evitar el Error CS1061
                var account = task.Result as GoogleSignInAccount;

                if (account != null && !string.IsNullOrEmpty(account.IdToken))
                {
                    SupabaseService.GoogleSignInTcs?.TrySetResult((account.IdToken, string.Empty));
                }
                else
                {
                    SupabaseService.GoogleSignInTcs?.TrySetResult((null, PassGold.Helpers.AppResources.Auth_GoogleEmptyToken));
                }
            }
            catch (ApiException apiEx)
            {
                SupabaseService.GoogleSignInTcs?.TrySetResult((null, $"Google API Error: {apiEx.StatusCode}"));
            }
            catch (System.Exception ex)
            {
                SupabaseService.GoogleSignInTcs?.TrySetResult((null, PassGold.Helpers.AppResources.F(PassGold.Helpers.AppResources.Auth_ExceptionPrefix, ex.Message)));
            }
        }
    }
}