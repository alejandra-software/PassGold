using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace GoldeenRide.Models;

[Table("asignaciones")]
public class Asignacion : BaseModel
{
    // ALERTA: Forzamos a C# a generar el UUID exacto para evitar bloqueos
    [PrimaryKey("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("id_viaje")]
    public string IdViaje { get; set; } = string.Empty;

    [Column("id_chofer")]
    public string IdChofer { get; set; } = string.Empty;

    [Column("id_vehiculo")]
    public string IdVehiculo { get; set; } = string.Empty;

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "programado";

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}