using PassGold.ViewModels;
using PassGold.Views;
using PassGold.Services;
using System;
using Microsoft.Maui.Controls;

namespace PassGold;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        BindingContext = AppShellViewModel.Instance;

        // --- Rutas Generales y del Chofer ---
        Routing.RegisterRoute("schedule-trip", typeof(ScheduleTripPage));
        Routing.RegisterRoute("trip-details", typeof(TripDetailsPage));
        Routing.RegisterRoute("admin-fleet", typeof(AdminFleetPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("add-vehicle", typeof(AddVehiclePage));
        Routing.RegisterRoute("active-trip", typeof(ActiveTripPage));
        Routing.RegisterRoute("edit-trip", typeof(EditTripPage));
        Routing.RegisterRoute("edit-vehicle", typeof(EditVehiclePage));

        // --- Rutas del Pasajero y Perfil de Flotas ---
        Routing.RegisterRoute("passenger-rules", typeof(PassengerRulesPage));
        Routing.RegisterRoute("fleet-profile", typeof(FleetProfilePage));
        Routing.RegisterRoute("fleet-schedule", typeof(FleetSchedulePage));
        Routing.RegisterRoute("pasajero-reserva", typeof(PasajeroReservaPage));
        Routing.RegisterRoute("fleet-directory", typeof(FleetDirectoryPage));
        Routing.RegisterRoute("mapa-pasajero", typeof(Views.MapaRastreoPage));

        // --- Nuevas rutas añadidas (Mayúsculas corregidas) ---
        Routing.RegisterRoute("pasajero-detalle", typeof(PasajeroDetallePage));
        Routing.RegisterRoute("set-home-location", typeof(SetHomeLocationPage));
    }

    private async void MenuNavegar_Clicked(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.Button btn && btn.CommandParameter is string ruta)
        {
            Current.FlyoutIsPresented = false;
            await Current.GoToAsync(ruta);
        }
    }

    private async void MenuCerrarSesion_Clicked(object sender, EventArgs e)
    {
        Current.FlyoutIsPresented = false;
        await SupabaseService.Instance.LogoutAsync();
        AppShellViewModel.Instance.ResetMenu();
        await Current.GoToAsync("//login");
    }
}