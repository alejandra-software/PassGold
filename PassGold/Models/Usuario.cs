using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = "";

    [Column("email")]
    public string Email { get; set; } = "";

    [Column("nombre")]
    public string Nombre { get; set; } = "";

    [Column("rol")]
    public string Rol { get; set; } = "";

    [Column("foto_perfil")]
    public string? FotoPerfil { get; set; }

    [Column("id_jefe")]
    public string? IdJefe { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "activo";

    [Column("id_vehiculo_default")]
    public string? IdVehiculoDefault { get; set; }

    [Column("nombre_flota")]
    public string? NombreFlota { get; set; }

    [Column("descripcion_flota")]
    public string? DescripcionFlota { get; set; }

    [Column("foto_portada")]
    public string? FotoPortada { get; set; }

    [Column("codigo_flota")]
    public string? CodigoFlota { get; set; }

    //  NUEVOS CAMPOS añadadidos
    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("rutas_flota")]
    public string? RutasFlota { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
