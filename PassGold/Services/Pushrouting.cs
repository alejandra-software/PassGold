using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PassGold.Services;

// Cuando el pasajero toca el aviso "tu chofer está cerca":
//  · Con la app ABIERTA  -> abre el mapa de rastreo al instante.
//  · Con la app CERRADA  -> (lo más común: el pasajero está esperando en su casa)
//    el toque llega ANTES de que exista la pantalla principal, así que el destino
//    queda guardado y App.xaml.cs lo abre apenas la sesión está lista.
//    Antes se intentaba navegar de inmediato y fallaba justo en este caso.
public static class PushRouting
{
    private static string? _idAsignacionPendiente;
    private static bool _navegando;

    public static void Recibir(string idAsignacion)
    {
        if (string.IsNullOrWhiteSpace(idAsignacion)) return;
        _idAsignacionPendiente = idAsignacion.Trim();
        MainThread.BeginInvokeOnMainThread(async () => await IntentarNavegarAsync());
    }

    // Se puede llamar las veces que haga falta: solo navega si ya hay algo pendiente
    // y la app está lista (pantalla principal armada + sesión iniciada).
    public static async Task IntentarNavegarAsync()
    {
        if (_navegando || string.IsNullOrEmpty(_idAsignacionPendiente)) return;
        if (Shell.Current == null || !Preferences.Default.Get("IsLoggedIn", false)) return;

        _navegando = true;
        try
        {
            string id = _idAsignacionPendiente!;
            _idAsignacionPendiente = null;

            // Se registra justo antes de usarla. Si AppShell ya la tenía registrada,
            // Routing avisa con ArgumentException y simplemente seguimos.
            try { Routing.RegisterRoute("mapa-pasajero", typeof(Views.MapaRastreoPage)); }
            catch (ArgumentException) { }

            await Shell.Current.GoToAsync($"mapa-pasajero?IdAsignacionRastreo={Uri.EscapeDataString(id)}");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "PushRouting.IntentarNavegarAsync");
        }
        finally
        {
            _navegando = false;
        }
    }
}