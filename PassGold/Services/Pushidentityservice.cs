using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;

namespace PassGold.Services;

// Todo lo que toca OneSignal desde la app vive acá, en un solo lugar.
// Así, si una versión futura del SDK cambia un nombre, se arregla en este archivo.
public static class PushIdentityService
{
    // Liga este celular con el usuario de Supabase. El servidor (Edge Function
    // "rastreo-chofer") le manda el aviso a ese mismo id, por eso es imprescindible.
    public static void IniciarSesion(string userId)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(userId)) OneSignal.Login(userId);
        }
        catch (Exception ex) { Debug.WriteLine($"OneSignal.Login falló: {ex.Message}"); }

        SincronizarIdioma();
    }

    // 🆕 Le dice a OneSignal en qué idioma tiene la app ESTE usuario (el que eligió en
    // Ajustes, o el del celular si está en "Automático"). Así el aviso "tu micro está
    // cerca" llega en el mismo idioma que la app, y no según el idioma del teléfono.
    public static void SincronizarIdioma()
    {
        try { OneSignal.User.Language = PassGold.Helpers.AppResources.IsSpanish ? "es" : "en"; }
        catch (Exception ex) { Debug.WriteLine($"OneSignal.User.Language falló: {ex.Message}"); }
    }

    // Al cerrar sesión, este celular deja de recibir los avisos de esa cuenta.
    public static void CerrarSesion()
    {
        try { OneSignal.Logout(); }
        catch (Exception ex) { Debug.WriteLine($"OneSignal.Logout falló: {ex.Message}"); }
    }

    public static bool PermisoConcedido()
    {
        try { return OneSignal.Notifications.Permission; }
        catch { return false; }
    }

    // Muestra el aviso del sistema "¿Permitir notificaciones?". Se llama SOLO cuando el
    // usuario activa la opción en Ajustes (antes se pedía al instalar la app).
    // Si ya estaba concedido devuelve true sin mostrar nada.
    public static async Task<bool> PedirPermisoAsync()
    {
        try { return await OneSignal.Notifications.RequestPermissionAsync(false); }
        catch (Exception ex)
        {
            Debug.WriteLine($"RequestPermission falló: {ex.Message}");
            return false;
        }
    }
}