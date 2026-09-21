using SQLite;
using System;

namespace PassGold.Models.Local;

[Table("SesionUsuario")]
public class UsuarioLocal
{
    // Usamos las propiedades exactas de tu modelo de Supabase
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? FotoPerfil { get; set; }
    public string? IdJefe { get; set; }
    public string Estado { get; set; } = "activo";
    public string? CodigoFlota { get; set; }

    // Fecha en la que guardamos este dato en el tel�fono
    public DateTime UltimaSincronizacion { get; set; } = DateTime.UtcNow;
}
