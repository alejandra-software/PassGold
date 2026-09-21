using PassGold.ViewModels;

namespace PassGold.Views;

public partial class ActiveTripPage : ContentPage
{
    public ActiveTripPage()
    {
        InitializeComponent();
    }

    private async void FotoPasajero_Tapped(object sender, EventArgs e)
    {
        if (sender is Element el && el.BindingContext is PasajeroAbordaje pasajero && pasajero.TieneFoto)
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(pasajero.FotoUrl!));
        }
    }
}