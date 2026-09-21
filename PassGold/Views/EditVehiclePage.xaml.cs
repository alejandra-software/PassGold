using PassGold.ViewModels;

namespace PassGold.Views;

public partial class EditVehiclePage : ContentPage
{
    public EditVehiclePage()
    {
        InitializeComponent();
    }

    private async void VerFotoGrande_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is EditVehicleViewModel vm && vm.HasPhoto)
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(vm.FotoUrl));
        }
    }
}