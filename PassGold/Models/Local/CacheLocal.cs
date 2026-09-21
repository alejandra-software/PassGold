using SQLite;
using System;

namespace PassGold.Models.Local;

[Table("CacheLocal")]
public class CacheLocal
{
    // La clave ser� el nombre del dato, ej: "Flotas_Directorio"
    [PrimaryKey]
    public string Clave { get; set; } = string.Empty;

    // Aqu� guardaremos todo lo que venga de Supabase convertido en texto
    public string DatosJson { get; set; } = string.Empty;

    public DateTime FechaGuardado { get; set; } = DateTime.UtcNow;
}
