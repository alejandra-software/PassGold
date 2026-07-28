using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace GoldeenRide.Models;

[Table("viajes")]
public class Viaje : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = "";

    [Column("id_creador")]
    public string? IdCreador { get; set; }

    [Column("tipo_viaje")]
    public string TipoViaje { get; set; } = "";

    // 🔥 NUEVOS CAMPOS: TIEMPOS REALES Y RUTA 🔥
    [Column("id_ruta")]
    public string? IdRuta { get; set; }

    [Column("hora_inicio_recorrido")]
    public TimeSpan? HoraInicioRecorrido { get; set; }

    [Column("hora_llegada_destino")]
    public TimeSpan? HoraLlegadaDestino { get; set; }

    // Mantenemos estos por compatibilidad temporal con datos viejos
    [Column("hora_salida")]
    public DateTime HoraSalida { get; set; }

    [Column("ruta_general")]
    public string RutaGeneral { get; set; } = "";

    [Column("estado")]
    public string Estado { get; set; } = "programado";

    [Column("dias_semana")]
    public string DiasSemana { get; set; } = "";

    [Column("fecha_inicio")]
    public DateTime? FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }
}