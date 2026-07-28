using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace GoldeenRide.Models;

[Table("rutas")]
public class Ruta : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = "";

    [Column("id_propietario")]
    public string IdPropietario { get; set; } = "";

    [Column("nombre_ruta")]
    public string NombreRuta { get; set; } = "";

    [Column("origen")]
    public string Origen { get; set; } = "";

    [Column("destino")]
    public string Destino { get; set; } = "";

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }
}