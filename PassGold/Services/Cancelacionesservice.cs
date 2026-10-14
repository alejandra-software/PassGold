using Newtonsoft.Json.Linq;
using PassGold.Helpers;
using PassGold.Models;
using PassGold.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Operador = Supabase.Postgrest.Constants.Operator;

namespace PassGold.Services;

// Todo lo relacionado con cancelar viajes que ya tienen pasajeros.
// La parte delicada (borrar en el orden correcto y dejar constancia del motivo)
// la hacen funciones de Supabase en UNA transacción: ver supabase_cancelaciones.sql.
public static class CancelacionesService
{
    // En el idioma del chofer (el motivo elegido es el texto que verán los pasajeros)
    public static string[] MotivosRapidos => new[]
    {
        AppResources.Cancel_ReasonNoClasses,
        AppResources.Cancel_ReasonRain,
        AppResources.Cancel_ReasonDriverSick,
        AppResources.Cancel_ReasonMechanical,
        AppResources.Cancel_ReasonFewPassengers,
        AppResources.Cancel_ReasonOther
    };

    private static string Volver => AppResources.Alert_GoBack;

    // ───────────────────────── Chofer: pedir el motivo ─────────────────────────

    // Devuelve null si el chofer se arrepintió.
    public static async Task<string?> PedirMotivoAsync(int pasajerosAfectados)
    {
        var shell = Shell.Current;
        if (shell == null) return null;

        string titulo = pasajerosAfectados == 1
            ? AppResources.Cancel_AskReasonOne
            : AppResources.F(AppResources.Cancel_AskReasonMany, pasajerosAfectados);

        var motivos = MotivosRapidos;
        string? elegido = await shell.DisplayActionSheet(titulo, Volver, null, motivos);
        if (string.IsNullOrEmpty(elegido) || elegido == Volver) return null;

        if (elegido == motivos[^1])
        {
            string? escrito = await shell.DisplayPromptAsync(
                AppResources.Cancel_ReasonPromptTitle,
                AppResources.Cancel_ReasonPromptMsg,
                AppResources.Global_Done, Volver, AppResources.Cancel_ReasonPromptPlaceholder, 120);
            return string.IsNullOrWhiteSpace(escrito) ? null : escrito.Trim();
        }

        // Sin el emoji del principio: "📚 No hay clases" → "No hay clases"
        int espacio = elegido.IndexOf(' ');
        return espacio > 0 ? elegido[(espacio + 1)..] : elegido;
    }

    // ───────────────────────── Llamadas a Supabase ─────────────────────────

    private static async Task<Supabase.Client?> ClienteAsync()
    {
        var client = SupabaseService.Instance.Client;
        if (client == null)
        {
            await SupabaseService.Instance.IsSessionActiveAsync();
            client = SupabaseService.Instance.Client;
        }
        return client;
    }

    public static async Task<(bool Ok, int Asignaciones, int Reservas, string Error)> CancelarAsignacionesAsync(IList<string> idsAsignaciones, string motivo)
    {
        if (idsAsignaciones.Count == 0) return (true, 0, 0, "");
        try
        {
            var client = await ClienteAsync();
            if (client == null) return (false, 0, 0, AppResources.Global_NoServerConnection);

            var resp = await client.Rpc("cancelar_asignaciones", new Dictionary<string, object>
            {
                { "p_ids", idsAsignaciones.ToList() },
                { "p_motivo", motivo }
            });

            var json = JObject.Parse(resp.Content ?? "{}");
            return (true, json.Value<int?>("asignaciones") ?? 0, json.Value<int?>("reservas") ?? 0, "");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "CancelacionesService.CancelarAsignacionesAsync");
            return (false, 0, 0, MensajeLegible(ex));
        }
    }

    // Termina la serie desde ese día (hora de El Salvador) en adelante; los días
    // anteriores quedan como historial.
    public static async Task<(bool Ok, int Asignaciones, int Reservas, bool ViajeBorrado, string Error)> TerminarSerieAsync(string idViaje, DateTime desde, string motivo)
    {
        try
        {
            var client = await ClienteAsync();
            if (client == null) return (false, 0, 0, false, AppResources.Global_NoServerConnection);

            var resp = await client.Rpc("terminar_serie", new Dictionary<string, object>
            {
                { "p_viaje", idViaje },
                { "p_desde", desde.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                { "p_motivo", motivo }
            });

            var json = JObject.Parse(resp.Content ?? "{}");
            return (true,
                json.Value<int?>("asignaciones") ?? 0,
                json.Value<int?>("reservas") ?? 0,
                json.Value<bool?>("viaje_borrado") ?? false,
                "");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "CancelacionesService.TerminarSerieAsync");
            return (false, 0, 0, false, MensajeLegible(ex));
        }
    }

    public static async Task<(bool Ok, int Movidas, string Error)> MoverReservasAsync(string idAsignacionDesde, string idAsignacionHacia)
    {
        try
        {
            var client = await ClienteAsync();
            if (client == null) return (false, 0, AppResources.Global_NoServerConnection);

            var resp = await client.Rpc("mover_reservas", new Dictionary<string, object>
            {
                { "p_desde", idAsignacionDesde },
                { "p_hacia", idAsignacionHacia }
            });

            int.TryParse((resp.Content ?? "0").Trim(), out int movidas);
            return (true, movidas, "");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "CancelacionesService.MoverReservasAsync");
            return (false, 0, MensajeLegible(ex));
        }
    }

    public static async Task<(bool Ok, string Error)> ReemplazarParadasAsync(string idViaje, IEnumerable<WaypointTemporal> metas)
    {
        try
        {
            var client = await ClienteAsync();
            if (client == null) return (false, AppResources.Global_NoServerConnection);

            var paradas = new JArray(metas.Select(m => new JObject
            {
                ["nombre"] = m.Alias,
                ["lat"] = m.Latitud,
                ["lon"] = m.Longitud
            }));

            await client.Rpc("reemplazar_paradas", new Dictionary<string, object>
            {
                { "p_viaje", idViaje },
                { "p_paradas", paradas }
            });
            return (true, "");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "CancelacionesService.ReemplazarParadasAsync");
            return (false, MensajeLegible(ex));
        }
    }

    // Cuántas reservas activas hay en estas asignaciones (para pedir motivo o no)
    public static async Task<int> ContarReservasActivasAsync(IList<string> idsAsignaciones)
    {
        int total = 0;
        var client = await ClienteAsync();
        if (client == null) return 0;

        for (int i = 0; i < idsAsignaciones.Count; i += 100)
        {
            var lote = idsAsignaciones.Skip(i).Take(100).ToList();
            var resp = await client.From<Reserva>().Filter("id_asignacion", Operador.In, lote).Get();
            total += (resp.Models ?? new List<Reserva>()).Count(r => r.Estado != "cancelado");
        }
        return total;
    }

    // Supabase devuelve el error como JSON; nos quedamos con el texto que escribimos en la función
    private static string MensajeLegible(Exception ex)
    {
        string msg = ex.Message ?? "";
        try
        {
            int inicio = msg.IndexOf('{');
            if (inicio >= 0)
            {
                var json = JObject.Parse(msg[inicio..]);
                string? texto = json.Value<string>("message");
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    if (texto.Contains("Could not find the function", StringComparison.OrdinalIgnoreCase))
                        return AppResources.Cancel_SupabaseMissingFunction;
                    return texto;
                }
            }
        }
        catch { }
        if (msg.Contains("Could not find the function", StringComparison.OrdinalIgnoreCase))
            return AppResources.Cancel_SupabaseMissingFunction;
        return msg;
    }

    // ───────────────────────── Pasajero: ver avisos ─────────────────────────

    private static bool _mostrando = false;

    // Se llama al abrir el panel del pasajero. Muestra una sola alerta con todos los
    // viajes que le cancelaron desde la última vez, y los marca como vistos.
    public static async Task MostrarAvisosPendientesAsync()
    {
        if (_mostrando) return;
        _mostrando = true;
        try
        {
            var client = await ClienteAsync();
            var yo = SupabaseService.Instance.GetCurrentUser();
            if (client == null || yo == null || string.IsNullOrEmpty(yo.Id)) return;

            var resp = await client.From<Cancelacion>()
                .Filter("id_pasajero", Operador.Equals, yo.Id)
                .Filter("visto", Operador.Equals, "false")
                .Get();

            var pendientes = (resp.Models ?? new List<Cancelacion>())
                .OrderBy(c => c.FechaViaje)
                .ToList();
            if (pendientes.Count == 0) return;

            var cultura = AppResources.Culture;
            var texto = new StringBuilder();

            // 🔧 Antes salía una línea por cada día (y repetidas si había dos reservas
            // el mismo día): 39 líneas imposibles de leer. Ahora se agrupa por horario y
            // motivo: "Ida 6:15 p. m. · del lun 5 al vie 30 oct (20 días)".
            var grupos = pendientes
                .GroupBy(c => new
                {
                    Tipo = AppResources.TripTypeLabel(c.TipoViaje),
                    Hora = c.FechaViaje.AHoraElSalvador().TimeOfDay,
                    Motivo = c.Motivo.Trim()
                })
                .OrderBy(g => g.Min(c => c.FechaViaje))
                .ToList();

            int totalDias = 0;
            foreach (var g in grupos.Take(5))
            {
                var dias = g.Select(c => c.FechaViaje.AHoraElSalvador().Date).Distinct().OrderBy(d => d).ToList();
                totalDias += dias.Count;
                string hora = DateTime.Today.Add(g.Key.Hora).ToString("h:mm tt", cultura);
                string cuando = dias.Count == 1
                    ? dias[0].ToString(AppResources.Fmt_DayMonthLong, cultura)
                    : AppResources.F(AppResources.Cancel_RangeDays, dias.First().ToString(AppResources.Fmt_DayAbbrevNumMonth, cultura), dias.Last().ToString(AppResources.Fmt_DayAbbrevNumMonth, cultura), dias.Count);
                texto.AppendLine($"• {g.Key.Tipo} {hora}");
                texto.AppendLine($"   {cuando}");
                texto.AppendLine(AppResources.F(AppResources.Cancel_ReasonLine, g.Key.Motivo));
            }
            foreach (var g in grupos.Skip(5))
                totalDias += g.Select(c => c.FechaViaje.AHoraElSalvador().Date).Distinct().Count();
            if (grupos.Count > 5)
                texto.AppendLine(AppResources.F(AppResources.Cancel_MoreSlots, grupos.Count - 5));
            texto.AppendLine();
            texto.Append(AppResources.Cancel_NoLongerActive);

            string titulo = totalDias == 1 ? AppResources.Cancel_TitleOne : AppResources.F(AppResources.Cancel_TitleMany, totalDias);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(titulo, texto.ToString(), AppResources.PassengerDash_Understood);
            });

            // Marcar como vistas recién después de que el pasajero tocó "Entendido"
            var ids = pendientes.Select(c => c.Id).ToList();
            try
            {
                await client.Rpc("marcar_cancelaciones_vistas", new Dictionary<string, object> { { "p_ids", ids } });
            }
            catch (Exception ex) { CrashLogger.Guardar(ex, "CancelacionesService.MarcarVistas"); }
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "CancelacionesService.MostrarAvisosPendientesAsync");
        }
        finally
        {
            _mostrando = false;
        }
    }
}