using Android.App;
using Android.Content;
using Android.Content.PM;

namespace PassGold.Platforms.Android;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "https",
    DataHost = "dqvmeqgxsocfsscqevlp.supabase.co", // <-- URL corregida (qg)
    DataPathPrefix = "/auth/v1/callback")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}