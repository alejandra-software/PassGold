namespace PassGold.Helpers;

// Enlaces a las páginas legales publicadas en GitHub Pages.
// Si algún día cambias el nombre del repositorio de las páginas, cambia SOLO BaseUrl.
public static class LegalLinks
{
    public const string BaseUrl = "https://alejandra-software.github.io/passgold-legal";

    public const string CorreoSoporte = "hernandez2026alejandra@gmail.com";

    // La página abre en el mismo idioma que la app
    private static string Lang => AppResources.IsSpanish ? "es" : "en";

    public static string Privacidad => $"{BaseUrl}/privacidad.html?lang={Lang}";
    public static string Terminos => $"{BaseUrl}/terminos.html?lang={Lang}";
    public static string EliminarCuenta => $"{BaseUrl}/eliminar-cuenta.html?lang={Lang}";
}