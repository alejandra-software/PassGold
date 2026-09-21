using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace PassGold.ViewModels;

public partial class FleetDirectoryViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string textoBusqueda = "";

    private List<FlotaPasajeroCard> _todasLasFlotas = new();
    public ObservableCollection<FlotaPasajeroCard> FlotasDisponibles { get; } = new();

    [RelayCommand]
    public async Task LoadDirectoryAsync()
    {
        // Evitar recargar si ya bajamos las flotas
        if (_todasLasFlotas.Count > 0) return;

        IsLoading = true;
        try
        {
            var flotas = await _supabaseService.GetPublicFleetsAsync();
            _todasLasFlotas.Clear();
            FlotasDisponibles.Clear();

            foreach (var flota in flotas)
            {
                var rutasUnicas = new List<string>();
                if (!string.IsNullOrWhiteSpace(flota.RutasFlota))
                    rutasUnicas = flota.RutasFlota.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).Distinct().ToList();
                if (rutasUnicas.Count == 0) rutasUnicas.Add("Rutas a convenir");

                var card = new FlotaPasajeroCard
                {
                    IdFlota = flota.Id,
                    NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? $"Transportes {flota.Nombre}" : flota.NombreFlota,
                    NombreJefe = flota.Nombre ?? "",
                    FotoPerfil = flota.FotoPerfil ?? "",
                    Telefono = flota.Telefono ?? "",
                    RutasList = rutasUnicas,
                    FlotaOriginal = flota
                };

                _todasLasFlotas.Add(card);
                FlotasDisponibles.Add(card);
            }
        }
        catch { }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void BuscarFlotas()
    {
        FlotasDisponibles.Clear();
        if (string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            foreach (var f in _todasLasFlotas) FlotasDisponibles.Add(f);
            return;
        }

        var palabrasClave = TextoBusqueda.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var resultados = _todasLasFlotas.Where(f =>
        {
            string datosFlota = $"{f.NombreFlota}{f.NombreJefe}{string.Join("", f.RutasList)}".ToLowerInvariant().Replace(" ", "");
            return palabrasClave.All(palabra => datosFlota.Contains(palabra));
        }).ToList();

        foreach (var f in resultados) FlotasDisponibles.Add(f);
    }

    [RelayCommand]
    public async Task IrAPerfilFlotaAsync(FlotaPasajeroCard flotaCard)
    {
        if (flotaCard == null || Shell.Current == null) return;

        var navigationParams = new Dictionary<string, object>
        {
            { "FlotaObj", flotaCard.FlotaOriginal }
        };
        // Viajamos al Perfil Social de la flota
        await Shell.Current.GoToAsync("fleet-profile", navigationParams);
    }
}
