using GoldeenRide.ViewModels;

namespace GoldeenRide.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
        {
            // ✅ Le quitamos la palabra "Async" al comando
            vm.LoadProfileCommand.Execute(null);
        }
    }
}