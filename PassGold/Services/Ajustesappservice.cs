
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace PassGold.Services;

// Tema (claro / oscuro) e idioma de la app.
// Por defecto TODO es automático: sigue lo que tenga el celular.
// El usuario puede forzar otra cosa desde Ajustes y queda guardado.
public static class AjustesAppService
{
    public const string Auto = "auto";
    public const string Claro = "claro";
    public const string Oscuro = "oscuro";
    public const string Es = "es";
    public const string En = "en";

    // Idioma REAL del celular, capturado antes de aplicar cualquier idioma forzado
    // (así "Automático" siempre puede volver a él).
    private static readonly CultureInfo IdiomaDelSistema = CultureInfo.CurrentUICulture;

    public static string TemaGuardado => Preferences.Default.Get("TemaApp", Auto);
    public static string IdiomaGuardado => Preferences.Default.Get("IdiomaApp", Auto);

    // Se llama UNA vez, al arrancar, antes de crear ninguna pantalla.
    public static void AplicarAlArrancar(Application app)
    {
        AplicarIdioma(IdiomaGuardado, guardar: false);
        app.UserAppTheme = ATema(TemaGuardado);
    }

    public static void AplicarTema(string tema)
    {
        Preferences.Default.Set("TemaApp", tema);
        if (Application.Current != null) Application.Current.UserAppTheme = ATema(tema);
    }

    // Cambia el idioma de los textos. Las pantallas que ya estaban abiertas siguen
    // con el idioma anterior hasta que se vuelven a crear (ver SettingsViewModel).
    public static void AplicarIdioma(string idioma, bool guardar = true)
    {
        if (guardar) Preferences.Default.Set("IdiomaApp", idioma);

        CultureInfo cultura = idioma switch
        {
            Es => new CultureInfo("es"),
            En => new CultureInfo("en"),
            _ => IdiomaDelSistema
        };

        // Idioma de los TEXTOS.
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        CultureInfo.CurrentUICulture = cultura;

        // 🌐 Fechas y horas en el MISMO idioma que los textos (antes el texto salía en
        // inglés pero los días/meses/"a. m." seguían la región del celular, o al revés).
        // Los NÚMEROS se dejan siempre con punto decimal (formato invariante) para que
        // coordenadas, capacidades, etc. se lean y escriban igual en cualquier celular.
        var formato = (CultureInfo)PassGold.Helpers.AppResources.Culture.Clone();
        formato.NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        CultureInfo.DefaultThreadCurrentCulture = formato;
        CultureInfo.CurrentCulture = formato;

        // 🆕 Las notificaciones push también en el idioma de la app
        PushIdentityService.SincronizarIdioma();
    }

    private static AppTheme ATema(string tema) => tema switch
    {
        Claro => AppTheme.Light,
        Oscuro => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };
}