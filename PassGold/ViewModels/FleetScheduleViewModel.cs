using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PassGold.ViewModels;

// Se deja por compatibilidad con otras pantallas que la usen
public class AgrupacionDia : List<OpcionViajePasajero>
{
    public DateTime FechaReal { get; set; }
    public string NombreDia { get; set; } = "";
}

internal static class TemaAgenda
{
    public static bool Oscuro => Application.Current?.RequestedTheme == AppTheme.Dark;
}

// 🆕 Un micro (chofer + unidad) dentro de un horario. En hora pico salen varios
// y el pasajero elige con quién va.
public partial class UnidadOpcion : ObservableObject
{
    public OpcionViajePasajero Opcion { get; init; } = new();
    public string Nombre { get; init; } = "";
    public string Foto { get; init; } = "";
    public bool TieneFoto => !string.IsNullOrWhiteSpace(Foto);
    public string Placa { get; init; } = "";
    public string Detalle => string.IsNullOrWhiteSpace(Placa) ? AppResources.FleetSched_BusGeneric : $"🚐 {Placa}";
    public HorarioChip? Chip { get; set; }
    public ICommand? ElegirCommand { get; set; }

    [ObservableProperty] private bool elegida;
    partial void OnElegidaChanged(bool value) { OnPropertyChanged(nameof(Marca)); OnPropertyChanged(nameof(ColorFondo)); }

    public string Marca => Elegida ? "●" : "○";
    public Color ColorFondo => Elegida ? Color.FromArgb("#33FFC107") : Colors.Transparent;
}

// Un botón de hora en la agenda: junta todas las unidades del mismo viaje a la misma hora
public partial class HorarioChip : ObservableObject
{
    public string Clave { get; init; } = "";          // viaje + día
    public DateTime Fecha { get; init; }
    public bool EsIda { get; init; }
    public string Hora { get; init; } = "";
    public string Detalle { get; init; } = "";
    public List<UnidadOpcion> Unidades { get; init; } = new();
    public bool YaReservado { get; init; }
    public bool YaPaso { get; init; }
    public bool VariasUnidades => Unidades.Count > 1;
    public ICommand? TocarCommand { get; set; }
    public ICommand? TocarEnHorarioCommand { get; set; }   // vista "📅 horario"

    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private UnidadOpcion? unidadElegida;

    partial void OnIsSelectedChanged(bool value) => Refrescar();
    partial void OnUnidadElegidaChanged(UnidadOpcion? value)
    {
        foreach (var u in Unidades) u.Elegida = u == value;
        Refrescar();
    }

    private void Refrescar()
    {
        OnPropertyChanged(nameof(ColorFondo));
        OnPropertyChanged(nameof(ColorBorde));
        OnPropertyChanged(nameof(ColorTexto));
        OnPropertyChanged(nameof(Marca));
        OnPropertyChanged(nameof(MostrarUnidades));
        OnPropertyChanged(nameof(TextoChofer));
        OnPropertyChanged(nameof(TextoCompacto));
    }

    // Con varios micros, al elegir el horario se despliegan para escoger chofer
    public bool MostrarUnidades => IsSelected && VariasUnidades;

    public string TextoChofer =>
        IsSelected && UnidadElegida != null ? AppResources.F(AppResources.FleetSched_With, UnidadElegida.Nombre)
        : Unidades.Count == 1 ? $"👤 {Unidades[0].Nombre}"
        : AppResources.F(AppResources.FleetSched_ManyBuses, Unidades.Count);

    public Color ColorFondo =>
        IsSelected ? Color.FromArgb("#FFC107")
        : YaReservado ? (TemaAgenda.Oscuro ? Color.FromArgb("#1B5E20") : Color.FromArgb("#E8F5E9"))
        : (TemaAgenda.Oscuro ? Color.FromArgb("#2A2A2A") : Colors.White);

    public Color ColorBorde =>
        IsSelected ? Color.FromArgb("#FFC107")
        : YaReservado ? Color.FromArgb("#2E7D32")
        : (TemaAgenda.Oscuro ? Color.FromArgb("#444444") : Color.FromArgb("#DDDDDD"));

    public Color ColorTexto => IsSelected ? Colors.Black : (TemaAgenda.Oscuro ? Colors.White : Colors.Black);
    public string Marca => YaReservado ? "✅" : IsSelected ? "✔" : "";
    public double Opacidad => YaPaso ? 0.35 : 1.0;

    // Texto corto para la celda de la vista "horario": 🏫 llegada / 🏠 salida, ×2 si hay 2 micros
    public string TextoCompacto => $"{(EsIda ? "🏫" : "🏠")}{(VariasUnidades ? $"×{Unidades.Count}" : "")} {Marca}".Trim();
}

// 🆕 Vista "📅 horario" (como el horario de la universidad): una fila por hora,
// una columna por día. Las celdas usan los MISMOS HorarioChip que la vista por día.
public class CeldaHorario
{
    public List<HorarioChip> Chips { get; init; } = new();
    public bool Vacia => Chips.Count == 0;
}

public class FilaHorario
{
    public string Hora { get; init; } = "";
    public List<CeldaHorario> Celdas { get; init; } = new();
    public Color ColorFondo { get; init; } = Colors.Transparent;
}

public partial class DiaAgenda : ObservableObject
{
    public DateTime Fecha { get; init; }
    public string Abrev { get; init; } = "";
    public string Numero { get; init; } = "";
    public string Mes { get; init; } = "";           // solo en el primer día de cada mes / de la lista
    public bool MostrarMes => !string.IsNullOrEmpty(Mes);
    public bool EsPasado { get; init; }
    public bool EsHoy { get; init; }
    public List<HorarioChip> Llegadas { get; init; } = new();
    public List<HorarioChip> Salidas { get; init; } = new();
    public bool SinLlegadas => Llegadas.Count == 0;
    public bool SinSalidas => Salidas.Count == 0;
    public bool SinViajes => SinLlegadas && SinSalidas;
    public ICommand? ElegirCommand { get; set; }

    [ObservableProperty] private bool esActivo;
    [ObservableProperty] private bool tieneSeleccion;

    partial void OnEsActivoChanged(bool value)
    {
        OnPropertyChanged(nameof(ColorFondo));
        OnPropertyChanged(nameof(ColorTexto));
    }

    public Color ColorFondo => EsActivo ? Color.FromArgb("#FFC107") : Colors.Transparent;
    public Color ColorTexto => EsActivo ? Colors.Black : (TemaAgenda.Oscuro ? Colors.White : Colors.Black);
    public double Opacidad => (SinViajes || EsPasado) ? 0.4 : 1.0;
    public string TextoHoy => EsHoy ? AppResources.FleetSched_TodayTag : "";
}

[QueryProperty(nameof(FlotaRecibida), "FlotaParaCalendario")]
public partial class FleetScheduleViewModel : ObservableObject
{
    // Cuántas semanas hacia adelante se muestran (sin tener que cambiar de semana)
    private const int SemanasVisibles = 5;

    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private string tituloAgenda = AppResources.FleetSched_DefaultTitle;
    [ObservableProperty] private bool isPasajero = false;
    [ObservableProperty] private DiaAgenda? diaActivo;
    [ObservableProperty] private string textoBotonContinuar = AppResources.FleetSched_PickYourTimes;
    [ObservableProperty] private bool haySeleccion = false;
    [ObservableProperty] private string tituloDiaActivo = "";
    [ObservableProperty] private string mensajeSinViajes = AppResources.FleetSched_NoTripsDay;

    public ObservableCollection<DiaAgenda> Dias { get; } = new();

    // La selección se mantiene aunque se cambie de día
    private readonly Dictionary<string, HorarioChip> _seleccion = new();
    private bool _cargadoUnaVez = false;
    private DateTime _primerLunes = DateTime.Today;

    // ── Vista "📅 horario" ──
    [ObservableProperty] private bool vistaHorario = false;
    public bool VistaPorDia => !VistaHorario;
    partial void OnVistaHorarioChanged(bool value)
    {
        OnPropertyChanged(nameof(VistaPorDia));
        OnPropertyChanged(nameof(TextoBotonVista));
        if (value) ArmarHorario();
    }
    public string TextoBotonVista => VistaHorario ? AppResources.FleetSched_ViewByDay : AppResources.FleetSched_ViewAsTimetable;

    public ObservableCollection<DiaAgenda> ColumnasHorario { get; } = new();
    public ObservableCollection<FilaHorario> FilasHorario { get; } = new();
    [ObservableProperty] private string textoSemanaHorario = "";
    [ObservableProperty] private bool puedeSemanaAnterior;
    [ObservableProperty] private bool puedeSemanaSiguiente;
    [ObservableProperty] private bool horarioVacio;
    private int _semanaHorario = 0;

    private Usuario? _flotaRecibida;
    public Usuario? FlotaRecibida
    {
        get => _flotaRecibida;
        set
        {
            SetProperty(ref _flotaRecibida, value);
            if (value != null)
            {
                TituloAgenda = string.IsNullOrWhiteSpace(value.NombreFlota) ? AppResources.F(AppResources.FleetSched_TitleOf, value.Nombre) : value.NombreFlota!;
                _ = CargarHorariosDeFlotaAsync(value);
            }
        }
    }

    public FleetScheduleViewModel()
    {
        _ = VerificarRolAsync();
    }

    private async Task VerificarRolAsync()
    {
        try
        {
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser == null || string.IsNullOrEmpty(authUser.Id)) return;
            var data = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (data == null) return;
            string rol = data.Rol.ToLower();
            MainThread.BeginInvokeOnMainThread(() => IsPasajero = rol.Contains("pasajero") || rol.Contains("passenger"));
        }
        catch { }
    }

    // Al volver de confirmar la reserva: refrescar para que aparezcan los ✅
    public void RecargarSiHaceFalta()
    {
        if (_cargadoUnaVez && FlotaRecibida != null) _ = CargarHorariosDeFlotaAsync(FlotaRecibida);
    }

    private async Task CargarHorariosDeFlotaAsync(Usuario flota)
    {
        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);
        try
        {
            var cultura = AppResources.Culture;
            DateTime hoy = DateTime.Today;
            DateTime lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            _primerLunes = lunes;
            DateTime ultimo = lunes.AddDays(SemanasVisibles * 7 - 1);

            var choferes = await _supabaseService.GetEmployeesByBossAsync(flota.Id);
            var idsFlota = choferes.Select(c => c.Id).ToList();
            idsFlota.Add(flota.Id);

            // 🔧 Una sola consulta para las 5 semanas. Antes había que cambiar de semana con
            // ◀ ▶ (y eso no funcionaba): el pasajero no podía elegir días de la semana siguiente.
            var asignaciones = await _supabaseService.GetAsignacionesPorRangoAsync(lunes, ultimo.AddDays(1).AddSeconds(-1), idsFlota);
            var viajes = await _supabaseService.GetViajesByIdsAsync(asignaciones.Select(a => a.IdViaje).Distinct().ToList());

            var vehiculos = new List<Vehiculo>();
            try { vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(flota.Id); } catch { }

            // Lo que YO ya tengo reservado (✅) y con qué choferes suelo viajar
            var yaReservadas = new HashSet<string>();
            var yo = _supabaseService.GetCurrentUser();
            if (yo != null && !string.IsNullOrEmpty(yo.Id))
            {
                var mias = await _supabaseService.GetReservasPorPasajeroYRangoAsync(yo.Id, lunes, ultimo);
                foreach (var r in mias.Where(r => r.Estado != "cancelado")) yaReservadas.Add(r.IdAsignacion);
            }
            var choferesHabituales = new HashSet<string>(asignaciones.Where(a => yaReservadas.Contains(a.Id)).Select(a => a.IdChofer));

            var opciones = new List<OpcionViajePasajero>();
            foreach (var asig in asignaciones)
            {
                var viaje = viajes.FirstOrDefault(v => v.Id == asig.IdViaje);
                if (viaje == null || (viaje.Estado != "activo" && viaje.Estado != "programado")) continue;

                var chofer = asig.IdChofer == flota.Id ? flota : choferes.FirstOrDefault(c => c.Id == asig.IdChofer);
                var micro = vehiculos.FirstOrDefault(v => v.Id == asig.IdVehiculo);
                opciones.Add(new OpcionViajePasajero
                {
                    IdAsignacion = asig.Id,
                    IdViajeBase = viaje.Id,
                    IdChofer = asig.IdChofer,
                    FechaAsignacion = asig.Fecha.AHoraElSalvador(),
                    DestinoOOrigen = string.IsNullOrWhiteSpace(viaje.MetaTexto) ? "" : viaje.MetaTexto!,
                    EsIda = AppResources.IsInboundTrip(viaje.TipoViaje),
                    FotoChofer = chofer?.FotoPerfil ?? "",
                    NombreChofer = chofer?.Nombre ?? AppResources.FleetSched_DriverDefault,
                    ChoferInfo = micro?.Placa ?? "",
                    YaReservado = yaReservadas.Contains(asig.Id)
                });
            }

            DateTime ahora = DateTime.Now;
            var dias = new List<DiaAgenda>();
            int mesAnterior = -1;
            for (DateTime fecha = lunes; fecha <= ultimo; fecha = fecha.AddDays(1))
            {
                var delDia = opciones.Where(o => o.FechaAsignacion.Date == fecha).ToList();
                if (fecha.DayOfWeek == DayOfWeek.Sunday && delDia.Count == 0) continue; // domingo solo si hay viajes

                var chips = delDia
                    .GroupBy(o => o.IdViajeBase)
                    .Select(g => CrearChip(g.Key, fecha, g.OrderBy(o => o.FechaAsignacion).ToList(), viajes, choferesHabituales, ahora, cultura))
                    .OrderBy(c => c.Fecha.TimeOfDay)
                    .ToList();

                // Lo que quedó seleccionado pero ya se reservó o ya pasó, se suelta
                foreach (var c in chips.Where(c => c.IsSelected && (c.YaReservado || c.YaPaso)))
                {
                    c.IsSelected = false;
                    _seleccion.Remove(c.Clave);
                }
                foreach (var c in chips.Where(c => c.IsSelected)) _seleccion[c.Clave] = c;

                string mes = fecha.Month != mesAnterior ? Capitalizar(fecha.ToString("MMM", cultura).TrimEnd('.')) : "";
                mesAnterior = fecha.Month;

                var dia = new DiaAgenda
                {
                    Fecha = fecha,
                    Abrev = Capitalizar(fecha.ToString("ddd", cultura).TrimEnd('.')),
                    Numero = fecha.Day.ToString(),
                    Mes = mes,
                    EsPasado = fecha < hoy,
                    EsHoy = fecha == hoy,
                    Llegadas = chips.Where(c => c.EsIda).ToList(),
                    Salidas = chips.Where(c => !c.EsIda).ToList()
                };
                dia.ElegirCommand = ElegirDiaCommand;
                dia.TieneSeleccion = chips.Any(c => c.IsSelected);
                dias.Add(dia);
            }

            DateTime? anterior = DiaActivo?.Fecha;
            var activo = dias.FirstOrDefault(d => anterior.HasValue && d.Fecha == anterior.Value)
                      ?? dias.FirstOrDefault(d => d.Fecha >= hoy && !d.SinViajes)
                      ?? dias.FirstOrDefault(d => d.Fecha == hoy)
                      ?? dias.FirstOrDefault();

            bool todoVacio = dias.Where(d => !d.EsPasado).All(d => d.SinViajes);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Dias.Clear();
                foreach (var d in dias) Dias.Add(d);
                MensajeSinViajes = todoVacio
                    ? AppResources.FleetSched_NoSchedulesYet
                    : AppResources.FleetSched_NoTripsDay;
                if (activo != null) ElegirDia(activo);
                ActualizarBoton();
                _cargadoUnaVez = true;
                if (VistaHorario) ArmarHorario();
            });
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "FleetScheduleViewModel.CargarHorariosDeFlotaAsync");
            MainThread.BeginInvokeOnMainThread(() => MensajeSinViajes = AppResources.FleetSched_LoadError);
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    private HorarioChip CrearChip(string idViaje, DateTime fecha, List<OpcionViajePasajero> unidades, List<Viaje> viajes,
                                  HashSet<string> choferesHabituales, DateTime ahora, CultureInfo cultura)
    {
        var primera = unidades.First();
        var viaje = viajes.First(v => v.Id == idViaje);
        string clave = $"{idViaje}|{fecha:yyyyMMdd}";

        var detalle = new List<string>();
        if (primera.EsIda && viaje.HoraLlegadaDestino.HasValue)
            detalle.Add(AppResources.F(AppResources.FleetSched_Arrives, DateTime.Today.Add(viaje.HoraLlegadaDestino.Value).ToString("h:mm tt", cultura)));
        if (!string.IsNullOrWhiteSpace(primera.DestinoOOrigen)) detalle.Add(primera.DestinoOOrigen);

        var opciones = unidades.Select(u => new UnidadOpcion
        {
            Opcion = u,
            Nombre = u.NombreChofer,
            Foto = u.FotoChofer,
            Placa = u.ChoferInfo
        }).ToList();

        // Si ya estaba elegido antes (al recargar), se conserva el mismo chofer
        _seleccion.TryGetValue(clave, out var previo);
        string? idChoferPrevio = previo?.UnidadElegida?.Opcion.IdChofer;

        var chip = new HorarioChip
        {
            Clave = clave,
            Fecha = primera.FechaAsignacion,
            EsIda = primera.EsIda,
            Hora = primera.FechaAsignacion.ToString("h:mm tt", cultura),
            Detalle = string.Join(" · ", detalle),
            Unidades = opciones,
            YaReservado = unidades.Any(u => u.YaReservado),
            YaPaso = primera.FechaAsignacion < ahora,
            IsSelected = previo != null
        };
        chip.TocarCommand = TocarHorarioCommand;
        chip.TocarEnHorarioCommand = TocarEnHorarioCommand;
        foreach (var o in opciones) { o.Chip = chip; o.ElegirCommand = ElegirUnidadCommand; }

        if (chip.IsSelected)
            chip.UnidadElegida = opciones.FirstOrDefault(o => o.Opcion.IdChofer == idChoferPrevio) ?? UnidadPorDefecto(opciones, choferesHabituales);
        _ultimosHabituales = choferesHabituales;
        return chip;
    }

    private HashSet<string> _ultimosHabituales = new();

    // Por defecto: el chofer con el que ya viaja; si no, el primero
    private static UnidadOpcion UnidadPorDefecto(List<UnidadOpcion> opciones, HashSet<string> habituales) =>
        opciones.FirstOrDefault(o => habituales.Contains(o.Opcion.IdChofer)) ?? opciones.First();

    private static string Capitalizar(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    // ───────────────────────── Vista "📅 horario" ─────────────────────────

    [RelayCommand]
    public void CambiarVista()
    {
        if (!VistaHorario && DiaActivo != null)
            _semanaHorario = Math.Max(0, (int)((DiaActivo.Fecha - _primerLunes).TotalDays / 7));
        VistaHorario = !VistaHorario;
    }

    [RelayCommand]
    public void SemanaHorarioAnterior() { if (_semanaHorario > 0) { _semanaHorario--; ArmarHorario(); } }

    [RelayCommand]
    public void SemanaHorarioSiguiente() { if (_semanaHorario < SemanasVisibles - 1) { _semanaHorario++; ArmarHorario(); } }

    private void ArmarHorario()
    {
        var cultura = AppResources.Culture;
        DateTime lunes = _primerLunes.AddDays(7 * _semanaHorario);
        DateTime sabado = lunes.AddDays(5);
        var dias = Dias.Where(d => d.Fecha >= lunes && d.Fecha <= lunes.AddDays(6)).ToList();

        var horas = dias.SelectMany(d => d.Llegadas.Concat(d.Salidas))
                        .Select(c => c.Fecha.TimeOfDay).Distinct().OrderBy(h => h).ToList();

        bool oscuro = TemaAgenda.Oscuro;
        var filas = horas.Select((h, i) => new FilaHorario
        {
            Hora = DateTime.Today.Add(h).ToString("h:mm tt", cultura),
            ColorFondo = i % 2 == 0 ? Colors.Transparent : (oscuro ? Color.FromArgb("#1AFFFFFF") : Color.FromArgb("#0D000000")),
            Celdas = dias.Select(d => new CeldaHorario
            {
                Chips = d.Llegadas.Concat(d.Salidas).Where(c => c.Fecha.TimeOfDay == h).OrderBy(c => c.EsIda ? 0 : 1).ToList()
            }).ToList()
        }).ToList();

        ColumnasHorario.Clear();
        foreach (var d in dias) ColumnasHorario.Add(d);
        FilasHorario.Clear();
        foreach (var f in filas) FilasHorario.Add(f);

        TextoSemanaHorario = AppResources.F(AppResources.FleetSched_WeekRange, lunes.ToString(AppResources.Fmt_DayNumMonth, cultura), sabado.ToString(AppResources.Fmt_DayNumMonth, cultura));
        PuedeSemanaAnterior = _semanaHorario > 0;
        PuedeSemanaSiguiente = _semanaHorario < SemanasVisibles - 1;
        HorarioVacio = filas.Count == 0;
    }

    // En el horario las celdas son chiquitas: si a esa hora salen varios micros,
    // se pregunta con qué chofer ir antes de marcarlo.
    [RelayCommand]
    public async Task TocarEnHorarioAsync(HorarioChip? chip)
    {
        if (chip == null || Shell.Current == null) return;

        if (IsPasajero && chip.VariasUnidades && !chip.IsSelected && !chip.YaReservado && !chip.YaPaso)
        {
            var textos = chip.Unidades.Select(u => $"👤 {u.Nombre} · {u.Detalle}").ToArray();
            string titulo = $"{(chip.EsIda ? AppResources.FleetSched_ArriveAtU : AppResources.FleetSched_LeaveU)} · {chip.Fecha.ToString(AppResources.Fmt_DayAbbrevNum, AppResources.Culture)} {chip.Hora}\n{AppResources.FleetSched_WhoWith}";
            string? elegido = await Shell.Current.DisplayActionSheet(titulo, AppResources.Global_Cancel, null, textos);
            int i = Array.IndexOf(textos, elegido);
            if (i < 0) return;
            chip.UnidadElegida = chip.Unidades[i];
        }

        await TocarHorarioAsync(chip);
    }

    [RelayCommand]
    public void ElegirDia(DiaAgenda? dia)
    {
        if (dia == null) return;
        foreach (var d in Dias) d.EsActivo = d == dia;
        DiaActivo = dia;
        var cultura = AppResources.Culture;
        string nombre = Capitalizar(dia.Fecha.ToString(AppResources.Fmt_DayMonthLong, cultura));
        TituloDiaActivo = dia.EsHoy ? AppResources.F(AppResources.FleetSched_TodayPrefix, nombre) : dia.EsPasado ? AppResources.F(AppResources.FleetSched_PastSuffix, nombre) : nombre;
    }

    [RelayCommand]
    public async Task TocarHorarioAsync(HorarioChip? chip)
    {
        if (chip == null || Shell.Current == null) return;

        // Chofer o jefe mirando: solo información
        if (!IsPasajero)
        {
            string quienes = string.Join("\n", chip.Unidades.Select(u => $"👤 {u.Nombre}  {u.Detalle}"));
            await Shell.Current.DisplayAlert($"{(chip.EsIda ? AppResources.FleetSched_ArrivalAtU : AppResources.FleetSched_DepartureFromU)} · {chip.Hora}", quienes, AppResources.Global_Ok);
            return;
        }

        if (chip.YaReservado)
        {
            await Shell.Current.DisplayAlert(AppResources.FleetSched_AlreadyBookedTitle, AppResources.FleetSched_AlreadyBookedMsg, AppResources.Global_Ok);
            return;
        }
        if (chip.YaPaso)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.FleetSched_TripAlreadyLeft, AppResources.Global_Ok);
            return;
        }

        chip.IsSelected = !chip.IsSelected;
        if (chip.IsSelected)
        {
            chip.UnidadElegida ??= UnidadPorDefecto(chip.Unidades, _ultimosHabituales);
            _seleccion[chip.Clave] = chip;
        }
        else
        {
            _seleccion.Remove(chip.Clave);
        }

        var dia = Dias.FirstOrDefault(d => d.Llegadas.Contains(chip) || d.Salidas.Contains(chip));
        if (dia != null) dia.TieneSeleccion = dia.Llegadas.Any(c => c.IsSelected) || dia.Salidas.Any(c => c.IsSelected);
        ActualizarBoton();
    }

    [RelayCommand]
    public void ElegirUnidad(UnidadOpcion? unidad)
    {
        if (unidad?.Chip == null) return;
        unidad.Chip.UnidadElegida = unidad;
    }

    private void ActualizarBoton()
    {
        int n = _seleccion.Count;
        HaySeleccion = n > 0;
        TextoBotonContinuar = n switch
        {
            0 => AppResources.FleetSched_PickYourTimes,
            1 => AppResources.FleetSched_ContinueOne,
            _ => AppResources.F(AppResources.FleetSched_ContinueMany, n)
        };
    }

    [RelayCommand]
    public async Task IrAConfirmarReservaAsync()
    {
        if (Shell.Current == null || !IsPasajero) return;
        if (_seleccion.Count == 0)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.FleetSched_TapAtLeastOne, AppResources.Global_Ok);
            return;
        }

        // De cada horario se reserva el micro que eligió el pasajero
        var elegidos = _seleccion.Values
            .OrderBy(c => c.Fecha)
            .Select(c => (c.UnidadElegida ?? c.Unidades.First()).Opcion)
            .ToList();

        await Shell.Current.Navigation.PushAsync(new PassGold.Views.ConfirmarReservaPage(elegidos));
        // Al volver, OnAppearing recarga: lo reservado aparece con ✅ y se suelta solo
    }
}