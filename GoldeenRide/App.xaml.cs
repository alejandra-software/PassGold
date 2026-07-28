using System;
using System.Threading.Tasks;
using GoldeenRide.Services;
using GoldeenRide.ViewModels;
using Microsoft.Maui.Controls;

namespace GoldeenRide;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        window.Created += async (s, e) =>
        {
            await InitializarSesionAsync();
        };

        return window;
    }

    private async Task InitializarSesionAsync()
    {
        try
        {
            bool sesionActiva = await SupabaseService.Instance.IsSessionActiveAsync();
            if (sesionActiva && Shell.Current != null)
            {
                var usuario = SupabaseService.Instance.GetCurrentUser();
                if (usuario != null && !string.IsNullOrEmpty(usuario.Id))
                {
                    var datos = await SupabaseService.Instance.GetUserDataAsync(usuario.Id!);
                    await AppShellViewModel.Instance.UpdateMenuStateAsync();

                    if (datos != null && !string.IsNullOrEmpty(datos.Rol))
                    {
                        string rol = datos.Rol.ToLower();

                        if (rol.Contains("chofer") || rol.Contains("driver"))
                        {
                            await Shell.Current.GoToAsync("//driver-dashboard");
                        }
                        else if (rol.Contains("pasajero") || rol.Contains("passenger"))
                        {
                            await Shell.Current.GoToAsync("//passenger-dashboard");
                        }
                    }
                    else
                    {
                        // 🟢 FIX: Es un usuario de Google que aún no tiene rol.
                        // Apagamos el menú de 3 rayas y lo encerramos en el paso 2.
                        AppShellViewModel.Instance.ResetMenu();
                        await Shell.Current.GoToAsync("//register-step2");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error de navegación: {ex.Message}");
        }
    }
}