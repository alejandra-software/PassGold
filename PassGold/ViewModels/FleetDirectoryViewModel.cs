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
using Microsoft.Maui.ApplicationModel;

using PassGold.Helpers;

namespace PassGold.ViewModels;

public partial class FleetDirectoryViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string textoBusqueda = "";
    // 🆕 Lo que se muestra cuando la lista está vacía (antes: pantalla en blanco sin explicación)
    [ObservableProperty] private string mensajeVacio = "";

    private List<FlotaPasajeroCard> _todasLasFlotas = new();
    public ObservableCollection<FlotaPasajeroCard> FlotasDisponibles { get; } = new();

    [RelayCommand]
    public async Task LoadDirectoryAsync()
    {
        if (_todasLasFlotas.Count > 0) return;

        MainThread.BeginInvokeOnMainThread(() => { IsLoading = true; MensajeVacio = ""; });

        try
        {
            // Sin sesión real de Supabase, las reglas de seguridad no devuelven ninguna flota
            if (_supabaseService.GetCurrentUser() == null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    MensajeVacio = AppResources.Directory_SessionInactive);
                return;
            }

            // 🔧 Consulta propia (antes GetPublicFleetsAsync escondía cualquier error y
            // devolvía la lista vacía sin decir nada)
            var client = _supabaseService.Client;
            if (client == null)
            {
                MainThread.BeginInvokeOnMainThread(() => MensajeVacio = AppResources.Global_NoServerConnection);
                return;
            }
            var respuesta = await client.From<Usuario>().Get();
            var flotas = (respuesta.Models ?? new List<Usuario>())
                .Where(u => (u.Rol ?? "").Contains("jefe", StringComparison.OrdinalIgnoreCase)
                         || (u.Rol ?? "").Contains("owner", StringComparison.OrdinalIgnoreCase)
                         || (u.Rol ?? "").Contains("independiente", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var flotasProcesadas = new List<FlotaPasajeroCard>();

            foreach (var flota in flotas)
            {
                var rutasUnicas = new List<string>();
                if (!string.IsNullOrWhiteSpace(flota.RutasFlota))
                    rutasUnicas = flota.RutasFlota.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).Distinct().ToList();
                if (rutasUnicas.Count == 0) rutasUnicas.Add(AppResources.Fleet_RoutesToAgree);

                flotasProcesadas.Add(new FlotaPasajeroCard
                {
                    IdFlota = flota.Id,
                    NombreFlota = string.IsNullOrEmpty(flota.NombreFlota) ? AppResources.F(AppResources.Fleet_DefaultName, flota.Nombre) : flota.NombreFlota,
                    NombreJefe = flota.Nombre ?? "",
                    FotoPerfil = flota.FotoPerfil ?? "",
                    Telefono = flota.Telefono ?? "",
                    RutasList = rutasUnicas,
                    FlotaOriginal = flota
                });
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _todasLasFlotas.Clear();
                FlotasDisponibles.Clear();
                foreach (var card in flotasProcesadas)
                {
                    _todasLasFlotas.Add(card);
                    FlotasDisponibles.Add(card);
                }
                if (flotasProcesadas.Count == 0)
                    MensajeVacio = AppResources.Directory_NoFleetsYet;
            });
        }
        catch (Exception ex)
        {
            CrashLogger.Guardar(ex, "FleetDirectoryViewModel.LoadDirectoryAsync");
            MainThread.BeginInvokeOnMainThread(() =>
                MensajeVacio = AppResources.F(AppResources.Directory_LoadError, ex.Message));
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public void BuscarFlotas()
    {
        MainThread.BeginInvokeOnMainThread(() =>
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
            if (resultados.Count == 0 && _todasLasFlotas.Count > 0)
                MensajeVacio = AppResources.Directory_NoMatch;
        });
    }

    [RelayCommand]
    public async Task IrAPerfilFlotaAsync(FlotaPasajeroCard flotaCard)
    {
        if (flotaCard == null || Shell.Current == null) return;

        var navigationParams = new Dictionary<string, object>
        {
            { "FlotaObj", flotaCard.FlotaOriginal }
        };
        await Shell.Current.GoToAsync("fleet-profile", navigationParams);
    }
}