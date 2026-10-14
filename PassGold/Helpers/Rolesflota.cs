using PassGold.Models;

namespace PassGold.Helpers;

// Un solo lugar para decidir quién puede hacer qué en una flota.
// Antes cada pantalla revisaba el texto del rol por su cuenta.
public static class RolesFlota
{
    // Valores tal como se guardan en la base de datos
    public const string Pasajero = "pasajero";
    public const string Independiente = "chofer_independiente";
    public const string Jefe = "chofer_jefe";
    public const string Empleado = "chofer_empleado";

    public static string Normalizar(string? rol)
    {
        string r = (rol ?? "").ToLowerInvariant();
        if (r.Contains("jefe") || r.Contains("owner")) return Jefe;
        if (r.Contains("empleado") || r.Contains("employee")) return Empleado;
        if (r.Contains("independ")) return Independiente;
        return Pasajero;
    }

    public static bool EsDuenoDeFlota(string? rol)
    {
        string r = Normalizar(rol);
        return r == Jefe || r == Independiente;
    }

    public static bool EsEmpleado(string? rol) => Normalizar(rol) == Empleado;
    public static bool EsPasajero(string? rol) => Normalizar(rol) == Pasajero;

    // Empleado al que su jefe le dio permiso de crear y editar viajes
    public static bool EsSubJefe(Usuario? u) =>
        u != null && EsEmpleado(u.Rol) && !string.IsNullOrEmpty(u.IdJefe) && u.PuedeGestionarViajes;

    // ¿Puede crear y editar los viajes de la flota?
    public static bool PuedeGestionarViajes(Usuario? u) =>
        u != null && (EsDuenoDeFlota(u.Rol) || EsSubJefe(u));

    // Id del dueño de la flota con la que trabaja esta persona
    public static string IdFlota(Usuario u) =>
        EsDuenoDeFlota(u.Rol) ? u.Id : (u.IdJefe ?? u.Id);
}