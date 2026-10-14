using System.Globalization;
using System.Text;
using Microsoft.Maui.Devices.Sensors;
#if IOS
using CoreLocation;
using MapKit;
#endif

namespace PassGold.Helpers;

/// <summary>
/// Buscador de lugares compartido por todas las pantallas con mapa.
/// En iPhone, Geocoding.Default usa CLGeocoder de Apple, que solo entiende direcciones:
/// si no reconoce el nombre (ej. "Unicaes") devuelve el centro del país, un terreno rural.
/// Por eso en iPhone usamos MKLocalSearch (lugares de Apple Maps) y, para todos,
/// descartamos resultados fuera del país o que sean el "centro del país".
/// </summary>
public static class BuscadorLugares
{
    // Caja aproximada de El Salvador
    private const double LatMin = 13.0, LatMax = 14.6, LonMin = -90.3, LonMax = -87.5;

    // Centro aproximado del país (para sesgar la búsqueda cuando no hay referencia)
    private const double CentroLat = 13.75, CentroLon = -88.95;

    // Siglas comunes que los buscadores no siempre entienden
    private static readonly Dictionary<string, string> Siglas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["unicaes"] = "Universidad Católica de El Salvador",
        ["ues"] = "Universidad de El Salvador",
        ["uca"] = "Universidad Centroamericana José Simeón Cañas",
        ["utec"] = "Universidad Tecnológica de El Salvador",
        ["ufg"] = "Universidad Francisco Gavidia",
        ["udb"] = "Universidad Don Bosco",
        ["uees"] = "Universidad Evangélica de El Salvador",
        ["unab"] = "Universidad Dr. Andrés Bello",
        ["ugb"] = "Universidad Gerardo Barrios",
        ["esen"] = "Escuela Superior de Economía y Negocios",
        ["itca"] = "ITCA-FEPADE",
    };

    private static Location? _centroPaisGeocoder;

    /// <param name="query">Lo que escribió el usuario.</param>
    /// <param name="cerca">Centro actual del mapa (opcional): se priorizan resultados cercanos.</param>
    public static async Task<Location?> BuscarAsync(string query, Location? cerca = null)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length == 0) return null;

        var referencia = cerca != null && EnElSalvador(cerca) ? cerca : null;
        var variantes = Variantes(query);

#if IOS
        foreach (var v in variantes)
        {
            var r = await BuscarAppleAsync(v, referencia);
            if (r != null) return r;
        }
#endif

        var centroPais = await ObtenerCentroPaisAsync();
        foreach (var v in variantes)
        {
            IEnumerable<Location>? resultados = null;
            try { resultados = await Geocoding.Default.GetLocationsAsync($"{v}, El Salvador"); }
            catch (FeatureNotSupportedException) { }

            var valido = resultados?
                .Where(EnElSalvador)
                .Where(l => centroPais == null || Location.CalculateDistance(l, centroPais, DistanceUnits.Kilometers) > 3)
                .FirstOrDefault();

            if (valido != null) return valido;
        }
        return null;
    }

#if IOS
    private static async Task<Location?> BuscarAppleAsync(string texto, Location? referencia)
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var centro = referencia != null
                    ? new CLLocationCoordinate2D(referencia.Latitude, referencia.Longitude)
                    : new CLLocationCoordinate2D(CentroLat, CentroLon);
                // Con referencia: ~60 km alrededor. Sin referencia: todo el país.
                var span = referencia != null ? new MKCoordinateSpan(0.6, 0.6) : new MKCoordinateSpan(1.8, 2.9);

                var request = new MKLocalSearchRequest
                {
                    NaturalLanguageQuery = texto,
                    Region = new MKCoordinateRegion(centro, span),
                };
                if (OperatingSystem.IsIOSVersionAtLeast(13))
                    request.ResultTypes = MKLocalSearchResultType.PointOfInterest | MKLocalSearchResultType.Address;

                var respuesta = await new MKLocalSearch(request).StartAsync();
                return respuesta?.MapItems?
                    .Select(m => new Location(m.Placemark.Coordinate.Latitude, m.Placemark.Coordinate.Longitude))
                    .FirstOrDefault(EnElSalvador);
            });
        }
        catch
        {
            // Sin resultados o sin internet: seguimos con el geocoder normal
            return null;
        }
    }
#endif

    private static async Task<Location?> ObtenerCentroPaisAsync()
    {
        if (_centroPaisGeocoder != null) return _centroPaisGeocoder;
        try { _centroPaisGeocoder = (await Geocoding.Default.GetLocationsAsync("El Salvador"))?.FirstOrDefault(); }
        catch { }
        return _centroPaisGeocoder;
    }

    private static List<string> Variantes(string query)
    {
        var lista = new List<string>();
        if (Siglas.TryGetValue(QuitarTildes(query), out var nombreCompleto))
            lista.Add(nombreCompleto);
        lista.Add(query);
        var sinTildes = QuitarTildes(query);
        if (sinTildes != query) lista.Add(sinTildes);
        return lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool EnElSalvador(Location l) =>
        l.Latitude is >= LatMin and <= LatMax && l.Longitude is >= LonMin and <= LonMax;

    public static string QuitarTildes(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}