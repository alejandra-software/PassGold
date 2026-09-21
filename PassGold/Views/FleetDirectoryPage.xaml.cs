using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class FleetDirectoryPage : ContentPage
{
    public FleetDirectoryPage()
    {
        InitializeComponent();
    }

    // FIX: faltaba disparar la carga de flotas al entrar a la pagina — el ViewModel
    // tenia LoadDirectoryAsync listo, pero nada lo llamaba, por eso nunca se veia nada.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FleetDirectoryViewModel vm)
        {
            vm.LoadDirectoryCommand.Execute(null);
        }
    }
}