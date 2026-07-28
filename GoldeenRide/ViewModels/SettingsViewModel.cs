using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Models;
using GoldeenRide.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System;

namespace GoldeenRide.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService = SupabaseService.Instance;

    [ObservableProperty] private string userName = "Cargando...";
    [ObservableProperty] private string userRole = "";

    [ObservableProperty] private bool isFleetManager = false;
    [ObservableProperty] private bool isEmployee = false;
    [ObservableProperty] private bool hasBoss = false;

    [ObservableProperty] private string myFleetCode = "Cargando...";
    [ObservableProperty] private string inputFleetCode = "";
    [ObservableProperty] private string currentBossName = "";

    [ObservableProperty] private bool isLoading = false;

    private Usuario? _currentUserData;

    public SettingsViewModel()
    {
        _ = LoadProfileAsync();
    }

    // Helper para sacar las traducciones desde el código C#
    private string GetTranslation(string key)
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var value))
            return value?.ToString() ?? key;
        return key;
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        IsLoading = true;
        var authUser = _supabaseService.GetCurrentUser();
        if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
        {
            _currentUserData = await _supabaseService.GetUserDataAsync(authUser.Id);
            if (_currentUserData != null)
            {
                UserName = _currentUserData.Nombre;
                UserRole = _currentUserData.Rol;

                string rolLower = UserRole.ToLower();
                IsFleetManager = rolLower.Contains("jefe") || rolLower.Contains("owner");
                IsEmployee = rolLower.Contains("empleado");

                if (IsFleetManager)
                {
                    // ✅ Ya NO mostramos el UUID. Se genera (o recupera) un código
                    // corto de 6 caracteres, amigable para compartir de palabra o por WhatsApp.
                    MyFleetCode = await _supabaseService.GetOrCreateFleetCodeAsync(_currentUserData.Id)
                                   ?? "Error generando código";
                }

                if (IsEmployee && !string.IsNullOrEmpty(_currentUserData.IdJefe))
                {
                    HasBoss = true;
                    var jefe = await _supabaseService.GetUserDataAsync(_currentUserData.IdJefe);
                    CurrentBossName = jefe?.Nombre ?? "Jefe Desconocido";
                }
            }
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task CopyFleetCodeAsync()
    {
        await Clipboard.Default.SetTextAsync(MyFleetCode);
        if (Shell.Current != null)
            await Shell.Current.DisplayAlertAsync(
                GetTranslation("AlertCopiedTitle"),
                GetTranslation("AlertCopiedMsg"),
                GetTranslation("AlertOk"));
    }

    // 🔥 NUEVO: Regenerar el código por seguridad (ej. si sospechas que se filtró).
    // Sin caducidad automática — el jefe decide cuándo cambiarlo con este botón.
    [RelayCommand]
    public async Task RegenerateFleetCodeAsync()
    {
        if (_currentUserData == null || Shell.Current == null) return;

        bool confirmar = await Shell.Current.DisplayAlert(
            "Regenerar código",
            "El código actual dejará de funcionar de inmediato para cualquiera que intente unirse con él. Los empleados que ya están en tu flota NO se ven afectados. ¿Continuar?",
            "Sí, regenerar", "Cancelar");
        if (!confirmar) return;

        IsLoading = true;
        string? nuevoCodigo = await _supabaseService.RegenerateFleetCodeAsync(_currentUserData.Id);
        IsLoading = false;

        if (nuevoCodigo != null)
        {
            MyFleetCode = nuevoCodigo;
            await Shell.Current.DisplayAlert("Listo", $"Tu nuevo código es: {nuevoCodigo}", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo regenerar el código. Intenta de nuevo.", "OK");
        }
    }

    [RelayCommand]
    public async Task JoinFleetAsync()
    {
        if (string.IsNullOrWhiteSpace(InputFleetCode))
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("Atención", "Ingresa el código que te dio tu jefe.", "OK");
            return;
        }

        if (_currentUserData == null || Shell.Current == null) return;

        IsLoading = true;

        // Limpiar el código de espacios/saltos de línea invisibles.
        // El servicio ya normaliza a mayúsculas, así que no importa cómo lo escriba el usuario.
        string codigoLimpio = InputFleetCode.Trim();

        var (success, message) = await _supabaseService.JoinFleetByCodeAsync(codigoLimpio, _currentUserData.Id);

        if (success)
        {
            InputFleetCode = string.Empty;
            await Shell.Current.DisplayAlertAsync("¡Bienvenido!", message, "OK");
            // Recargamos el perfil para que la UI se actualice y muestre "Trabajando en la flota de..."
            await LoadProfileAsync();
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error de vinculación", message, "Cerrar");
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        if (Shell.Current == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            GetTranslation("AlertLogoutTitle"),
            GetTranslation("AlertLogoutMsg"),
            GetTranslation("AlertYesExit"),
            GetTranslation("AlertCancel"));

        if (confirm)
        {
            IsLoading = true;
            await _supabaseService.LogoutAsync();
            IsLoading = false;

            AppShellViewModel.Instance.ResetMenu(); // Ocultamos el menú al salir
            await Shell.Current.GoToAsync("///login");
        }
    }
}