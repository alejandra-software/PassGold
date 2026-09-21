using System.Globalization;
using System.Reflection;
using PassGold.Helpers; // Referencia a nuestra clase de textos

namespace PassGold.Services;

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
        // �Y"� CORRECCI�"N: Se agrega BindingFlags.NonPublic para poder leer las propiedades generadas por Visual Studio
        var propertyInfo = typeof(AppResources).GetProperty(key, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

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
