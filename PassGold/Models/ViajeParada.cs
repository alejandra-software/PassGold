using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PassGold.Models;

[Table("viaje_paradas")]
public class ViajeParada : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("id_viaje")]
    public string IdViaje { get; set; } = string.Empty;

    [Column("orden")]
    public int Orden { get; set; } = 1;

    [Column("nombre_lugar")]
    public string NombreLugar { get; set; } = string.Empty;

    [Column("latitud")]
    public double? Latitud { get; set; }

    [Column("longitud")]
    public double? Longitud { get; set; }
}