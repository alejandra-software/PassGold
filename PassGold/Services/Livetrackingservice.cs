using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using Newtonsoft.Json.Linq;

namespace PassGold.Services;

// Publica la posición del CHOFER mientras dura SU viaje — y nada más.
//
//  · Solo se enciende dentro de la ventana [inicio, fin] que le pasa quien lo llama
//    (sale de "hora de inicio real" y "llegada real" del viaje). Antes de la hora de
//    inicio NO usa el GPS; pasada la hora de llegada se apaga solo.
//  · Cada envío va a la Edge Function "rastreo-chofer", que comprueba que la
//    asignación es de este chofer, guarda la posición y avisa a los pasajeros.
//  · FASE 1 (esta versión): funciona mientras la app esté abierta al frente.
//    Mientras comparte mantiene la pantalla encendida. El servicio en segundo plano
//    (Android) viene en la fase 2.
public class LiveTrackingService
{
    // Mismos valores que ya usan SupabaseService.cs y CloudinaryService.cs
    private const string SupabaseUrl = "https://dqvmeqgxsocfsscqevlp.supabase.co";
    private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImRxdm1lcWd4c29jZnNzY3FldmxwIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODE2Mjk3MDQsImV4cCI6MjA5NzIwNTcwNH0.6U1l7GqzVc9-iDD2PppSwxeKnXXTx5uq15BPRhosAak";
    // Nombre de la Edge Function en Supabase. El editor de Supabase le puso un nombre
    // automático al crearla (igual que "bright-responder" para Cloudinary): si algún día
    // se vuelve a desplegar con otro nombre, solo hay que cambiar esta línea.
    private const string NombreFuncion = "bright-service";
    private static readonly string FunctionUrl = $"{SupabaseUrl}/functions/v1/{NombreFuncion}";

    private const int IntervaloSegundos = 8;

    private static LiveTrackingService? _instance;
    public static LiveTrackingService Instance => _instance ??= new LiveTrackingService();

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private CancellationTokenSource? _cts;

    public bool EstaCompartiendo { get; private set; }
    public string? IdAsignacionActiva { get; private set; }

    // Se dispara en el hilo principal: true = empezó a compartir, false = se detuvo.
    public event Action<bool>? EstadoCambio;
    // Mensajes para mostrarle al chofer (se disparan en el hilo principal).
    public event Action<string>? Problema;

    // Estado visible en la pantalla del viaje ("✅ Ubicación enviada a las 14:05:12").
    // Sirve para que el chofer (y tú al probar) sepa si de verdad se está enviando.
    public string EstadoTexto { get; private set; } = string.Empty;
    public event Action<string>? EstadoTextoCambio;

    private void Informar(string texto)
    {
        EstadoTexto = texto;
        MainThread.BeginInvokeOnMainThread(() => EstadoTextoCambio?.Invoke(texto));
    }

    // Opción "Compartir mi ubicación" del chofer (se guarda en el propio celular).
    // Mismo nombre que ya usaba el interruptor de Ajustes ("CompartirGpsChofer").
    // Arranca APAGADA: el chofer la enciende en Ajustes, y ahí mismo se le pide el
    // permiso de ubicación — nunca a mitad de un viaje mientras maneja.
    public static bool CompartirActivado
    {
        get => Preferences.Default.Get("CompartirGpsChofer", false);
        set => Preferences.Default.Set("CompartirGpsChofer", value);
    }

    private LiveTrackingService() { }

    // Viajes que el chofer finalizó a mano hoy: el arranque automático no los
    // vuelve a encender (si no, al tocar "Finalizar" se reencendería solo).
    private static readonly HashSet<string> _finalizadasManual = new();
    public static void MarcarFinalizadaManual(string idAsignacion) { lock (_finalizadasManual) _finalizadasManual.Add(idAsignacion); }
    public static bool FueFinalizadaManual(string idAsignacion) { lock (_finalizadasManual) return _finalizadasManual.Contains(idAsignacion); }

    // Ventana de rastreo de un viaje (hora local de El Salvador):
    //   inicio = hora de inicio del recorrido (o de salida) − 10 min
    //   fin    = hora de llegada (o salida + 2 h) + 15 min
    public static (DateTime inicio, DateTime fin) VentanaDelViaje(DateTime dia, PassGold.Models.Viaje viaje)
    {
        TimeSpan horaInicio = viaje.HoraInicioRecorrido ?? PassGold.Helpers.ZonaHorariaHelper.AHoraElSalvador(viaje.HoraSalida).TimeOfDay;
        TimeSpan horaFin = viaje.HoraLlegadaDestino ?? horaInicio.Add(TimeSpan.FromHours(2));
        DateTime inicio = dia.Date.Add(horaInicio).AddMinutes(-10);
        DateTime fin = dia.Date.Add(horaFin).AddMinutes(15);
        if (horaFin < horaInicio) fin = fin.AddDays(1); // cruza la medianoche
        return (inicio, fin);
    }

    // inicioVentana / finVentana: hora local del celular (El Salvador).
    public async Task<bool> IniciarAsync(string idAsignacion, DateTime inicioVentana, DateTime finVentana)
    {
        if (!CompartirActivado) { Debug.WriteLine("[RASTREO] X. No arranca: CompartirActivado=false"); return false; }
        if (string.IsNullOrWhiteSpace(idAsignacion)) { Debug.WriteLine("[RASTREO] X. No arranca: idAsignacion vacío"); return false; }
        if (DateTime.Now > finVentana) { Debug.WriteLine("[RASTREO] X. No arranca: la ventana del viaje ya terminó"); return false; }
        if (EstaCompartiendo && IdAsignacionActiva == idAsignacion) { Debug.WriteLine("[RASTREO] Ya estaba compartiendo"); return true; }

        Detener();

        bool permiso = await PedirPermisoUbicacionAsync();
        Debug.WriteLine($"[RASTREO] 5. Permiso de ubicación concedido: {permiso}");
        if (!permiso)
        {
            AvisarProblema(PassGold.Helpers.AppResources.LiveTrack_NeedPermission);
            return false;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        IdAsignacionActiva = idAsignacion;
        EstaCompartiendo = true;
        MainThread.BeginInvokeOnMainThread(() => EstadoCambio?.Invoke(true));

#if ANDROID
        // 🆕 Notificación fija "Compartiendo tu ubicación": con ella Android deja que el
        // GPS siga funcionando aunque el chofer apague o bloquee la pantalla.
        // Se arranca AQUÍ (con la app abierta), porque Android no deja arrancarla
        // después desde segundo plano.
        PassGold.Droid.ServicioUbicacionChofer.Iniciar(
            PassGold.Helpers.AppResources.LiveTrack_ServiceTitle,
            PassGold.Helpers.AppResources.F(PassGold.Helpers.AppResources.LiveTrack_ServiceText,
                finVentana.ToString("hh:mm tt", PassGold.Helpers.AppResources.Culture)));
#endif

        _ = Task.Run(() => BucleAsync(idAsignacion, inicioVentana, finVentana, cts.Token));
        return true;
    }

    public void Detener()
    {
        try { _cts?.Cancel(); } catch { }
        _cts = null;

        bool estabaCompartiendo = EstaCompartiendo;
        EstaCompartiendo = false;
        IdAsignacionActiva = null;

#if ANDROID
        // 🆕 Se quita la notificación fija y se apaga el servicio
        PassGold.Droid.ServicioUbicacionChofer.Detener();
#endif

        MainThread.BeginInvokeOnMainThread(() =>
        {
            DeviceDisplay.Current.KeepScreenOn = false;
            if (estabaCompartiendo) EstadoCambio?.Invoke(false);
        });
    }

    private async Task BucleAsync(string idAsignacion, DateTime inicio, DateTime fin, CancellationToken ct)
    {
        try
        {
            // 1) Antes de la hora de inicio: esperar SIN encender el GPS
            if (DateTime.Now < inicio)
                Informar(PassGold.Helpers.AppResources.F(PassGold.Helpers.AppResources.LiveTrack_StatusScheduled, inicio.ToString("hh:mm tt", PassGold.Helpers.AppResources.Culture)));
            while (DateTime.Now < inicio && !ct.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(15), ct);

            // 2) Dentro de la ventana: compartir. Pantalla encendida para que la app
            //    siga viva mientras maneja (fase 1).
#if !ANDROID
            // En iPhone todavía hace falta la pantalla encendida (fase 2 pendiente).
            // En Android ya no: el servicio en primer plano mantiene vivo el GPS.
            MainThread.BeginInvokeOnMainThread(() => DeviceDisplay.Current.KeepScreenOn = true);
#endif
            Informar(PassGold.Helpers.AppResources.LiveTrack_StatusWaitingGps);
            Debug.WriteLine("[RASTREO] 6. Bucle de envío arrancó");

            int sinUbicacion = 0;
            int fallosEnvio = 0;
            bool avisoGpsMostrado = false;
            bool avisoRedMostrado = false;

            while (DateTime.Now <= fin && !ct.IsCancellationRequested)
            {
                Location? loc = null;
                try
                {
                    loc = await ObtenerUbicacionAsync(ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RASTREO] GPS falló: {ex.GetType().Name} - {ex.Message}");
                }

                if (loc == null)
                {
                    Debug.WriteLine("[RASTREO] GPS no devolvió ubicación");
                    sinUbicacion++;
                    Informar(PassGold.Helpers.AppResources.LiveTrack_StatusNoGps);
                    if (sinUbicacion >= 3 && !avisoGpsMostrado)
                    {
                        avisoGpsMostrado = true;
                        AvisarProblema(PassGold.Helpers.AppResources.LiveTrack_GpsError);
                    }
                }
                else
                {
                    sinUbicacion = 0;
                    var (ok, motivo) = await EnviarAsync(idAsignacion, loc, ct);
                    Debug.WriteLine($"[RASTREO] 7. Envío ({loc.Latitude:F5}, {loc.Longitude:F5}) → ok={ok} {motivo}");
                    if (ok)
                    {
                        Informar(PassGold.Helpers.AppResources.F(PassGold.Helpers.AppResources.LiveTrack_StatusSent, DateTime.Now.ToString("HH:mm:ss")));
                        fallosEnvio = 0;
                        avisoRedMostrado = false;
                    }
                    else
                    {
                        Informar(PassGold.Helpers.AppResources.F(PassGold.Helpers.AppResources.LiveTrack_StatusFailed, motivo ?? "?"));
                        fallosEnvio++;
                        if (fallosEnvio >= 5 && !avisoRedMostrado)
                        {
                            avisoRedMostrado = true;
                            AvisarProblema(PassGold.Helpers.AppResources.LiveTrack_NoServer);
                        }
                    }
                }

                // Si la red falla seguido, espaciamos los intentos para no gastar batería
                int espera = fallosEnvio >= 4 ? 30 : IntervaloSegundos;
                await Task.Delay(TimeSpan.FromSeconds(espera), ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "LiveTrackingService.BucleAsync");
        }
        finally
        {
            // Terminó la ventana (o hubo un error): apagar. Si alguien ya llamó a
            // Detener() / inició otra sesión, no tocamos nada.
            if (_cts != null && _cts.Token == ct) Detener();
        }
    }

    private static Task<Location?> ObtenerUbicacionAsync(CancellationToken ct)
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
        // Se pide desde el hilo principal (así lo exige Android en algunos equipos)
        return MainThread.InvokeOnMainThreadAsync(() => Geolocation.Default.GetLocationAsync(request, ct));
    }

    // Devuelve (ok, motivo): el motivo se muestra en pantalla cuando falla.
    private async Task<(bool ok, string? motivo)> EnviarAsync(string idAsignacion, Location loc, CancellationToken ct)
    {
        try
        {
            for (int intento = 0; intento < 2; intento++)
            {
                string? token = ObtenerToken();
                if (string.IsNullOrEmpty(token))
                {
                    await SupabaseService.Instance.IsSessionActiveAsync();
                    token = ObtenerToken();
                    if (string.IsNullOrEmpty(token))
                        return (false, PassGold.Helpers.AppResources.LiveTrack_StatusNoSession);
                }

                var payload = new JObject
                {
                    ["id_asignacion"] = idAsignacion,
                    ["latitud"] = loc.Latitude,
                    ["longitud"] = loc.Longitude
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, FunctionUrl);
                request.Headers.Add("apikey", SupabaseAnonKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode) return (true, null);

                if ((int)response.StatusCode == 401 && intento == 0)
                {
                    // El token venció: se repone la sesión y se reintenta una vez
                    await SupabaseService.Instance.IsSessionActiveAsync();
                    continue;
                }

                string cuerpo = await response.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"rastreo-chofer {(int)response.StatusCode}: {cuerpo}");
#if DEBUG
                string detalle = cuerpo.Length > 120 ? cuerpo[..120] : cuerpo;
                return (false, $"HTTP {(int)response.StatusCode} {detalle}");
#else
                return (false, $"HTTP {(int)response.StatusCode}");
#endif
            }
            return (false, "HTTP 401");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Debug.WriteLine($"Envío de ubicación falló: {ex.Message}");
            return (false, ex.GetType().Name);
        }
    }

    private static string? ObtenerToken()
    {
        return SupabaseService.Instance.Client?.Auth.CurrentSession?.AccessToken
               ?? SupabaseService.Instance.CurrentSession?.AccessToken;
    }

    private static async Task<bool> PedirPermisoUbicacionAsync()
    {
        try
        {
            var status = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var s = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (s != PermissionStatus.Granted)
                    s = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                return s;
            });
            return status == PermissionStatus.Granted;
        }
        catch { return false; }
    }

    private void AvisarProblema(string mensaje)
    {
        MainThread.BeginInvokeOnMainThread(() => Problema?.Invoke(mensaje));
    }
}