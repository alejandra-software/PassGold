using SQLite;
using System;

namespace PassGold.Models.Local;

[Table("Vehiculos")]
public class VehiculoLocal
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string IdPropietario { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FotoUrl { get; set; } = string.Empty;
}
