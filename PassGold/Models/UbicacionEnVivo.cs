using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

// Una sola fila por asignación (se sobrescribe cada pocos segundos).
// Va en sintonía con la tabla creada en supabase_rastreo.sql.
[Table("ubicaciones_en_vivo")]
public class UbicacionEnVivo : BaseModel
{
    [PrimaryKey("id_asignacion", true)]
    public string IdAsignacion { get; set; } = string.Empty;

    [Column("id_chofer")]
    public string IdChofer { get; set; } = string.Empty;

    [Column("latitud")]
    public double Latitud { get; set; }

    [Column("longitud")]
    public double Longitud { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; }
}