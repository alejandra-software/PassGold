using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace PassGold.ViewModels;

[QueryProperty(nameof(FlotaSeleccionada), "FlotaObj")]
public partial class FleetProfileViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool hasTeam = false;
    [ObservableProperty] private bool hasBio = false;

    //  Para que el chofer pueda ver el perfil de una flota (directorio ahora
    // abierto a todos) pero SIN la opción de reservar — solo el pasajero ve el
    // botón "Ver Horarios" abajo. Mismo criterio de rol que ya usa AppShellViewModel.
    [ObservableProperty] private bool isPasajero = false;

    public FleetProfileViewModel()
    {
        _ = CargarRolAsync();
    }

    private async Task CargarRolAsync()
    {
        try
        {
            var authUser = _supabaseService.GetCurrentUser();
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            {
                var data = await _supabaseService.GetUserDataAsync(authUser.Id);
                if (data != null)
                {
                    string rolLower = data.Rol.ToLower();
                    IsPasajero = rolLower.Contains("pasajero") || rolLower.Contains("passenger");
                }
            }
        }
        catch { }
    }

    // Objeto que recibimos desde la pantalla anterior
    private Usuario? _flotaSeleccionada;
    public Usuario? FlotaSeleccionada
    {
        get => _flotaSeleccionada;
        set
        {
            SetProperty(ref _flotaSeleccionada, value);
            if (value != null)
                ProcesarPerfil(value);
        }
    }

    public ObservableCollection<string> ListaHashtags { get; } = new();
    public ObservableCollection<Usuario> EquipoFlota { get; } = new();

    private void ProcesarPerfil(Usuario flota)
    {
        HasBio = !string.IsNullOrWhiteSpace(flota.DescripcionFlota);

        // Convertir las rutas normales en #Hashtags de redes sociales
        ListaHashtags.Clear();
        if (!string.IsNullOrWhiteSpace(flota.RutasFlota))
        {
            var rutas = flota.RutasFlota.Split(new[] { ',', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var r in rutas)
            {
                string hashtag = $"#{r.Trim().Replace(" ", "")}";
                ListaHashtags.Add(hashtag);
            }
        }

        // Cargar a los empleados de forma as�ncrona
        _ = CargarEquipoAsync(flota.Id);
    }

    private async Task CargarEquipoAsync(string idJefe)
    {
        IsLoading = true;
        try
        {
            var empleados = await _supabaseService.GetEmployeesByBossAsync(idJefe);

            // Filtramos para no mostrar al jefe dos veces (ya que �l es el due�o del perfil)
            var soloEmpleados = empleados.Where(e => e.Id != idJefe).ToList();

            Application.Current?.Dispatcher.Dispatch(() =>
            {
                EquipoFlota.Clear();
                foreach (var emp in soloEmpleados)
                {
                    EquipoFlota.Add(emp);
                }
                HasTeam = EquipoFlota.Count > 0;
            });
        }
        catch { }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task IrAHorariosAsync()
    {
        if (FlotaSeleccionada == null) return;

        // Aqu� es donde lo enviaremos a la NUEVA pantalla del calendario limpio a pantalla completa
        var navigationParams = new Dictionary<string, object>
        {
            { "FlotaParaCalendario", FlotaSeleccionada }
        };
        await Shell.Current.GoToAsync("fleet-schedule", navigationParams);
    }
}