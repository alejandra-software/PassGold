using GoldeenRide.ViewModels;

namespace GoldeenRide.Views;

public partial class DriverDashboardPage : ContentPage
{
    public DriverDashboardPage()
    {
        InitializeComponent();
        BindingContext = new DriverDashboardViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DriverDashboardViewModel vm)
        {
            await vm.LoadDriverDataCommand.ExecuteAsync(null);
        }
    }
}