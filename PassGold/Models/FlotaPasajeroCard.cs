using System.Collections.Generic;

namespace PassGold.Models;

public class FlotaPasajeroCard
{
    public string IdFlota { get; set; } = "";
    public string NombreFlota { get; set; } = "";
    public string NombreJefe { get; set; } = "";
    public string FotoPerfil { get; set; } = "";
    public string Telefono { get; set; } = "";
    public bool TieneTelefono => !string.IsNullOrWhiteSpace(Telefono);
    public List<string> RutasList { get; set; } = new();
    public Usuario FlotaOriginal { get; set; } = new();
}