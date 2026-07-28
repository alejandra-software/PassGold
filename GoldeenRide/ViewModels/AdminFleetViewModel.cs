using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using GoldeenRide.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace GoldeenRide.ViewModels;

public partial class AdminFleetViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    public ObservableCollection<Vehiculo> MisMicrobuses { get; } = new();
    public ObservableCollection<Usuario> MisChoferes { get; } = new();

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool hasNoEmployees = false;

    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private string fleetCode = "Cargando...";

    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private string joinCodeInput = "";
    [ObservableProperty] private string joinErrorMessage = "";
    [ObservableProperty] private string bossName = "";
    [ObservableProperty] private bool hasJoinedFleet = false;

    [RelayCommand]
    public async Task LoadFleetDataAsync()
    {
        IsLoading = true;
        JoinErrorMessage = string.Empty;

        // 🔥 BLOQUEO FANTASMAL: Limpiamos antes de consultar
        IsFleetManager = false;
        IsEmployee = false;
        FleetCode = "Cargando...";
        MisMicrobuses.Clear();
        MisChoferes.Clear();

        try
        {
            var currentUser = _supabaseService.GetCurrentUser();

            if (currentUser != null && !string.IsNullOrEmpty(currentUser.Id))
            {
                var userData = await _supabaseService.GetUserDataAsync(currentUser.Id);

                if (userData != null)
                {
                    string rol = userData.Rol.ToLower();

                    // Bloqueamos la interfaz según el rol estricto
                    if (rol.Contains("jefe") || rol.Contains("owner") || rol.Contains("independiente"))
                    {
                        IsFleetManager = true;
                        IsEmployee = false;

                        var codigo = await _supabaseService.GetOrCreateFleetCodeAsync(currentUser.Id);
                        FleetCode = string.IsNullOrWhiteSpace(codigo) ? "ERR-CÓD" : codigo;

                        var vehiculos = await _supabaseService.GetVehiclesByOwnerAsync(currentUser.Id);
                        foreach (var v in vehiculos) MisMicrobuses.Add(v);

                        var empleados = await _supabaseService.GetEmployeesByBossAsync(currentUser.Id);
                        foreach (var e in empleados) MisChoferes.Add(e);

                        HasNoEmployees = MisChoferes.Count == 0;
                    }
                    else if (rol.Contains("empleado") || rol.Contains("employee"))
                    {
                        IsFleetManager = false;
                        IsEmployee = true;

                        if (!string.IsNullOrEmpty(userData.IdJefe))
                        {
                            HasJoinedFleet = true;
                            var bossData = await _supabaseService.GetUserDataAsync(userData.IdJefe);
                            BossName = $"{AppResources.SettingsWorkingFor} {bossData?.Nombre ?? AppResources.Dashboard_Unknown}";
                        }
                        else
                        {
                            HasJoinedFleet = false;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en AdminFleet: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CopyCodeAsync()
    {
        await Clipboard.Default.SetTextAsync(FleetCode);
        if (Shell.Current != null)
            await Shell.Current.DisplayAlert(AppResources.Global_Attention, AppResources.SettingsInviteDesc, AppResources.Global_Ok);
    }

    [RelayCommand]
    public async Task RegenerateCodeAsync()
    {
        if (Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert(
            "Regenerar Código",
            "¿Estás seguro de que deseas cambiar tu código de flota? El código anterior dejará de funcionar para nuevos empleados.",
            "Sí, cambiar", "Cancelar");

        if (!confirmar) return;

        IsLoading = true;
        var currentUser = _supabaseService.GetCurrentUser();

        if (currentUser != null)
        {
            var nuevoCodigo = await _supabaseService.RegenerateFleetCodeAsync(currentUser.Id);

            if (!string.IsNullOrEmpty(nuevoCodigo))
            {
                FleetCode = nuevoCodigo;
                await Shell.Current.DisplayAlert("Éxito", "Tu código de flota ha sido actualizado.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo regenerar el código.", "OK");
            }
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task JoinFleetAsync()
    {
        if (string.IsNullOrWhiteSpace(JoinCodeInput))
        {
            JoinErrorMessage = AppResources.Error_EmptyList;
            return;
        }

        IsLoading = true;
        JoinErrorMessage = string.Empty;

        var currentUser = _supabaseService.GetCurrentUser();
        if (currentUser != null)
        {
            var (success, message) = await _supabaseService.JoinFleetByCodeAsync(JoinCodeInput, currentUser.Id);

            if (success)
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(AppResources.Global_Ok, message, AppResources.Global_Ok);

                JoinCodeInput = string.Empty;
                await LoadFleetDataAsync();
            }
            else JoinErrorMessage = message;
        }

        IsLoading = false;
    }

    [RelayCommand]
    public async Task AddMicrobus()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("add-vehicle");
    }

    [RelayCommand]
    public async Task ToggleVehiculoActivoAsync(Vehiculo vehiculo)
    {
        if (vehiculo == null || Shell.Current == null) return;

        bool estaActivo = vehiculo.Estado == "activo";
        string pregunta = estaActivo
            ? $"¿Desactivar el microbús {vehiculo.Placa}? Ya no aparecerá disponible para programar viajes nuevos, pero su historial se conserva."
            : $"¿Reactivar el microbús {vehiculo.Placa}?";

        bool confirmar = await Shell.Current.DisplayAlert("Confirmar", pregunta, "Sí", "Cancelar");
        if (!confirmar) return;

        IsLoading = true;
        bool exito = estaActivo
            ? await _supabaseService.DeactivateVehiculoAsync(vehiculo.Id)
            : await _supabaseService.ReactivateVehiculoAsync(vehiculo.Id);
        IsLoading = false;

        if (exito) await LoadFleetDataAsync();
        else await Shell.Current.DisplayAlert("Error", "No se pudo actualizar el microbús.", "OK");
    }

    [RelayCommand]
    public async Task RemoverEmpleadoAsync(Usuario empleado)
    {
        if (empleado == null || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert(
            "Confirmar",
            $"¿Sacar a {empleado.Nombre} de tu flota? Perderá acceso inmediato a los viajes y datos de tu equipo.",
            "Sí, sacarlo", "Cancelar");
        if (!confirmar) return;

        IsLoading = true;
        bool exito = await _supabaseService.RemoveEmployeeFromFleetAsync(empleado.Id);
        IsLoading = false;

        if (exito) await LoadFleetDataAsync();
        else await Shell.Current.DisplayAlert("Error", "No se pudo sacar al empleado.", "OK");
    }
}