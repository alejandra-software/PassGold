using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace PassGold.Droid;

// =====================================================================
// 🆕 SERVICIO EN PRIMER PLANO DEL CHOFER
//
// Mientras dura un viaje, este servicio muestra una notificación fija
// ("Compartiendo tu ubicación con tus pasajeros") y le dice a Android que
// la app está trabajando para el usuario. Gracias a eso:
//   · el GPS sigue funcionando aunque el chofer APAGUE O BLOQUEE la pantalla,
//   · Android no duerme la app ni le corta internet a los pocos minutos.
//
// El envío de la ubicación lo sigue haciendo LiveTrackingService (no cambió);
// este servicio solo mantiene la app despierta. Se apaga solo al terminar
// el horario del viaje o al tocar "Finalizar".
//
// Requisitos en AndroidManifest.xml: FOREGROUND_SERVICE,
// FOREGROUND_SERVICE_LOCATION y WAKE_LOCK.
// =====================================================================
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class ServicioUbicacionChofer : Service
{
    private const int IdNotificacion = 4701;
    private const string IdCanal = "passgold_viaje_en_curso";
    private const string ExtraTexto = "texto";

    private PowerManager.WakeLock? _wakeLock;

    // ── Lo llama LiveTrackingService (con la app abierta en pantalla) ──
    public static void Iniciar(string titulo, string texto)
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            var intent = new Intent(ctx, typeof(ServicioUbicacionChofer));
            intent.PutExtra("titulo", titulo);
            intent.PutExtra(ExtraTexto, texto);
            ContextCompat.StartForegroundService(ctx, intent);
        }
        catch (Exception ex)
        {
            // Si Android no lo deja arrancar, el rastreo sigue funcionando
            // como antes (solo con la pantalla encendida).
            PassGold.Services.CrashLogger.Guardar(ex, "ServicioUbicacionChofer.Iniciar");
        }
    }

    public static void Detener()
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            ctx.StopService(new Intent(ctx, typeof(ServicioUbicacionChofer)));
        }
        catch { }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        string titulo = intent?.GetStringExtra("titulo") ?? "PassGold";
        string texto = intent?.GetStringExtra(ExtraTexto) ?? "";

        CrearCanal();
        var notificacion = CrearNotificacion(titulo, texto);

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(IdNotificacion, notificacion, ForegroundService.TypeLocation);
            else
                StartForeground(IdNotificacion, notificacion);
        }
        catch (Exception ex)
        {
            PassGold.Services.CrashLogger.Guardar(ex, "ServicioUbicacionChofer.StartForeground");
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        // Que el procesador no se duerma con la pantalla apagada (máximo 4 h por seguridad)
        if (_wakeLock == null)
        {
            var pm = (PowerManager?)GetSystemService(PowerService);
            _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "PassGold:ViajeEnCurso");
            _wakeLock?.Acquire(4 * 60 * 60 * 1000L);
        }

        // Si Android cierra la app, NO se reinicia sola: el chofer la vuelve a abrir.
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        try { if (_wakeLock?.IsHeld == true) _wakeLock.Release(); } catch { }
        _wakeLock = null;

        StopForeground(StopForegroundFlags.Remove);

        base.OnDestroy();
    }

    private void CrearCanal()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager == null || manager.GetNotificationChannel(IdCanal) != null) return;

        bool es = PassGold.Helpers.AppResources.IsSpanish;
        var canal = new NotificationChannel(IdCanal,
            es ? "Viaje en curso" : "Trip in progress",
            NotificationImportance.Low) // sin sonido: solo se queda visible
        {
            Description = es
                ? "Aparece mientras compartes tu ubicación con tus pasajeros."
                : "Shown while you share your location with your passengers."
        };
        canal.SetShowBadge(false);
        manager.CreateNotificationChannel(canal);
    }

    private Notification CrearNotificacion(string titulo, string texto)
    {
        // Tocar la notificación abre la app
        var abrir = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "") ?? new Intent(this, typeof(MainActivity));
        abrir.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pending = PendingIntent.GetActivity(this, 0, abrir,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, IdCanal)
            .SetContentTitle(titulo)
            .SetContentText(texto)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(texto))
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation) // ícono de ubicación del sistema
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetCategory(NotificationCompat.CategoryNavigation)
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate)
            .SetContentIntent(pending)
            .Build()!;
    }
}