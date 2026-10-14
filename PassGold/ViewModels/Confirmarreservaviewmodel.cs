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
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Maps;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Operador = Supabase.Postgrest.Constants.Operator;

namespace PassGold.ViewModels;

// Un lugar que el pasajero ya usó o guardó (para no buscarlo a mano cada vez)
public class LugarFrecuente
{
    public string Nombre { get; set; } = "";
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public DateTime UltimoUso { get; set; }
    // true = lado de la casa (recogida en ida / bajada en regreso)
    // false = punto alterno (bajada en ida / recogida en regreso), ej. Metrocentro
    public bool EsCasa { get; set; }
    public string Texto => EsCasa ? $"🏠 {Nombre}" : $"📍 {Nombre}";
}

// Una opción de punto en la universidad (meta o parada oficial del chofer)
public class PuntoOficial
{
    public string Nombre { get; set; } = "";
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Texto => $"🏁 {Nombre}";
}

// 🔧 REDISEÑO "ESPEJO": el pasajero marca DOS lugares y la app los pone al derecho
// o al revés según la dirección del viaje:
//   🏠 su casa (ej. Colonia San Rafael)
//   🚩 el punto oficial del chofer o uno alterno (ej. Metrocentro)
//   IDA:     🟢 recoge en San Rafael  →  🔵 deja en Metrocentro
//   REGRESO: 🟢 recoge en Metrocentro →  🔵 deja en San Rafael
// Así se pueden reservar llegadas y salidas juntas en un solo paso.
public partial class ConfirmarReservaViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;
    private readonly List<OpcionViajePasajero> _selecciones;

    // Compatibilidad con el resto de la app
    public bool EsIda { get; }
    public bool EsRegreso => !EsIda;

    [ObservableProperty] private bool isLoading = true;
    [ObservableProperty] private bool isSaving = false;
    [ObservableProperty] private bool isMapExpanded = false;
    [ObservableProperty] private bool showDetailsPanel = true;
    [ObservableProperty] private MapType currentMapType = MapType.Street;

    [RelayCommand]
    public void ToggleMapExpansion() { IsMapExpanded = !IsMapExpanded; ShowDetailsPanel = !IsMapExpanded; }

    [RelayCommand]
    public void ToggleMapType() => CurrentMapType = CurrentMapType == MapType.Street ? MapType.Hybrid : MapType.Street;

    // ── Qué se está editando: casa o universidad ──
    [ObservableProperty] private bool editandoCasa = true;
    public bool EditandoUniversidad => !EditandoCasa;

    partial void OnEditandoCasaChanged(bool value)
    {
        OnPropertyChanged(nameof(EditandoUniversidad));
        OnPropertyChanged(nameof(TextoAyudaMapa));
        OnPropertyChanged(nameof(TextoAyudaLugares));
        MostrarLugaresDelLado();
        RedibujarPines();
    }

    [RelayCommand] public void VerCasa() => EditandoCasa = true;
    [RelayCommand] public void VerUniversidad() => EditandoCasa = false;

    public string TextoAyudaMapa => EditandoCasa
        ? AppResources.Confirm_MapHintHome
        : AppResources.Confirm_MapHintAlt;

    public string TextoAyudaLugares => EditandoCasa
        ? AppResources.Confirm_PlacesHome
        : AppResources.Confirm_PlacesAlt;

    // ── Resumen de lo que se va a reservar ──
    [ObservableProperty] private string resumenSeleccion = "";

    // ── Casa / punto personal ──
    [ObservableProperty] private double? latCasa;
    [ObservableProperty] private double? lngCasa;
    [ObservableProperty] private bool casaEsNueva = false;
    // Referencia para el chofer ("Portón negro frente a la tienda")
    [ObservableProperty] private string aliasUbicacionPersonal = "";

    // ── Punto en la universidad ──
    [ObservableProperty] private string modoAlterno = "MetaOficial";
    [ObservableProperty] private double? latAlterno;
    [ObservableProperty] private double? lngAlterno;
    [ObservableProperty] private string textoAlterno = "";
    [ObservableProperty] private bool guardarNuevoAlternoEnHistorial = true;

    public string TabCasaTexto => LatCasa.HasValue ? AppResources.Confirm_TabHome + " ✓" : AppResources.Confirm_TabHome;
    public string TabUniversidadTexto => LatAlterno.HasValue ? AppResources.Confirm_TabAlt + " ✓" : AppResources.Confirm_TabAlt;
    partial void OnLatCasaChanged(double? value) { OnPropertyChanged(nameof(TabCasaTexto)); ActualizarEspejo(); }
    partial void OnLatAlternoChanged(double? value) { OnPropertyChanged(nameof(TabUniversidadTexto)); ActualizarEspejo(); }
    partial void OnAliasUbicacionPersonalChanged(string value) => ActualizarEspejo();
    partial void OnTextoAlternoChanged(string value) => ActualizarEspejo();

    // 🆕 Vista previa del "espejo": cómo quedan recogida y bajada en cada dirección
    [ObservableProperty] private string espejoIda = "";
    [ObservableProperty] private string espejoRegreso = "";
    public bool HayIda => _selecciones.Any(s => s.EsIda);
    public bool HayRegreso => _selecciones.Any(s => !s.EsIda);

    private void ActualizarEspejo()
    {
        string casa = LatCasa.HasValue ? (string.IsNullOrWhiteSpace(AliasUbicacionPersonal) ? AppResources.Confirm_MissingAddressText : AliasUbicacionPersonal.Trim()) : AppResources.Confirm_MissingAddress;
        string otro = LatAlterno.HasValue && !string.IsNullOrWhiteSpace(TextoAlterno) ? TextoAlterno.Trim() : AppResources.Confirm_MissingPoint;
        EspejoIda = AppResources.F(AppResources.Confirm_MirrorPickup, casa) + "\n" + AppResources.F(AppResources.Confirm_MirrorDropoff, otro);
        EspejoRegreso = AppResources.F(AppResources.Confirm_MirrorPickup, otro) + "\n" + AppResources.F(AppResources.Confirm_MirrorDropoff, casa);
    }

    public ObservableCollection<PuntoOficial> PuntosOficiales { get; } = new();
    public bool HayPuntosOficiales => PuntosOficiales.Count > 0;

    // 🆕 Historial separado por lado: lo que se muestra depende de la pestaña activa
    public ObservableCollection<LugarFrecuente> MisLugares { get; } = new();
    public bool HayLugares => MisLugares.Count > 0;
    private List<LugarFrecuente> _lugaresCasa = new();
    private List<LugarFrecuente> _lugaresAlternos = new();

    private void MostrarLugaresDelLado()
    {
        MisLugares.Clear();
        foreach (var l in (EditandoCasa ? _lugaresCasa : _lugaresAlternos).Take(10)) MisLugares.Add(l);
    }

    public ObservableCollection<Pin> PinesMapa { get; } = new();

    // ── Repetición semanal ──
    // Textos en el idioma de la app (se comparan por igualdad, nunca se guardan en la BD)
    public static string AlcanceSoloElegidos => AppResources.Confirm_ScopeOnlyChosen;
    public static string AlcanceUnMes => AppResources.Confirm_ScopeOneMonth;
    public static string AlcanceCiclo => AppResources.Confirm_ScopeCycle;
    public static string AlcancePersonalizado => AppResources.Confirm_ScopeCustom;

    public ObservableCollection<string> OpcionesAlcance { get; } = new() { AlcanceSoloElegidos, AlcanceUnMes, AlcanceCiclo, AlcancePersonalizado };
    [ObservableProperty] private string alcanceSeleccionado = AlcanceSoloElegidos;
    [ObservableProperty] private bool usandoRangoPersonalizado = false;
    [ObservableProperty] private DateTime fechaHastaPersonalizada = DateTime.Today.AddDays(28);
    [ObservableProperty] private DateTime fechaMinimaRango = DateTime.Today;
    [ObservableProperty] private DateTime fechaMaximaRango = DateTime.Today.AddMonths(6);

    // 🔧 Antes el selector cambiaba el alcance pero nada mostraba el calendario de
    // "hasta qué fecha": por eso no se podía elegir un rango.
    partial void OnAlcanceSeleccionadoChanged(string value) => UsandoRangoPersonalizado = value == AlcancePersonalizado;

    public ConfirmarReservaViewModel(List<OpcionViajePasajero> seleccionesIniciales)
    {
        _selecciones = seleccionesIniciales ?? new List<OpcionViajePasajero>();
        EsIda = _selecciones.FirstOrDefault()?.EsIda ?? true;
        MisLugares.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HayLugares));
        PuntosOficiales.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HayPuntosOficiales));
        ResumenSeleccion = ArmarResumen(_selecciones);

        Task.Run(CargarDatosAsync);
    }

    private static string ArmarResumen(List<OpcionViajePasajero> sel)
    {
        if (sel.Count == 0) return "";
        var cultura = AppResources.Culture;
        var partes = sel.OrderBy(o => o.FechaAsignacion)
            .Select(o => $"{(o.EsIda ? "🏫" : "🏠")} {o.FechaAsignacion.ToString("ddd d", cultura)} {o.FechaAsignacion.ToString("h:mm tt", cultura)}"
                         + (string.IsNullOrWhiteSpace(o.NombreChofer) ? "" : $" ({o.NombreChofer})"))
            .ToList();
        string encabezado = sel.Count == 1 ? AppResources.Confirm_SummaryOne : AppResources.F(AppResources.Confirm_SummaryMany, sel.Count);
        return $"{encabezado}\n{string.Join("  ·  ", partes)}\n{AppResources.Confirm_SummaryLegend}";
    }

    // ───────────────────────── Carga ─────────────────────────

    private async Task CargarDatosAsync()
    {
        try
        {
            var yo = _supabaseService.GetCurrentUser();
            string miId = yo?.Id ?? "";

            // 1) Puntos oficiales en la universidad (paradas o meta de cada viaje elegido)
            var oficiales = new List<PuntoOficial>();
            foreach (var idViaje in _selecciones.Select(s => s.IdViajeBase).Distinct())
            {
                var paradas = await _supabaseService.GetParadasByViajeAsync(idViaje);
                foreach (var p in paradas.OrderBy(p => p.Orden))
                    if (p.Latitud.HasValue && p.Longitud.HasValue)
                        oficiales.Add(new PuntoOficial { Nombre = p.NombreLugar, Latitud = p.Latitud.Value, Longitud = p.Longitud.Value });

                if (paradas.Count == 0)
                {
                    var viaje = await _supabaseService.GetViajeByIdAsync(idViaje);
                    if (viaje?.MetaLatitud != null && viaje.MetaLongitud != null)
                        oficiales.Add(new PuntoOficial { Nombre = viaje.MetaTexto ?? AppResources.Confirm_OfficialPointDefault, Latitud = viaje.MetaLatitud.Value, Longitud = viaje.MetaLongitud.Value });

                    if (viaje?.FechaFin != null)
                    {
                        var fin = viaje.FechaFin.Value.Date;
                        MainThread.BeginInvokeOnMainThread(() => { if (fin < FechaMaximaRango) FechaMaximaRango = fin < FechaMinimaRango ? FechaMinimaRango : fin; });
                    }
                }
            }
            oficiales = oficiales.GroupBy(o => o.Nombre.Trim().ToLowerInvariant()).Select(g => g.First()).ToList();

            // 2) Historial: lugares guardados + puntos de reservas anteriores
            var lugares = new List<LugarFrecuente>();
            LugarFrecuente? casaSugerida = null;
            LugarFrecuente? uSugerida = null;

            if (!string.IsNullOrEmpty(miId))
            {
                // Tabla de lugares guardados (si existe)
                var guardados = await _supabaseService.GetUbicacionesPasajeroAsync(miId);
                foreach (var g in guardados.Where(g => g.Latitud.HasValue && g.Longitud.HasValue))
                {
                    lugares.Add(new LugarFrecuente
                    {
                        Nombre = string.IsNullOrWhiteSpace(g.Alias) ? g.DireccionTexto : g.Alias,
                        Latitud = g.Latitud!.Value,
                        Longitud = g.Longitud!.Value,
                        UltimoUso = g.CreadoEn,
                        EsCasa = g.Tipo != "alterno"
                    });
                }

                // Reservas anteriores: de acá sale el historial aunque no haya nada guardado
                var misReservas = await _supabaseService.GetReservasPorPasajeroYRangoAsync(miId, DateTime.Today.AddYears(-1), DateTime.Today.AddYears(1));
                var idsAsig = misReservas.Select(r => r.IdAsignacion).Distinct().ToList();
                var asignaciones = idsAsig.Count > 0 ? await _supabaseService.GetAsignacionesByIdsAsync(idsAsig) : new List<Asignacion>();
                var viajes = asignaciones.Count > 0 ? await _supabaseService.GetViajesByIdsAsync(asignaciones.Select(a => a.IdViaje).Distinct().ToList()) : new List<Viaje>();

                foreach (var r in misReservas.OrderByDescending(r => r.CreadoEn))
                {
                    var asig = asignaciones.FirstOrDefault(a => a.Id == r.IdAsignacion);
                    var viaje = asig != null ? viajes.FirstOrDefault(v => v.Id == asig.IdViaje) : null;
                    bool esIda = (viaje == null || AppResources.IsInboundTrip(viaje.TipoViaje));

                    LugarFrecuente? recogida = (r.LatitudRecogida.HasValue && r.LongitudRecogida.HasValue)
                        ? new LugarFrecuente { Nombre = Limpiar(r.PuntoRecogidaTexto), Latitud = r.LatitudRecogida.Value, Longitud = r.LongitudRecogida.Value, UltimoUso = r.CreadoEn }
                        : null;
                    LugarFrecuente? bajada = (r.LatitudBajada.HasValue && r.LongitudBajada.HasValue)
                        ? new LugarFrecuente { Nombre = Limpiar(r.PuntoBajadaTexto), Latitud = r.LatitudBajada.Value, Longitud = r.LongitudBajada.Value, UltimoUso = r.CreadoEn }
                        : null;

                    // Espejo: en la ida la casa es la recogida; en el regreso, la bajada
                    var casa = esIda ? recogida : bajada;
                    var u = esIda ? bajada : recogida;
                    if (casa != null) casa.EsCasa = true;
                    if (u != null) u.EsCasa = false;
                    if (casa != null) { lugares.Add(casa); casaSugerida ??= casa; }
                    if (u != null) { lugares.Add(u); uSugerida ??= u; }
                }
            }

            // Sin repetidos (dos puntos a menos de ~40 m son el mismo lugar), cada lado por separado.
            // De los alternos se quitan los puntos oficiales, que ya tienen sus propios botones.
            List<LugarFrecuente> SinRepetidos(IEnumerable<LugarFrecuente> origen)
            {
                var lista = new List<LugarFrecuente>();
                foreach (var l in origen.Where(l => !string.IsNullOrWhiteSpace(l.Nombre)).OrderByDescending(l => l.UltimoUso))
                    if (!lista.Any(x => DistanciaMetros(x.Latitud, x.Longitud, l.Latitud, l.Longitud) < 40))
                        lista.Add(l);
                return lista;
            }
            var lugaresCasa = SinRepetidos(lugares.Where(l => l.EsCasa));
            var lugaresAlternos = SinRepetidos(lugares.Where(l => !l.EsCasa))
                .Where(l => !oficiales.Any(o => DistanciaMetros(o.Latitud, o.Longitud, l.Latitud, l.Longitud) < 40))
                .ToList();

            var casaGuardada = casaSugerida ?? lugaresCasa.FirstOrDefault();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PuntosOficiales.Clear();
                foreach (var o in oficiales) PuntosOficiales.Add(o);

                _lugaresCasa = lugaresCasa;
                _lugaresAlternos = lugaresAlternos;

                // Casa: la última que usó (así no tiene que buscarla cada vez)
                if (casaGuardada != null)
                {
                    LatCasa = casaGuardada.Latitud; LngCasa = casaGuardada.Longitud;
                    UsarNombreDeCasa(casaGuardada.Nombre);
                    CasaEsNueva = false;
                }
                else
                {
                    CasaEsNueva = true;
                }

                // Punto oficial / alterno: el que usó la última vez (ej. siempre se baja en
                // Metrocentro); si nunca reservó, el punto oficial del chofer.
                var oficialCercano = uSugerida == null ? null : oficiales.FirstOrDefault(o => DistanciaMetros(o.Latitud, o.Longitud, uSugerida.Latitud, uSugerida.Longitud) < 40);
                if (oficialCercano != null) UsarPuntoOficial(oficialCercano);
                else if (uSugerida != null) { ModoAlterno = "Guardada"; LatAlterno = uSugerida.Latitud; LngAlterno = uSugerida.Longitud; TextoAlterno = uSugerida.Nombre; }
                else if (oficiales.Count > 0) UsarPuntoOficial(oficiales[0]);

                // Empezar por lo que falta
                EditandoCasa = !LatCasa.HasValue || LatAlterno.HasValue;
                MostrarLugaresDelLado();
                ActualizarEspejo();
                IsLoading = false;
                RedibujarPines();
            });
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "ConfirmarReservaViewModel.CargarDatosAsync");
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    private static string Limpiar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        return texto.Replace("⭐", "").Replace("🏁", "").Replace("📍", "").Trim();
    }

    private static double DistanciaMetros(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371000, dLat = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ───────────────────────── Elegir lugares ─────────────────────────

    [RelayCommand]
    public void SeleccionarPuntoOficial(PuntoOficial? punto)
    {
        if (punto == null) return;
        UsarPuntoOficial(punto);
        EditandoCasa = false;
        RedibujarPines();
    }

    private void UsarPuntoOficial(PuntoOficial punto)
    {
        ModoAlterno = "MetaOficial";
        LatAlterno = punto.Latitud; LngAlterno = punto.Longitud;
        TextoAlterno = punto.Nombre;
    }

    // ───────────────────────── Dirección clara para el chofer ─────────────────────────

    // 🆕 El chofer ve la lista de TODOS sus pasajeros: si 20 ponen "Mi casa", no
    // sirve de nada. Estas referencias no se aceptan como dirección.
    private static readonly string[] ReferenciasVagas =
    {
        "mi casa", "casa", "mi hogar", "hogar", "aqui", "aca", "mi ubicacion", "ubicacion",
        "ubicacion personal", "mi punto", "punto", "mi direccion", "direccion", "home", "my home",
        "house", "mi lugar", "lugar", "donde siempre", "el mismo", "la misma", "mi cuarto"
    };

    public static bool EsReferenciaVaga(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return true;
        var sinTildes = new string(texto.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        var limpio = new string(sinTildes.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        limpio = string.Join(" ", limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length < 6 || ReferenciasVagas.Contains(limpio);
    }

    private string _nombreCasaOriginal = "";

    // Si el nombre guardado es vago ("Mi Casa"), se reemplaza por la dirección real
    // del punto en el mapa, para que el pasajero solo tenga que completarla.
    private void UsarNombreDeCasa(string nombre)
    {
        if (!EsReferenciaVaga(nombre))
        {
            AliasUbicacionPersonal = nombre;
            _nombreCasaOriginal = nombre;
            return;
        }
        AliasUbicacionPersonal = "";
        _nombreCasaOriginal = "";
        if (LatCasa.HasValue && LngCasa.HasValue)
        {
            var punto = new Location(LatCasa.Value, LngCasa.Value);
            _ = Task.Run(async () =>
            {
                string sugerida = Limpiar(await ObtenerDireccionLegibleAsync(punto));
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (string.IsNullOrWhiteSpace(AliasUbicacionPersonal) && sugerida != Limpiar(AppResources.ConfirmarReserva_PinSinDireccion))
                        AliasUbicacionPersonal = sugerida;
                });
            });
        }
    }

    // Un lugar del historial se aplica a lo que esté activo (casa o universidad)
    [RelayCommand]
    public void SeleccionarLugar(LugarFrecuente? lugar)
    {
        if (lugar == null) return;
        if (EditandoCasa)
        {
            LatCasa = lugar.Latitud; LngCasa = lugar.Longitud;
            UsarNombreDeCasa(lugar.Nombre);
            CasaEsNueva = false;
        }
        else
        {
            ModoAlterno = "Guardada";
            LatAlterno = lugar.Latitud; LngAlterno = lugar.Longitud;
            TextoAlterno = lugar.Nombre;
        }
        RedibujarPines();
    }

    public async void ProcesarToqueMapa(Location ubicacion)
    {
        if (EditandoCasa)
        {
            LatCasa = ubicacion.Latitude; LngCasa = ubicacion.Longitude; CasaEsNueva = true;
            RedibujarPines();
            // Si todavía no escribió referencia, se sugiere la dirección (la puede cambiar)
            if (string.IsNullOrWhiteSpace(AliasUbicacionPersonal))
                AliasUbicacionPersonal = Limpiar(await ObtenerDireccionLegibleAsync(ubicacion));
        }
        else
        {
            ModoAlterno = "Mapa"; LatAlterno = ubicacion.Latitude; LngAlterno = ubicacion.Longitude;
            TextoAlterno = AppResources.ConfirmarReserva_Ubicando; RedibujarPines();
            TextoAlterno = Limpiar(await ObtenerDireccionLegibleAsync(ubicacion));
            RedibujarPines();
        }
    }

    private static async Task<string> ObtenerDireccionLegibleAsync(Location ubicacion)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(ubicacion);
            var lugar = placemarks?.FirstOrDefault();
            if (lugar != null)
            {
                var partes = new List<string>();
                if (!string.IsNullOrWhiteSpace(lugar.Thoroughfare)) partes.Add(lugar.Thoroughfare);
                if (!string.IsNullOrWhiteSpace(lugar.SubLocality)) partes.Add(lugar.SubLocality);
                if (!string.IsNullOrWhiteSpace(lugar.Locality)) partes.Add(lugar.Locality);
                if (partes.Count > 0) return string.Join(", ", partes);
            }
        }
        catch { }
        return AppResources.ConfirmarReserva_PinSinDireccion;
    }

    // Se muestran los dos puntos; el que se está editando va primero (el mapa se centra en él)
    private void RedibujarPines()
    {
        var casa = (LatCasa.HasValue && LngCasa.HasValue)
            ? new Pin { Label = $"🏠 {(string.IsNullOrWhiteSpace(AliasUbicacionPersonal) ? AppResources.Confirm_MyHomePin : AliasUbicacionPersonal)}", Type = PinType.SavedPin, Location = new Location(LatCasa.Value, LngCasa.Value) }
            : null;
        var u = (LatAlterno.HasValue && LngAlterno.HasValue)
            ? new Pin { Label = $"🏫 {TextoAlterno}", Type = PinType.Place, Location = new Location(LatAlterno.Value, LngAlterno.Value) }
            : null;

        PinesMapa.Clear();
        var primero = EditandoCasa ? casa : u;
        var segundo = EditandoCasa ? u : casa;
        if (primero != null) PinesMapa.Add(primero);
        if (segundo != null) PinesMapa.Add(segundo);
    }

    // ───────────────────────── Confirmar ─────────────────────────

    [RelayCommand]
    public async Task ConfirmarAsync()
    {
        if (Shell.Current == null || IsSaving) return;

        if (_selecciones.Count == 0)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Confirm_NoTripsChosen, AppResources.Global_Ok);
            return;
        }
        if (LatCasa == null || LngCasa == null)
        {
            EditandoCasa = true;
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Confirm_MarkAddress, AppResources.Global_Ok);
            return;
        }
        if (EsReferenciaVaga(AliasUbicacionPersonal))
        {
            EditandoCasa = true;
            await Shell.Current.DisplayAlert(AppResources.Confirm_WriteAddressTitle,
                AppResources.Confirm_WriteAddressMsg,
                AppResources.Global_Ok);
            return;
        }
        if (LatAlterno == null || LngAlterno == null)
        {
            EditandoCasa = false;
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Confirm_ChooseAltPoint, AppResources.Global_Ok);
            return;
        }
        if (UsandoRangoPersonalizado && FechaHastaPersonalizada.Date < DateTime.Today)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Confirm_RepeatDatePassed, AppResources.Global_Ok);
            return;
        }
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.PassengerDash_NeedInternetToBook, AppResources.Global_Ok);
            return;
        }

        var yo = _supabaseService.GetCurrentUser();
        if (yo == null || string.IsNullOrEmpty(yo.Id))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.PassengerDash_SessionExpired, AppResources.Global_Ok);
            return;
        }

        IsSaving = true;
        var resultado = await Task.Run(() => ReservarAsync(yo.Id));
        IsSaving = false;

        if (resultado.Creadas > 0)
        {
            string msg = resultado.Creadas == 1 ? AppResources.Confirm_BookedOne : AppResources.F(AppResources.Confirm_BookedMany, resultado.Creadas);
            if (resultado.YaTenias > 0) msg += "\n" + AppResources.F(AppResources.Confirm_AlreadyHadSome, resultado.YaTenias);
            if (!string.IsNullOrEmpty(resultado.Aviso)) msg += $"\n\n⚠️ {resultado.Aviso}";
            await Shell.Current.DisplayAlert(AppResources.Confirm_BookingConfirmedTitle, msg, AppResources.Global_Ok);
            await Shell.Current.Navigation.PopAsync();
        }
        else if (resultado.YaTenias > 0 && string.IsNullOrEmpty(resultado.Error))
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.Confirm_AlreadyHadAll, AppResources.Global_Ok);
            await Shell.Current.Navigation.PopAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error,
                string.IsNullOrEmpty(resultado.Error) ? AppResources.PassengerDash_BookingError : AppResources.F(AppResources.Confirm_CouldNotBook, resultado.Error),
                AppResources.Global_Ok);
        }
    }

    private async Task<(int Creadas, int YaTenias, string Aviso, string Error)> ReservarAsync(string miId)
    {
        try
        {
            var client = _supabaseService.Client;
            if (client == null) return (0, 0, "", AppResources.Global_NoServerConnection);

            string referenciaCasa = AliasUbicacionPersonal.Trim();
            string aviso = "";

            // 1) Guardar lugares nuevos en "mis lugares" (si la tabla no existe, la reserva sigue igual)
            if (CasaEsNueva || !string.Equals(referenciaCasa, _nombreCasaOriginal, StringComparison.OrdinalIgnoreCase))
                await _supabaseService.CreateUbicacionAsync(new UbicacionPasajero { Id = Guid.NewGuid().ToString(), IdPasajero = miId, Alias = referenciaCasa, DireccionTexto = referenciaCasa, Latitud = LatCasa, Longitud = LngCasa, Tipo = "casa" });
            if (ModoAlterno == "Mapa" && GuardarNuevoAlternoEnHistorial)
                await _supabaseService.CreateUbicacionAsync(new UbicacionPasajero { Id = Guid.NewGuid().ToString(), IdPasajero = miId, Alias = TextoAlterno, DireccionTexto = TextoAlterno, Latitud = LatAlterno, Longitud = LngAlterno, Tipo = "alterno" });

            // 2) Hasta qué fecha se repite cada selección (mismo día de la semana, misma hora)
            DateTime hoy = DateTime.Today;
            DateTime limite =
                AlcanceSeleccionado == AlcanceUnMes ? hoy.AddDays(30) :
                AlcanceSeleccionado == AlcanceCiclo ? FechaMaximaRango :
                AlcanceSeleccionado == AlcancePersonalizado ? FechaHastaPersonalizada.Date :
                DateTime.MinValue;
            if (limite > FechaMaximaRango) limite = FechaMaximaRango;

            // 3) Todas las salidas de esos viajes en el rango (por VIAJE, no por chofer:
            //    antes, si otro día manejaba otra persona, ese día se saltaba)
            var idsViajes = _selecciones.Select(s => s.IdViajeBase).Distinct().ToList();
            DateTime desde = _selecciones.Min(s => s.FechaAsignacion.Date);
            DateTime hasta = limite > desde ? limite : _selecciones.Max(s => s.FechaAsignacion.Date);

            var resp = await client.From<Asignacion>()
                .Filter("id_viaje", Operador.In, idsViajes)
                .Filter("fecha", Operador.GreaterThanOrEqual, desde.DesdeElSalvadorAUtc().ToString("o", CultureInfo.InvariantCulture))
                .Filter("fecha", Operador.LessThan, hasta.AddDays(1).DesdeElSalvadorAUtc().ToString("o", CultureInfo.InvariantCulture))
                .Get();
            var salidas = resp.Models ?? new List<Asignacion>();

            // 4) Lo que ya tengo reservado, para no duplicar
            var misReservas = await _supabaseService.GetReservasPorPasajeroYRangoAsync(miId, desde, hasta);
            var yaReservadas = new HashSet<string>(misReservas.Where(r => r.Estado != "cancelado").Select(r => r.IdAsignacion));

            var objetivo = new List<(Asignacion Asig, bool EsIda)>();
            var diasYaTomados = new HashSet<string>(); // viaje + día
            int yaTenias = 0;
            DateTime ahora = DateTime.Now;

            foreach (var sel in _selecciones.OrderBy(s => s.FechaAsignacion))
            {
                // Fechas: la elegida y, si se repite, la misma cada 7 días
                var fechas = new List<DateTime> { sel.FechaAsignacion.Date };
                if (limite != DateTime.MinValue)
                    for (var f = sel.FechaAsignacion.Date.AddDays(7); f <= limite; f = f.AddDays(7)) fechas.Add(f);

                foreach (var dia in fechas)
                {
                    string clave = $"{sel.IdViajeBase}|{dia:yyyyMMdd}";
                    if (!diasYaTomados.Add(clave)) continue;

                    var delDia = salidas.Where(a => a.IdViaje == sel.IdViajeBase && a.Fecha.AHoraElSalvador().Date == dia).ToList();
                    if (dia == sel.FechaAsignacion.Date && delDia.All(a => a.Id != sel.IdAsignacion))
                    {
                        // La elegida siempre cuenta, aunque la consulta no la haya traído
                        delDia.Insert(0, new Asignacion { Id = sel.IdAsignacion, IdViaje = sel.IdViajeBase, IdChofer = sel.IdChofer, Fecha = sel.FechaAsignacion });
                    }
                    if (delDia.Count == 0) continue; // ese día el viaje no sale

                    if (delDia.Any(a => yaReservadas.Contains(a.Id))) { yaTenias++; continue; }

                    var elegida = dia == sel.FechaAsignacion.Date
                        ? delDia.First(a => a.Id == sel.IdAsignacion)
                        : (delDia.FirstOrDefault(a => a.IdChofer == sel.IdChofer) ?? delDia.OrderBy(a => a.CreadoEn).First());

                    if (elegida.Fecha.AHoraElSalvador() < ahora) continue; // ya pasó

                    objetivo.Add((elegida, sel.EsIda));
                }
            }

            if (objetivo.Count == 0) return (0, yaTenias, "", "");

            // 5) Crear todas las reservas en lotes (antes: una llamada por viaje)
            var reservas = objetivo.Select(o =>
            {
                var r = new Reserva { Id = Guid.NewGuid().ToString(), IdAsignacion = o.Asig.Id, IdPasajero = miId, Estado = "pendiente", CreadoEn = DateTime.UtcNow };
                if (o.EsIda)
                {
                    r.PuntoRecogidaTexto = referenciaCasa; r.LatitudRecogida = LatCasa; r.LongitudRecogida = LngCasa;
                    r.PuntoBajadaTexto = TextoAlterno; r.LatitudBajada = LatAlterno; r.LongitudBajada = LngAlterno;
                }
                else
                {
                    r.PuntoRecogidaTexto = TextoAlterno; r.LatitudRecogida = LatAlterno; r.LongitudRecogida = LngAlterno;
                    r.PuntoBajadaTexto = referenciaCasa; r.LatitudBajada = LatCasa; r.LongitudBajada = LngCasa;
                }
                return r;
            }).ToList();

            int creadas = 0;
            for (int i = 0; i < reservas.Count; i += 50)
            {
                var lote = reservas.Skip(i).Take(50).ToList();
                try
                {
                    await client.From<Reserva>().Insert(lote);
                    creadas += lote.Count;
                }
                catch (Exception ex)
                {
                    CrashLogger.Guardar(ex, "ConfirmarReservaViewModel.ReservarAsync.Insert");
                    aviso = AppResources.Confirm_SomeNotBooked;
                }
            }

            return (creadas, yaTenias, aviso, creadas == 0 ? AppResources.Confirm_ServerRejected : "");
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "ConfirmarReservaViewModel.ReservarAsync");
            return (0, 0, "", ex.Message);
        }
    }
}