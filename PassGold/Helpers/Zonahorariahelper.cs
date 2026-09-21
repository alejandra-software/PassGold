using System;

namespace PassGold.Helpers;

//  ARREGLO DE RAÍZ para los bugs de horas incorrectas que venimos persiguiendo.
//
// El problema de fondo: hasta ahora, cada pantalla usaba ".ToLocalTime()", que
// calcula la hora usando LA ZONA HORARIA QUE TENGA CONFIGURADA EL CELULAR EN ESE
// MOMENTO. Eso trae 2 problemas:
//   1) Si algo en el camino (una caché, un fetch distinto) ya había etiquetado
//      mal el "Kind" de la fecha, ".ToLocalTime()" se comporta distinto según
//      ese etiquetado — a veces no convierte, a veces convierte de más.
//   2) Si el usuario viaja a otro país y su celular cambia de zona horaria
//      automáticamente, iba a ver los horarios de los microbuses calculados con
//      la hora de ESE OTRO país — cuando en realidad todos los viajes son de
//      El Salvador, sin importar desde dónde los mire el pasajero.
//
// La solución: una sola función, usada en TODOS lados, que:
//   - Ignora lo que diga el "Kind" de la fecha que le llega (evita el problema 1).
//   - Usa SIEMPRE la zona horaria fija de El Salvador, sin importar el celular
//     de quien mira la pantalla (evita el problema 2).
public static class ZonaHorariaHelper
{
    private static readonly TimeZoneInfo ZonaElSalvador = ObtenerZonaElSalvador();

    private static TimeZoneInfo ObtenerZonaElSalvador()
    {
        // 🔧 FIX: antes esto le preguntaba al sistema operativo del celular
        // ("America/El_Salvador" o "Central America Standard Time"). En un
        // dispositivo real (Huawei, con Android modificado) esto devolvió un
        // desfase incorrecto — un viaje guardado bien (22:30 UTC = 4:30pm en El
        // Salvador) se mostraba como 4:30 AM, una diferencia de 12 horas, como si
        // el desfase se hubiera SUMADO en vez de restado. En vez de seguir
        // confiando en la base de datos de zonas horarias del teléfono (que varía
        // según el fabricante y puede venir incompleta o desactualizada), se usa
        // SIEMPRE el desfase fijo de El Salvador escrito a mano acá — sin
        // preguntarle nada al sistema operativo, así no importa qué celular sea.
        return TimeZoneInfo.CreateCustomTimeZone(
            id: "ElSalvadorFijo",
            baseUtcOffset: TimeSpan.FromHours(-6),
            displayName: "El Salvador (fijo)",
            standardDisplayName: "El Salvador (fijo)");
    }

    /// <summary>
    /// Convierte una fecha a la hora de El Salvador, SIEMPRE de la misma forma,
    /// sin importar el "Kind" con el que haya llegado ni la zona horaria del
    /// celular. Reemplaza a ".ToLocalTime()" en todas las pantallas que muestran
    /// horarios de viajes.
    /// </summary>
    public static DateTime AHoraElSalvador(this DateTime fecha)
    {
        // 🔧 CAMBIO IMPORTANTE (10/sept): la evidencia real (comparando lo que
        // Supabase tiene guardado contra lo que esta función recibía) mostró que
        // el dato YA LLEGA convertido a la hora de El Salvador antes de que
        // nuestro código lo toque — aunque venga etiquetado como "Kind=Utc". Restar
        // el desfase acá ENCIMA de eso restaba dos veces, dejando la hora 12 horas
        // atrasada en vez de la correcta. Por eso ahora esta función no convierte
        // nada — solo devuelve la fecha tal cual llega.
        return fecha;
    }

    /// <summary>
    /// La función inversa: para cuando el CHOFER elige una fecha/hora (siempre
    /// pensando "esto es la hora de El Salvador", sin importar desde qué país
    /// esté armando el horario), y hay que guardarla en Supabase como UTC.
    /// Reemplaza a ".ToUniversalTime()", que usaba la zona horaria del celular
    /// del chofer en ese momento — el mismo problema que ya arreglamos del lado
    /// de mostrar, pero al revés, del lado de guardar.
    /// </summary>
    public static DateTime DesdeElSalvadorAUtc(this DateTime fechaEnElSalvador)
    {
        DateTime comoSinEspecificar = DateTime.SpecifyKind(fechaEnElSalvador, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(comoSinEspecificar, ZonaElSalvador);
    }
}