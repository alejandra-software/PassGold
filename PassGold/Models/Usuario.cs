using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace PassGold.Models;

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = "";

    // 🔧 Estos campos pueden venir en null desde Supabase (por ejemplo, alguien que
    // entró con Google y no terminó el registro). Antes ese null hacía fallar
    // cualquier ".Rol.Contains(...)" y, por ejemplo, el directorio de flotas quedaba
    // vacío para TODOS. Ahora un null se convierte en texto vacío.
    private string _email = "";
    private string _nombre = "";
    private string _rol = "";
    private string _estado = "activo";

    [Column("email")]
    public string Email { get => _email; set => _email = value ?? ""; }

    [Column("nombre")]
    public string Nombre { get => _nombre; set => _nombre = value ?? ""; }

    [Column("rol")]
    public string Rol { get => _rol; set => _rol = value ?? ""; }

    [Column("foto_perfil")]
    public string? FotoPerfil { get; set; }

    [Column("id_jefe")]
    public string? IdJefe { get; set; }

    [Column("estado")]
    public string Estado { get => _estado; set => _estado = value ?? "activo"; }

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

    // 🆕 Sub-jefe: el jefe le permite a este empleado crear y editar viajes.
    // Solo se cambia con la función fijar_permiso_viajes de Supabase.
    [Column("puede_gestionar_viajes")]
    public bool PuedeGestionarViajes { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}