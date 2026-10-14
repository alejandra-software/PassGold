using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace PassGold.Services;

// 🆕 Guarda el último error real (con pila de llamadas completa) en un archivo
// dentro del propio celular — así se puede ver sin tener la laptop conectada.
// Se guarda en dos momentos:
//   1) Cuando un try/catch de la app atrapa algo (ver ScheduleTripViewModel,
//      EditTripViewModel) — ahí además se muestra un mensaje en pantalla al toque.
//   2) Como red de seguridad global, por si algo se escapa de todos los try/catch
//      que ya existen (ver el enganche en MauiProgram.cs) — este caso no muestra
//      alerta en el momento (la app ya se está cerrando), pero sí queda guardado
//      para mostrarlo la próxima vez que se abra (ver App.xaml.cs).
public static class CrashLogger
{
    private static string LogPath => Path.Combine(FileSystem.AppDataDirectory, "ultimo_error.txt");

    public static void Guardar(Exception ex, string origen)
    {
        try
        {
            string contenido = $"=== ERROR EN: {origen} ===\nFecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{ex}\n";
            File.WriteAllText(LogPath, contenido);
        }
        catch { /* si ni esto se puede guardar, no hay nada más que hacer */ }
    }

    public static string? LeerUltimoError()
    {
        try
        {
            if (File.Exists(LogPath)) return File.ReadAllText(LogPath);
        }
        catch { }
        return null;
    }

    public static void BorrarUltimoError()
    {
        try { if (File.Exists(LogPath)) File.Delete(LogPath); } catch { }
    }
}