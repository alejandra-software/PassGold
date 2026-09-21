using Microsoft.Maui.ApplicationModel.Communication;
using PassGold.Helpers;
using PassGold.ViewModels;

namespace PassGold.Views;

public partial class PasajeroDetallePage : ContentPage
{
    private readonly string _telefono;
    private readonly string _fotoUrl;

    public PasajeroDetallePage(PasajeroItem pasajero)
    {
        InitializeComponent();

        NombreLabel.Text = pasajero.Nombre;
        UbicacionLabel.Text = pasajero.Ubicacion;
        _telefono = pasajero.Telefono ?? "";
        _fotoUrl = pasajero.FotoUrl ?? "";

        bool tieneFoto = !string.IsNullOrWhiteSpace(_fotoUrl);
        FotoGrande.IsVisible = tieneFoto;
        IconoRespaldo.IsVisible = !tieneFoto;
        if (tieneFoto) FotoGrande.Source = _fotoUrl;

        bool tieneTelefono = !string.IsNullOrWhiteSpace(_telefono);
        TelefonoCard.IsVisible = tieneTelefono;
        SinTelefonoLabel.IsVisible = !tieneTelefono;
        if (tieneTelefono) TelefonoLabel.Text = _telefono;
    }

    private async void CerrarBtn_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Navigation.PopModalAsync();
        return true;
    }

    private async void Foto_Tapped(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_fotoUrl))
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(_fotoUrl));
        }
    }

    private async void Llamar_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_telefono)) return;

        try
        {
            PhoneDialer.Default.Open(_telefono);
        }
        catch
        {
            // No hay marcador disponible en este dispositivo (poco común, pero
            // puede pasar en emuladores o tablets sin función de llamada).
            await DisplayAlert(AppResources.Global_Error, AppResources.PassengerDetail_CallError, AppResources.Global_Ok);
        }
    }
}