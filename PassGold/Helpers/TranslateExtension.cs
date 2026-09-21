using System;
using System.Reflection;
using Microsoft.Maui.Controls.Xaml;

namespace PassGold.Helpers;

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
        // 1. Si no hay llave, devolvemos vac�o para no crashear
        if (string.IsNullOrWhiteSpace(Key)) return "";

        try
        {
            // ?? CORRECCI�N: Se agrega BindingFlags.NonPublic para encontrar los textos de AppResources
            var propertyInfo = typeof(AppResources).GetProperty(Key, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(null)?.ToString() ?? Key;
            }

            // 3. Si te equivocas en una letra en el XAML, te mostrar� la llave escrita en lugar de explotar
            return $"[{Key}]";
        }
        catch
        {
            // 4. Salvavidas extremo: Si la Reflexi�n falla por completo
            return $"[{Key}]";
        }
    }
}
