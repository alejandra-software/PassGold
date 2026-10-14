using PassGold.ViewModels;

namespace PassGold.Views;

public partial class FleetSchedulePage : ContentPage
{
    public FleetSchedulePage()
    {
        InitializeComponent();
        BindingContext = new FleetScheduleViewModel();
    }

    // Al volver de confirmar la reserva, la agenda se recarga para mostrar los ✅
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FleetScheduleViewModel vm) vm.RecargarSiHaceFalta();
    }
}