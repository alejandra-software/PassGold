using System;
using System.Threading.Tasks;
using PassGold.Services;
using PassGold.ViewModels;
using PassGold.Models.Local;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PassGold;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly LocalDatabaseService _localDb;

    public App(LocalDatabaseService localDb)
    {
        InitializeComponent();
        _localDb = localDb;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        window.Created += (s, e) =>
        {
            Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await InitializarSesionAsync();
            });
        };

        return window;
    }

    private async Task InitializarSesionAsync()
    {
        try
        {
            var sesionLocal = await _localDb.ObtenerSesionActivaAsync();

            if (sesionLocal != null && !string.IsNullOrEmpty(sesionLocal.Rol))
            {
                await AppShellViewModel.Instance.UpdateMenuStateAsync();

                // �Y"� LA SOLUCI�"N AL CONGELAMIENTO INFINITO:
                // Verificamos de forma segura sin generar choques de procesamiento.
                if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
                {
                    await SupabaseService.Instance.IsSessionActiveAsync();
                }

                string rol = sesionLocal.Rol.ToLower();
                if (rol.Contains("chofer") || rol.Contains("driver"))
                    await Shell.Current.GoToAsync("//driver-dashboard");
                else
                    await Shell.Current.GoToAsync("//passenger-dashboard");

                return;
            }

            if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
            {
                AppShellViewModel.Instance.ResetMenu();
                await Shell.Current.GoToAsync("//login");
                return;
            }

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
                        await _localDb.GuardarSesionActivaAsync(new UsuarioLocal
                        {
                            Id = datos.Id,
                            Email = datos.Email,
                            Nombre = datos.Nombre,
                            Rol = datos.Rol,
                            FotoPerfil = datos.FotoPerfil,
                            IdJefe = datos.IdJefe
                        });

                        string rol = datos.Rol.ToLower();
                        if (rol.Contains("chofer") || rol.Contains("driver"))
                            await Shell.Current.GoToAsync("//driver-dashboard");
                        else
                            await Shell.Current.GoToAsync("//passenger-dashboard");
                    }
                    else
                    {
                        AppShellViewModel.Instance.ResetMenu();
                        await Shell.Current.GoToAsync("//register-step2");
                    }
                }
            }
            else
            {
                AppShellViewModel.Instance.ResetMenu();
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"�O Error de navegación: {ex.Message}");
            AppShellViewModel.Instance.ResetMenu();
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//login");
        }
    }
}
