using GoldeenRide.ViewModels;
using GoldeenRide.Views;
using GoldeenRide.Services;
using System;
using Microsoft.Maui.Controls;

namespace GoldeenRide;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        BindingContext = AppShellViewModel.Instance;

        Routing.RegisterRoute("schedule-trip", typeof(ScheduleTripPage));
        Routing.RegisterRoute("trip-details", typeof(TripDetailsPage));
        Routing.RegisterRoute("admin-fleet", typeof(AdminFleetPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("add-vehicle", typeof(AddVehiclePage));
        Routing.RegisterRoute("active-trip", typeof(ActiveTripPage));
        Routing.RegisterRoute("edit-trip", typeof(EditTripPage));
    }

    private async void MenuNavegar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string ruta)
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