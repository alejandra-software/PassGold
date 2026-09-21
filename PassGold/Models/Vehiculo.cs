using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("vehiculos")]
public class Vehiculo : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("id_propietario")]
    public string IdPropietario { get; set; } = string.Empty;

    [Column("placa")]
    public string Placa { get; set; } = string.Empty;

    [Column("capacidad")]
    public int Capacidad { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "activo";

    [Column("foto_url")]
    public string FotoUrl { get; set; } = string.Empty;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
