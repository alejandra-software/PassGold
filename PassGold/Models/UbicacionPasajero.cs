using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("ubicaciones_pasajeros")]
public class UbicacionPasajero : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = "";

    [Column("id_pasajero")]
    public string IdPasajero { get; set; } = "";

    [Column("alias")]
    public string Alias { get; set; } = "";

    [Column("direccion_texto")]
    public string DireccionTexto { get; set; } = "";

    [Column("latitud")]
    public double? Latitud { get; set; }

    [Column("longitud")]
    public double? Longitud { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Propiedad combinada para mostrar bonito en la lista desplegable de la App
    public string DisplayName => $"{Alias} - {DireccionTexto}";
}
