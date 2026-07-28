using System;
using System.Reflection;
using Microsoft.Maui.Controls.Xaml;

namespace GoldeenRide.Helpers;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return Translate();
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return Translate();
    }

    private string Translate()
    {
        // 1. Si no hay llave, devolvemos vacío para no crashear
        if (string.IsNullOrWhiteSpace(Key)) return "";

        try
        {
            // 2. Buscamos el texto de forma segura
            var propertyInfo = typeof(AppResources).GetProperty(Key, BindingFlags.Public | BindingFlags.Static);

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(null)?.ToString() ?? Key;
            }

            // 3. Si te equivocas en una letra en el XAML, te mostrará la llave escrita en lugar de explotar
            return $"[{Key}]";
        }
        catch
        {
            // 4. Salvavidas extremo: Si la Reflexión falla por completo
            return $"[{Key}]";
        }
    }
}