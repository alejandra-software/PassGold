using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Models;
using PassGold.Services;
using PassGold.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
using Microsoft.Maui.Networking;
using System.Collections.Generic;
using PassGold.Models.Local;

namespace PassGold.ViewModels;

public class AdminCachePayload
{
    public bool IsBoss { get; set; }
    public bool IsFleetManager { get; set; }
    public bool IsEmployee { get; set; }
    public string FleetCode { get; set; } = "Cargando...";
    public string BossName { get; set; } = "";
    public bool HasJoinedFleet { get; set; }
    public List<Vehiculo> Microbuses { get; set; } = new();
    public List<Usuario> Choferes { get; set; } = new();
    public string NombreFlota { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string DescripcionFlota { get; set; } = "";
    public string RutasFlota { get; set; } = "";
}

public partial class AdminFleetViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    public ObservableCollection<Vehiculo> MisMicrobuses { get; } = new();
    public ObservableCollection<Usuario> MisChoferes { get; } = new();

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool hasNoEmployees = false;
    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isBoss = false;
    [ObservableProperty] private string fleetCode = "Cargando...";

    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private string joinCodeInput = "";
    [ObservableProperty] private string joinErrorMessage = "";
    [ObservableProperty] private string bossName = "";
    [ObservableProperty] private bool hasJoinedFleet = false;

    //  NUEVO: Logistica de la flota (movido aqui desde Settings, solo lo administra jefe/independiente)
    [ObservableProperty] private string nombreFlotaInput = "";
    [ObservableProperty] private string telefonoInput = "";
    [ObservableProperty] private string descripcionInput = "";
    public ObservableCollection<string> RutasTags { get; } = new();
    [ObservableProperty] private string nuevaRutaInput = "";

    private Usuario? _currentUserData;
    private string _currentUserId = "";

    [RelayCommand]
    public async Task LoadFleetDataAsync()
    {
        IsLoading = true;
        JoinErrorMessage = string.Empty;

        var localDb = GetLocalDb();
        string currentUserId = "";

        //  LA CURA DE LA AMNESIA PARA EL ADMIN
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
        {
            currentUserId = authUser.Id;
        }
        else
        {
            var sesionLocal = await localDb.ObtenerSesionActivaAsync();
            if (sesionLocal != null) currentUserId = sesionLocal.Id;
        }

        if (string.IsNullOrEmpty(currentUserId))
        {
            IsLoading = false;
            return;
        }
        _currentUserId = currentUserId;

        string cacheKey = $"AdminFleetData_{currentUserId}";

        try
        {
            await Task.Delay(100);

            // 1. LECTURA INSTANTÁNEA OFFLINE
            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                var payload = JsonConvert.DeserializeObject<AdminCachePayload>(cacheJson);
                if (payload != null)
                {
                    PintarDatosEnPantalla(payload);
                }
            }

            // 2. SINCRONIZACIÓN FANTASMA
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var userData = await _supabaseService.GetUserDataAsync(currentUserId);
                        if (userData == null) return;
                        _currentUserData = userData;

                        string rol = userData.Rol.ToLower();
                        var nuevoPayload = new AdminCachePayload
                        {
                            IsBoss = rol.Contains("jefe") || rol.Contains("owner"),
                            IsEmployee = rol.Contains("empleado") || rol.Contains("employee"),
                            NombreFlota = userData.NombreFlota ?? "",
                            Telefono = userData.Telefono ?? "",
                            DescripcionFlota = userData.DescripcionFlota ?? "",
                            RutasFlota = userData.RutasFlota ?? ""
                        };
                        nuevoPayload.IsFleetManager = nuevoPayload.IsBoss || rol.Contains("independiente");

                        if (nuevoPayload.IsFleetManager)
                        {
                            nuevoPayload.Microbuses = await _supabaseService.GetVehiclesByOwnerAsync(currentUserId);

                            if (nuevoPayload.IsBoss)
                            {
                                var codigo = await _supabaseService.GetOrCreateFleetCodeAsync(currentUserId);
                                nuevoPayload.FleetCode = string.IsNullOrWhiteSpace(codigo) ? "ERR-CÓD" : codigo;
                                nuevoPayload.Choferes = await _supabaseService.GetEmployeesByBossAsync(currentUserId);
                            }
                        }
                        else if (nuevoPayload.IsEmployee && !string.IsNullOrEmpty(userData.IdJefe))
                        {
                            nuevoPayload.HasJoinedFleet = true;
                            var bossData = await _supabaseService.GetUserDataAsync(userData.IdJefe);
                            nuevoPayload.BossName = $"{AppResources.SettingsWorkingFor} {bossData?.Nombre ?? AppResources.Dashboard_Unknown}";
                        }

                        await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(nuevoPayload));

                        Application.Current?.Dispatcher.Dispatch(() =>
                        {
                            PintarDatosEnPantalla(nuevoPayload);
                        });
                    }
                    catch { }
                });
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private void PintarDatosEnPantalla(AdminCachePayload payload)
    {
        IsBoss = payload.IsBoss;
        IsFleetManager = payload.IsFleetManager;
        IsEmployee = payload.IsEmployee;
        FleetCode = payload.FleetCode;
        BossName = payload.BossName;
        HasJoinedFleet = payload.HasJoinedFleet;

        MisMicrobuses.Clear();
        foreach (var v in payload.Microbuses) MisMicrobuses.Add(v);

        MisChoferes.Clear();
        foreach (var e in payload.Choferes) MisChoferes.Add(e);

        HasNoEmployees = MisChoferes.Count == 0;

        NombreFlotaInput = payload.NombreFlota;
        TelefonoInput = payload.Telefono;
        DescripcionInput = payload.DescripcionFlota;

        RutasTags.Clear();
        if (!string.IsNullOrWhiteSpace(payload.RutasFlota))
        {
            var tags = payload.RutasFlota.Split(new[] { ',', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tags) RutasTags.Add(t.Trim());
        }
    }

    [RelayCommand]
    public void AgregarRutaTag()
    {
        if (!string.IsNullOrWhiteSpace(NuevaRutaInput))
        {
            string tagLimpio = NuevaRutaInput.Trim().TrimStart('#');
            if (!RutasTags.Contains(tagLimpio)) RutasTags.Add(tagLimpio);
            NuevaRutaInput = string.Empty;
        }
    }

    [RelayCommand]
    public void EliminarRutaTag(string tag)
    {
        if (RutasTags.Contains(tag)) RutasTags.Remove(tag);
    }

    [RelayCommand]
    public async Task SaveLogisticsAsync()
    {
        if (Shell.Current == null) return;
        IsLoading = true;

        try
        {
            if (_currentUserData == null && !string.IsNullOrEmpty(_currentUserId))
                _currentUserData = await _supabaseService.GetUserDataAsync(_currentUserId);

            if (_currentUserData == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsSessionError, AppResources.Global_Ok);
                return;
            }

            _currentUserData.NombreFlota = NombreFlotaInput.Trim();
            _currentUserData.Telefono = TelefonoInput.Trim();
            _currentUserData.DescripcionFlota = DescripcionInput.Trim();
            _currentUserData.RutasFlota = string.Join(", ", RutasTags);

            await _supabaseService.UpdateUserDataAsync(_currentUserData);
            await Shell.Current.DisplayAlert(AppResources.SettingsUpdatedTitle, AppResources.SettingsUpdatedMsg, AppResources.Global_Ok);
        }
        catch (System.Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsSaveError, AppResources.Global_Ok);
        }
        IsLoading = false;
    }

    // =========================================================================
    // RESTO DE LOS COMANDOS (BOTONES) INTACTOS
    // =========================================================================

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
        var localDb = GetLocalDb();
        var sesionLocal = await localDb.ObtenerSesionActivaAsync();
        string currentUserId = _supabaseService.GetCurrentUser()?.Id ?? sesionLocal?.Id ?? "";

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var nuevoCodigo = await _supabaseService.RegenerateFleetCodeAsync(currentUserId);

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

        var localDb = GetLocalDb();
        var sesionLocal = await localDb.ObtenerSesionActivaAsync();
        string currentUserId = _supabaseService.GetCurrentUser()?.Id ?? sesionLocal?.Id ?? "";

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var (success, message) = await _supabaseService.JoinFleetByCodeAsync(JoinCodeInput, currentUserId);

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

    [RelayCommand]
    public async Task EditarVehiculoAsync(Vehiculo vehiculo)
    {
        if (vehiculo == null || Shell.Current == null) return;

        var navParams = new Dictionary<string, object> { { "VehiculoSeleccionado", vehiculo } };
        await Shell.Current.GoToAsync("edit-vehicle", navParams);
    }

    [RelayCommand]
    public async Task EliminarVehiculoAsync(Vehiculo vehiculo)
    {
        if (vehiculo == null || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert("Confirmar", $"¿Estás seguro de que deseas eliminar el microbús {vehiculo.Placa} de tu flota?", "Sí, eliminar", "Cancelar");
        if (!confirmar) return;

        IsLoading = true;
        bool exito = await _supabaseService.DeleteVehicleAsync(vehiculo.Id);
        IsLoading = false;

        if (exito)
        {
            await LoadFleetDataAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo eliminar el microbús. Es posible que tenga viajes asignados en el historial.", "OK");
        }
    }
}