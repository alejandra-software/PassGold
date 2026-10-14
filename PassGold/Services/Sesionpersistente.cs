using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using AuthState = Supabase.Gotrue.Constants.AuthState;

namespace PassGold.Services;

// 🆕 Mantiene guardada en el celular la sesión de Supabase MÁS RECIENTE.
//
// El problema: la sesión solo se guardaba al iniciar sesión. Supabase la renueva
// sola cada hora y, al renovarla, el "refresh token" viejo deja de servir. Como el
// nuevo nunca se guardaba, al abrir la app otro día se intentaba reanudar con el
// token viejo, Supabase lo rechazaba y la app quedaba SIN sesión por debajo
// (aunque mostrara el panel con datos de la caché). Sin sesión, las reglas de
// seguridad no dejan leer casi nada: por ejemplo, el directorio de flotas sale vacío.
public static class SesionPersistente
{
    private static bool _enganchado = false;

    // Se llama después de iniciar sesión y al abrir la app. Llamarla varias veces no hace daño.
    public static void Enganchar()
    {
        if (_enganchado) return;
        var client = SupabaseService.Instance.Client;
        if (client == null) return;

        client.Auth.AddStateChangedListener((sender, estado) =>
        {
            if (estado == AuthState.SignedIn || estado == AuthState.TokenRefreshed || estado == AuthState.UserUpdated)
            {
                var sesion = sender.CurrentSession;
                if (sesion == null || string.IsNullOrEmpty(sesion.RefreshToken)) return;
                _ = GuardarAsync(JsonConvert.SerializeObject(sesion));
            }
        });

        _enganchado = true;

        // Guardar también la que está activa ahora mismo
        var actual = client.Auth.CurrentSession;
        if (actual != null && !string.IsNullOrEmpty(actual.RefreshToken))
            _ = GuardarAsync(JsonConvert.SerializeObject(actual));
    }

    private static async Task GuardarAsync(string json)
    {
        try { await SecureStorage.Default.SetAsync("supabase_session", json); }
        catch (Exception ex) { CrashLogger.Guardar(ex, "SesionPersistente.GuardarAsync"); }
    }
}