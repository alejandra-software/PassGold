// Evita el choque con Android.App.Application al compilar para Android
using Application = Microsoft.Maui.Controls.Application;
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
using Microsoft.Maui.ApplicationModel;

namespace PassGold.ViewModels;

public class AdminCachePayload
{
    public bool IsBoss { get; set; }
    public bool IsFleetManager { get; set; }
    public bool IsEmployee { get; set; }
    public string FleetCode { get; set; } = AppResources.Global_Loading;
    public string BossName { get; set; } = "";
    public bool HasJoinedFleet { get; set; }
    public List<Vehiculo> Microbuses { get; set; } = new();
    public List<Usuario> Choferes { get; set; } = new();
    public string NombreFlota { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string DescripcionFlota { get; set; } = "";
    public string RutasFlota { get; set; } = "";
}

// 🆕 Un empleado en la lista "Mis choferes", con su interruptor de sub-jefe.
public partial class EmpleadoFlotaItem : ObservableObject
{
    public Usuario Usuario { get; }
    public string Id => Usuario.Id;
    public string Nombre => Usuario.Nombre;
    public string Email => Usuario.Email;
    public string? FotoPerfil => Usuario.FotoPerfil;

    [ObservableProperty] private bool puedeGestionar;

    // La pantalla avisa al ViewModel cuando el jefe mueve el interruptor
    internal System.Func<EmpleadoFlotaItem, bool, Task>? AlCambiarPermiso;
    private bool _silencio;

    public EmpleadoFlotaItem(Usuario u)
    {
        Usuario = u;
        _silencio = true;
        PuedeGestionar = u.PuedeGestionarViajes;
        _silencio = false;
    }

    // Para regresar el interruptor sin volver a llamar a Supabase (si falló)
    internal void PonerSinAvisar(bool valor)
    {
        _silencio = true;
        PuedeGestionar = valor;
        _silencio = false;
    }

    partial void OnPuedeGestionarChanged(bool value)
    {
        if (_silencio) return;
        Usuario.PuedeGestionarViajes = value;
        if (AlCambiarPermiso != null) _ = AlCambiarPermiso(this, value);
    }
}

public partial class AdminFleetViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    private LocalDatabaseService GetLocalDb() =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();

    public ObservableCollection<Vehiculo> MisMicrobuses { get; } = new();
    public ObservableCollection<EmpleadoFlotaItem> MisChoferes { get; } = new();

    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private bool hasNoEmployees = false;
    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isBoss = false;
    [ObservableProperty] private string fleetCode = AppResources.Global_Loading;

    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private string joinCodeInput = "";
    [ObservableProperty] private string joinErrorMessage = "";
    [ObservableProperty] private string bossName = "";
    [ObservableProperty] private bool hasJoinedFleet = false;

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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoading = true;
            JoinErrorMessage = string.Empty;
        });

        var localDb = GetLocalDb();
        string currentUserId = "";

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
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
            return;
        }
        _currentUserId = currentUserId;

        string cacheKey = $"AdminFleetData_{currentUserId}";

        try
        {
            await Task.Delay(100);

            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            if (!string.IsNullOrEmpty(cacheJson))
            {
                var payload = JsonConvert.DeserializeObject<AdminCachePayload>(cacheJson);
                if (payload != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => PintarDatosEnPantalla(payload));
                }
            }

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
                                nuevoPayload.FleetCode = string.IsNullOrWhiteSpace(codigo) ? AppResources.AdminFleet_CodeError : codigo;
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

                        MainThread.BeginInvokeOnMainThread(() =>
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
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
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
        foreach (var e in payload.Choferes)
            MisChoferes.Add(new EmpleadoFlotaItem(e) { AlCambiarPermiso = CambiarPermisoAsync });

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

        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        try
        {
            if (_currentUserData == null && !string.IsNullOrEmpty(_currentUserId))
                _currentUserData = await _supabaseService.GetUserDataAsync(_currentUserId);

            if (_currentUserData == null)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    IsLoading = false;
                    await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsSessionError, AppResources.Global_Ok);
                });
                return;
            }

            _currentUserData.NombreFlota = NombreFlotaInput.Trim();
            _currentUserData.Telefono = TelefonoInput.Trim();
            _currentUserData.DescripcionFlota = DescripcionInput.Trim();
            _currentUserData.RutasFlota = string.Join(", ", RutasTags);

            await _supabaseService.UpdateUserDataAsync(_currentUserData);

            MainThread.BeginInvokeOnMainThread(async () => {
                IsLoading = false;
                await Shell.Current.DisplayAlert(AppResources.SettingsUpdatedTitle, AppResources.SettingsUpdatedMsg, AppResources.Global_Ok);
            });
        }
        catch (System.Exception)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                IsLoading = false;
                await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SettingsSaveError, AppResources.Global_Ok);
            });
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
            AppResources.AdminFleet_RegenerateTitle,
            AppResources.AdminFleet_RegenerateConfirm,
            AppResources.AdminFleet_YesChange, AppResources.Global_Cancel);

        if (!confirmar) return;

        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        var localDb = GetLocalDb();
        var sesionLocal = await localDb.ObtenerSesionActivaAsync();
        string currentUserId = _supabaseService.GetCurrentUser()?.Id ?? sesionLocal?.Id ?? "";

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var nuevoCodigo = await _supabaseService.RegenerateFleetCodeAsync(currentUserId);

            MainThread.BeginInvokeOnMainThread(async () => {
                IsLoading = false;
                if (!string.IsNullOrEmpty(nuevoCodigo))
                {
                    FleetCode = nuevoCodigo;
                    await Shell.Current.DisplayAlert(AppResources.Global_Success, AppResources.AdminFleet_CodeUpdated, AppResources.Global_Ok);
                }
                else
                {
                    await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.AdminFleet_RegenerateError, AppResources.Global_Ok);
                }
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public async Task JoinFleetAsync()
    {
        if (string.IsNullOrWhiteSpace(JoinCodeInput))
        {
            MainThread.BeginInvokeOnMainThread(() => JoinErrorMessage = AppResources.Error_EmptyList);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => {
            IsLoading = true;
            JoinErrorMessage = string.Empty;
        });

        var localDb = GetLocalDb();
        var sesionLocal = await localDb.ObtenerSesionActivaAsync();
        string currentUserId = _supabaseService.GetCurrentUser()?.Id ?? sesionLocal?.Id ?? "";

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var (success, message) = await _supabaseService.JoinFleetByCodeAsync(JoinCodeInput, currentUserId);

            MainThread.BeginInvokeOnMainThread(async () => {
                if (success)
                {
                    if (Shell.Current != null)
                        await Shell.Current.DisplayAlert(AppResources.Global_Ok, message, AppResources.Global_Ok);

                    JoinCodeInput = string.Empty;
                    await LoadFleetDataAsync();
                }
                else
                {
                    JoinErrorMessage = message;
                    IsLoading = false;
                }
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
        }
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
            ? AppResources.F(AppResources.AdminFleet_DeactivateBusConfirm, vehiculo.Placa)
            : AppResources.F(AppResources.AdminFleet_ReactivateBusConfirm, vehiculo.Placa);

        bool confirmar = await Shell.Current.DisplayAlert(AppResources.Global_Confirm, pregunta, AppResources.Global_Yes, AppResources.Global_Cancel);
        if (!confirmar) return;

        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        bool exito = estaActivo
            ? await _supabaseService.DeactivateVehiculoAsync(vehiculo.Id)
            : await _supabaseService.ReactivateVehiculoAsync(vehiculo.Id);

        MainThread.BeginInvokeOnMainThread(async () => {
            IsLoading = false;
            if (exito) await LoadFleetDataAsync();
            else await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.AdminFleet_BusUpdateError, AppResources.Global_Ok);
        });
    }

    // 🆕 El jefe da o quita a un empleado el permiso de crear y editar viajes
    private async Task CambiarPermisoAsync(EmpleadoFlotaItem item, bool permitir)
    {
        if (Shell.Current == null) return;

        if (permitir)
        {
            bool confirmar = await Shell.Current.DisplayAlert(
                AppResources.SubBoss_ConfirmTitle,
                AppResources.F(AppResources.SubBoss_ConfirmMsg, item.Nombre),
                AppResources.SubBoss_ConfirmYes, AppResources.Global_Cancel);
            if (!confirmar) { item.PonerSinAvisar(false); return; }
        }

        bool ok = Connectivity.Current.NetworkAccess == NetworkAccess.Internet
                  && await _supabaseService.FijarPermisoViajesAsync(item.Id, permitir);

        if (!ok)
        {
            item.PonerSinAvisar(!permitir);
            await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.SubBoss_Error, AppResources.Global_Ok);
            return;
        }

        // Que la caché de esta pantalla quede al día
        try
        {
            var localDb = GetLocalDb();
            string cacheKey = $"AdminFleetData_{_currentUserId}";
            string? cacheJson = await localDb.LeerCacheAsync(cacheKey);
            var payload = string.IsNullOrEmpty(cacheJson) ? null : JsonConvert.DeserializeObject<AdminCachePayload>(cacheJson);
            var emp = payload?.Choferes.Find(c => c.Id == item.Id);
            if (payload != null && emp != null)
            {
                emp.PuedeGestionarViajes = permitir;
                await localDb.GuardarCacheAsync(cacheKey, JsonConvert.SerializeObject(payload));
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task RemoverEmpleadoAsync(EmpleadoFlotaItem item)
    {
        var empleado = item?.Usuario;
        if (empleado == null || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert(
            AppResources.Global_Confirm,
            AppResources.F(AppResources.AdminFleet_RemoveEmployeeConfirm, empleado.Nombre),
            AppResources.AdminFleet_YesRemoveEmployee, AppResources.Global_Cancel);
        if (!confirmar) return;

        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        bool exito = await _supabaseService.RemoveEmployeeFromFleetAsync(empleado.Id);

        MainThread.BeginInvokeOnMainThread(async () => {
            IsLoading = false;
            if (exito) await LoadFleetDataAsync();
            else await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.AdminFleet_RemoveEmployeeError, AppResources.Global_Ok);
        });
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

        bool confirmar = await Shell.Current.DisplayAlert(AppResources.Global_Confirm, AppResources.F(AppResources.AdminFleet_DeleteBusConfirm, vehiculo.Placa), AppResources.AdminFleet_YesDelete, AppResources.Global_Cancel);
        if (!confirmar) return;

        MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

        bool exito = await _supabaseService.DeleteVehicleAsync(vehiculo.Id);

        MainThread.BeginInvokeOnMainThread(async () => {
            IsLoading = false;
            if (exito)
            {
                await LoadFleetDataAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert(AppResources.Global_Error, AppResources.AdminFleet_DeleteBusError, AppResources.Global_Ok);
            }
        });
    }
}