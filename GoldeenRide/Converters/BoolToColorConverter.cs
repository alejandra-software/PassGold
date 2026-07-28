using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace GoldeenRide.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b && b;
        string[] colors = parameter?.ToString()?.Split('|') ?? new[] { "Primary", "Transparent" };

        string colorResource = isTrue ? colors[0] : colors[1];

        //  Busca el color en tus diccionarios (Styles.xaml / Colors.xaml)
        if (Application.Current != null && Application.Current.Resources.TryGetValue(colorResource, out var color) && color is Color c1)
        {
            return c1;
        }

        //  Si no lo encuentra, intenta leerlo como un color genérico de sistema
        if (Color.TryParse(colorResource, out Color c2))
        {
            return c2;
        }

        return colorResource == "Transparent" ? Colors.Transparent : Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}