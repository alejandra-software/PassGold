using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

// Aviso para el pasajero de que el chofer canceló un viaje que tenía reservado.
// Las filas las crea la función cancelar_asignaciones de Supabase (supabase_cancelaciones.sql);
// la app solo las lee y las marca como vistas.
[Table("cancelaciones")]
public class Cancelacion : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("id_pasajero")]
    public string IdPasajero { get; set; } = string.Empty;

    [Column("fecha_viaje")]
    public DateTime FechaViaje { get; set; }

    [Column("tipo_viaje")]
    public string? TipoViaje { get; set; }

    [Column("punto_recogida_texto")]
    public string? PuntoRecogidaTexto { get; set; }

    [Column("motivo")]
    public string Motivo { get; set; } = string.Empty;

    [Column("visto")]
    public bool Visto { get; set; }
}