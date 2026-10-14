using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace PassGold.Models;

public partial class OpcionViajePasajero : ObservableObject
{
    // Variables Base (Usadas por toda la app)
    public string IdAsignacion { get; set; } = "";
    public string IdViajeBase { get; set; } = "";
    public string IdChofer { get; set; } = "";
    public DateTime FechaAsignacion { get; set; }
    public bool EsIda { get; set; }

    // Variables exclusivas para la nueva Red Social / Agenda de Flotas
    public string DestinoOOrigen { get; set; } = "";
    public string FotoChofer { get; set; } = "";
    public string NombreChofer { get; set; } = "";

    // Variables de diseño exclusivas para el Dashboard del Pasajero
    public string ChoferInfo { get; set; } = "";
    public Color ColorFondo { get; set; } = Colors.Transparent;
    public Color ColorTexto { get; set; } = Colors.White;
    public string TipoViajeCompleto { get; set; } = "";
    public string HoraSalida { get; set; } = "";
    public bool YaReservado { get; set; } = false;
    public string IdReservaExistente { get; set; } = "";

    // Propiedad interactiva (para el checkbox animado)
    [ObservableProperty]
    private bool isSelected = false;
}