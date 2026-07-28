using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldeenRide.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GoldeenRide.ViewModels;

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

    // ✅ NUEVO: propiedad calculada. La usamos en el flyout para mostrar
    // "Administrar Flota / Mis Vehículos" a cualquiera que tenga vehículos propios
    // (jefe o independiente), pero NUNCA a empleado ni pasajero.
    public bool CanManageFleet => IsJefe || IsIndependiente;

    [ObservableProperty] private FlyoutBehavior flyoutState = FlyoutBehavior.Disabled;

    public async Task UpdateMenuStateAsync()
    {
        var user = SupabaseService.Instance.GetCurrentUser();
        if (user != null && !string.IsNullOrEmpty(user.Id))
        {
            UserEmail = user.Email ?? "";
            var data = await SupabaseService.Instance.GetUserDataAsync(user.Id!);

            if (data != null)
            {
                UserName = data.Nombre;
                UserRole = data.Rol.ToUpper();
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
        // ✅ FIX: antes siempre mandaba a driver-dashboard, incluso a pasajeros.
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
