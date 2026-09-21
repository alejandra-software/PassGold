using PassGold.ViewModels;

namespace PassGold.Views;

public partial class AddVehiclePage : ContentPage
{
    public AddVehiclePage()
    {
        InitializeComponent();
    }

    private async void VerFotoGrande_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is AddVehicleViewModel vm && vm.HasPhoto)
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(vm.FotoUrl));
        }
    }
}