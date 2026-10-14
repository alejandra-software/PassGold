using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Helpers;
using PassGold.Models;
using PassGold.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;

namespace PassGold.ViewModels;

public partial class MapaRastreoViewModel : ObservableObject, IQueryAttributable
{
    private const int SegundosEntreConsultas = 6;
    // Más viejo que esto = el chofer dejó de enviar (túnel, sin datos, apagó el GPS...)
    private const int SegundosMaximosParaConsiderarVigente = 45;

    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private IDispatcherTimer? _timer;
    private string _idAsignacionActiva = "";
    private bool _consultando = false;

    [ObservableProperty] private bool isLoading = true;
    [ObservableProperty] private bool hasViajeActivo = false;
    [ObservableProperty] private string choferNombre = AppResources.Global_Loading;
    [ObservableProperty] private Location? posicionChofer;
    [ObservableProperty] private string estadoTexto = AppResources.Track_WaitingSignal;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Llega acá desde la notificación push (id_asignacion) o desde el botón
        // "Rastrear Unidad" del menú.
        if (query.TryGetValue("IdAsignacionRastreo", out var value)
            && value is string idAsig && !string.IsNullOrWhiteSpace(idAsig))
        {
            _idAsignacionActiva = idAsig;
            HasViajeActivo = true;
        }
    }

    [RelayCommand]
    public async Task InicializarMapaAsync()
    {
        IsLoading = true;
        try
        {
            // Si entramos desde el menú lateral (sin ID previo), buscamos si hay un viaje activo AHORA
            if (string.IsNullOrEmpty(_idAsignacionActiva))
                await BuscarViajeEnCursoAsync();

            if (HasViajeActivo && !string.IsNullOrEmpty(_idAsignacionActiva))
            {
                await CargarNombreChoferAsync();
                IniciarRastreo();
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "MapaRastreoViewModel.InicializarMapaAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuscarViajeEnCursoAsync()
    {
        try
        {
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser == null) return;

            DateTime hoy = DateTime.Today;

            var misReservas = await _supabaseService.GetReservasPorPasajeroYRangoAsync(authUser.Id, hoy.AddDays(-1), hoy.AddDays(1));
            var reservasActivas = misReservas.Where(r => r.Estado != "cancelado").ToList();
            if (reservasActivas.Count == 0) return;

            var idsAsignaciones = reservasActivas.Select(r => r.IdAsignacion).Distinct().ToList();
            var asignaciones = await _supabaseService.GetAsignacionesByIdsAsync(idsAsignaciones);
            var viajes = await _supabaseService.GetViajesByIdsAsync(asignaciones.Select(a => a.IdViaje).ToList());

            TimeSpan horaActual = DateTime.Now.TimeOfDay;

            // Misma regla de zona horaria que el resto de la app: siempre .AHoraElSalvador()
            foreach (var asig in asignaciones.Where(a => a.Fecha.AHoraElSalvador().Date == hoy))
            {
                var viaje = viajes.FirstOrDefault(v => v.Id == asig.IdViaje);
                if (viaje == null) continue;

                TimeSpan horaInicio = viaje.HoraInicioRecorrido ?? viaje.HoraSalida.AHoraElSalvador().TimeOfDay;
                TimeSpan horaFin = viaje.HoraLlegadaDestino ?? horaInicio.Add(TimeSpan.FromHours(2));

                // Un margen de 10 min a cada lado, por si el chofer arranca antes o llega después
                if (horaActual >= horaInicio.Subtract(TimeSpan.FromMinutes(10))
                    && horaActual <= horaFin.Add(TimeSpan.FromMinutes(10)))
                {
                    _idAsignacionActiva = asig.Id;
                    HasViajeActivo = true;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "MapaRastreoViewModel.BuscarViajeEnCursoAsync");
        }
    }

    // Antes el nombre solo se cargaba al entrar por el menú; al venir desde la
    // notificación se quedaba en "Cargando..." para siempre.
    private async Task CargarNombreChoferAsync()
    {
        try
        {
            var asig = await _supabaseService.GetAsignacionByIdAsync(_idAsignacionActiva);
            if (asig == null) return;

            var chofer = await _supabaseService.GetUserDataAsync(asig.IdChofer);
            if (chofer != null) ChoferNombre = chofer.Nombre;
        }
        catch { }
    }

    private void IniciarRastreo()
    {
        // Antes se creaba un temporizador NUEVO cada vez que se abría la pantalla y
        // solo se detenía el último — los anteriores seguían corriendo para siempre.
        DetenerRastreo();

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(SegundosEntreConsultas);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // Primera consulta inmediata, sin esperar al primer tick
        _ = ActualizarUbicacionChoferAsync();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        await ActualizarUbicacionChoferAsync();
    }

    private async Task ActualizarUbicacionChoferAsync()
    {
        if (_consultando || string.IsNullOrEmpty(_idAsignacionActiva)) return;
        _consultando = true;

        try
        {
            // La base de datos (RLS) solo devuelve la fila si ESTE pasajero tiene una
            // reserva activa en ESA asignación y la posición es reciente. Un pasajero
            // de otro chofer, aunque sea de la misma flota y hora, no recibe nada.
            var ubicaciones = await _supabaseService.GetUbicacionesEnVivoPorAsignacionAsync(_idAsignacionActiva);
            var ultima = ubicaciones.OrderByDescending(u => u.ActualizadoEn).FirstOrDefault();

            if (ultima == null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    EstadoTexto = AppResources.Track_NotSharing);
                return;
            }

            double edad = CalcularEdadSegundos(ultima.ActualizadoEn);
            bool vigente = edad <= SegundosMaximosParaConsiderarVigente;
            string hace = TextoEdad(edad);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PosicionChofer = new Location(ultima.Latitud, ultima.Longitud);
                EstadoTexto = vigente
                    ? AppResources.F(AppResources.Track_OnTheWay, hace)
                    : AppResources.F(AppResources.Track_WeakSignal, hace);
            });
        }
        catch { }
        finally
        {
            _consultando = false;
        }
    }

    // La librería de Supabase a veces entrega la hora ya convertida a la del celular
    // pero etiquetada como UTC (ver ZonaHorariaHelper). Probamos las dos lecturas y nos
    // quedamos con la que da una edad razonable: la equivocada siempre da ~6 horas de más.
    private static double CalcularEdadSegundos(DateTime actualizadoEn)
    {
        double comoUtc = Math.Abs((DateTime.UtcNow - DateTime.SpecifyKind(actualizadoEn, DateTimeKind.Utc)).TotalSeconds);
        double comoLocal = Math.Abs((DateTime.Now - DateTime.SpecifyKind(actualizadoEn, DateTimeKind.Local)).TotalSeconds);
        return Math.Min(comoUtc, comoLocal);
    }

    private static string TextoEdad(double segundos)
    {
        if (segundos < 60) return AppResources.F(AppResources.Track_SecondsAgo, Math.Max(1, (int)segundos));
        return AppResources.F(AppResources.Track_MinutesAgo, (int)(segundos / 60));
    }

    public void DetenerRastreo()
    {
        if (_timer == null) return;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
        _timer = null;
    }
}