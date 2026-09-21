using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("reservas")]
public class Reserva : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("id_asignacion")]
    public string IdAsignacion { get; set; } = string.Empty;

    [Column("id_pasajero")]
    public string IdPasajero { get; set; } = string.Empty;

    // PIN VERDE (O AZUL, dependiendo si es viaje de ida o regreso)
    [Column("punto_recogida_texto")]
    public string PuntoRecogidaTexto { get; set; } = string.Empty;

    [Column("latitud")]
    public double? LatitudRecogida { get; set; }

    [Column("longitud")]
    public double? LongitudRecogida { get; set; }

    // ?? NUEVO: PIN AZUL (O VERDE) - El destino del pasajero
    [Column("punto_bajada_texto")]
    public string? PuntoBajadaTexto { get; set; }

    [Column("latitud_bajada")]
    public double? LatitudBajada { get; set; }

    [Column("longitud_bajada")]
    public double? LongitudBajada { get; set; }

    [Column("hora_estimada_recogida")]
    public DateTime? HoraEstimadaRecogida { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "pendiente";

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }
}
