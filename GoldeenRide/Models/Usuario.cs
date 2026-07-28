using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace GoldeenRide.Models;

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("rol")]
    public string Rol { get; set; } = string.Empty;

    [Column("foto_perfil")]
    public string? FotoPerfil { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("id_jefe")]
    public string? IdJefe { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "activo"; // activo, inactivo, incapacidad

    [Column("id_vehiculo_default")]
    public string? IdVehiculoDefault { get; set; }

    [Column("nombre_flota")]
    public string? NombreFlota { get; set; }

    [Column("descripcion_flota")]
    public string? DescripcionFlota { get; set; }

    [Column("foto_portada")]
    public string? FotoPortada { get; set; }

    // ─── NUEVO: código corto y amigable para unirse a la flota ───
    // Ya NO usamos el UUID del jefe como "código secreto".
    [Column("codigo_flota")]
    public string? CodigoFlota { get; set; }
}