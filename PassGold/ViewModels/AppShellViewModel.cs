using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PassGold.Services;
using PassGold.Models.Local;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace PassGold.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    private static AppShellViewModel? _instance;
    public static AppShellViewModel Instance => _instance ??= new AppShellViewModel();

    [ObservableProperty] private bool isLogged = false;
    [ObservableProperty] private string userName = "Cargando...";
    [ObservableProperty] private string userRole = "";
    [ObservableProperty] private string userEmail = "";

    [ObservableProperty] private bool isJefe = false;
    [ObservableProperty] private bool isEmpleado = false;
    [ObservableProperty] private bool isIndependiente = false;
    [ObservableProperty] private bool isPasajero = false;

    public bool CanManageFleet => IsJefe || IsIndependiente;

    [ObservableProperty] private FlyoutBehavior flyoutState = FlyoutBehavior.Disabled;

    public async Task UpdateMenuStateAsync()
    {
        // ?? LA SOLUCI�N AL MEN� INVISIBLE:
        // Leemos el rol desde SQLite (instant�neo y offline) en lugar de depender del internet.
        var localDb = Application.Current?.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>() ?? new LocalDatabaseService();
        var localUser = await localDb.ObtenerSesionActivaAsync();

        if (localUser != null && !string.IsNullOrEmpty(localUser.Rol))
        {
            UserName = localUser.Nombre;
            UserRole = localUser.Rol.ToUpper();
            UserEmail = localUser.Email ?? "";

            string rolLower = localUser.Rol.ToLower();
            IsJefe = rolLower.Contains("jefe") || rolLower.Contains("owner");
            IsEmpleado = rolLower.Contains("empleado");
            IsIndependiente = rolLower.Contains("independiente") || rolLower.Contains("independent");
            IsPasajero = rolLower.Contains("pasajero") || rolLower.Contains("passenger");

            OnPropertyChanged(nameof(CanManageFleet));

            IsLogged = true;
            FlyoutState = FlyoutBehavior.Flyout; // ENCIENDE EL MEN� DE INMEDIATO
            return;
        }

        // Fallback a la nube por si es una instalaci�n completamente nueva
        var user = SupabaseService.Instance.GetCurrentUser();
        if (user != null && !string.IsNullOrEmpty(user.Id))
        {
            var data = await SupabaseService.Instance.GetUserDataAsync(user.Id!);
            if (data != null)
            {
                UserName = data.Nombre;
                UserRole = data.Rol.ToUpper();
                UserEmail = user.Email ?? "";
                string rolLower = data.Rol.ToLower();

                IsJefe = rolLower.Contains("jefe") || rolLower.Contains("owner");
                IsEmpleado = rolLower.Contains("empleado");
                IsIndependiente = rolLower.Contains("independiente") || rolLower.Contains("independent");
                IsPasajero = rolLower.Contains("pasajero") || rolLower.Contains("passenger");

                OnPropertyChanged(nameof(CanManageFleet));

                IsLogged = true;
                FlyoutState = FlyoutBehavior.Flyout;
                return;
            }
        }
        ResetMenu();
    }

    public void ResetMenu()
    {
        IsLogged = false; IsJefe = false; IsEmpleado = false; IsIndependiente = false; IsPasajero = false;
        FlyoutState = FlyoutBehavior.Disabled;
        UserName = "Invitado"; UserRole = ""; UserEmail = "";
        OnPropertyChanged(nameof(CanManageFleet));
    }

    [RelayCommand]
    public async Task GoToHomeAsync()
    {
        Shell.Current.FlyoutIsPresented = false;
        string destino = IsPasajero ? "///passenger-dashboard" : "///driver-dashboard";
        await Shell.Current.GoToAsync(destino);
    }

    [RelayCommand]
    public async Task GoToSettingsAsync()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("settings");
    }

    [RelayCommand]
    public async Task GoToAdminFleetAsync()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("admin-fleet");
    }

    [RelayCommand]
    public async Task GoToScheduleTripAsync()
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("schedule-trip");
    }
}
