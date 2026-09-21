using SQLite;
using System;

namespace PassGold.Models.Local;

[Table("Viajes")]
public class ViajeLocal
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string IdCreador { get; set; } = string.Empty;
    public string TipoViaje { get; set; } = string.Empty;
    public DateTime HoraSalida { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string DiasSemana { get; set; } = string.Empty;
}
