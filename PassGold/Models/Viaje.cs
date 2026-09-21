using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("viajes")]
public class Viaje : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("tipo_viaje")]
    public string TipoViaje { get; set; } = string.Empty;

    [Column("hora_salida")]
    public DateTime HoraSalida { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "programado";

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [Column("dias_semana")]
    public string DiasSemana { get; set; } = "Lu, Ma, Mi, Ju, Vi, Sa, Do";

    [Column("fecha_inicio")]
    public DateTime? FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    [Column("id_creador")]
    public string IdCreador { get; set; } = string.Empty;

    [Column("hora_inicio_recorrido")]
    public TimeSpan? HoraInicioRecorrido { get; set; }

    [Column("hora_llegada_destino")]
    public TimeSpan? HoraLlegadaDestino { get; set; }

    //  NUEVO: PIN ROJO - El estacionamiento base del chofer
    [Column("meta_texto")]
    public string? MetaTexto { get; set; }

    [Column("meta_latitud")]
    public double? MetaLatitud { get; set; }

    [Column("meta_longitud")]
    public double? MetaLongitud { get; set; }
}
