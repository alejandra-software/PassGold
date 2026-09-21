using Microsoft.Maui.Controls;
using SkiaSharp;
using System;
using System.IO;

namespace PassGold.Helpers;

// Genera, en memoria, la imagen de un pin con el nombre del pasajero escrito
// encima — así el chofer no tiene que adivinar ni tocar cada punto para saber
// quién es quién cuando hay 15-20 pasajeros en el mapa.
//
// Usa Pin.ImageSource, que es una propiedad NATIVA y multiplataforma de
// Microsoft.Maui.Controls.Maps (Android/iOS/Windows) — no hace falta ningún
// handler personalizado por plataforma para esto.
//
// NOTA API: desde SkiaSharp 3.0 (obligatorio ya en 4.x), el tamaño de fuente,
// el tipo de letra y la alineación de texto se movieron de SKPaint a una
// clase separada, SKFont. SKPaint ya solo maneja color/relleno/trazo.
public static class PinImageGenerator
{
    public static ImageSource GenerarPinConNombre(string? nombreCompleto, string colorHex)
    {
        string texto = TruncarNombre(nombreCompleto);
        var color = SKColor.Parse(colorHex);

        // 🔧 FIX: en SkiaSharp 4.x, un SKFont sin tipografía explícita usa
        // SKTypeface.Empty por defecto (ya no null), lo que hace que
        // MeasureText devuelva 0 y no dibuje nada. Por eso pasamos la
        // tipografía directo en el constructor.
        using var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 26);

        using var paintTexto = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        float textWidth = font.MeasureText(texto);
        float padH = 18f;
        float bubbleHeight = 40f;
        float puntaAltura = 14f;

        int totalWidth = (int)Math.Ceiling(textWidth + padH * 2);
        int totalHeight = (int)Math.Ceiling(bubbleHeight + puntaAltura);

        using var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var paintFondo = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        using var paintBorde = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f };

        // Burbuja redondeada
        var rect = new SKRect(1, 1, totalWidth - 1, bubbleHeight);
        canvas.DrawRoundRect(rect, 10, 10, paintFondo);
        canvas.DrawRoundRect(rect, 10, 10, paintBorde);

        // Triangulito apuntando hacia abajo, como un globo de mapa señalando el punto exacto
        using var path = new SKPath();
        float centroX = totalWidth / 2f;
        path.MoveTo(centroX - 9, bubbleHeight - 2);
        path.LineTo(centroX, totalHeight - 1);
        path.LineTo(centroX + 9, bubbleHeight - 2);
        path.Close();
        canvas.DrawPath(path, paintFondo);

        // Texto centrado — MeasureText/Metrics/DrawText ahora viven en SKFont
        SKFontMetrics metrics = font.Metrics;
        float textY = (bubbleHeight / 2f) - (metrics.Ascent + metrics.Descent) / 2f;
        canvas.DrawText(texto, centroX, textY, SKTextAlign.Center, font, paintTexto);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        byte[] bytes = data.ToArray();

        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    // Muestra solo el primer nombre (no el apellido completo) para que la
    // burbuja no ocupe medio mapa — y lo recorta si es muy largo.
    private static string TruncarNombre(string? nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";

        var partes = nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string primerNombre = partes.Length > 0 ? partes[0] : nombreCompleto;

        if (primerNombre.Length > 12)
            primerNombre = primerNombre.Substring(0, 11) + "…";

        return primerNombre;
    }
}