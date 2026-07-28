using System.Globalization;
using System.Reflection;
using GoldeenRide.Helpers; // Referencia a nuestra clase de textos

namespace GoldeenRide.Services;

/// <summary>
/// Servicio de localización actualizado para usar AppResources de C#
/// </summary>
public class LocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private LocalizationService() { }

    /// <summary>
    /// </summary>
    public string GetString(string key)
    {
        // Busca el texto en la nueva clase estática AppResources de C#
        var propertyInfo = typeof(AppResources).GetProperty(key, BindingFlags.Public | BindingFlags.Static);
        if (propertyInfo != null)
        {
            return propertyInfo.GetValue(null)?.ToString() ?? key;
        }
        return key;
    }

    /// <summary>
    /// Obtiene el idioma actual del dispositivo
    /// </summary>
    public string GetCurrentLanguageCode()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
    }
}