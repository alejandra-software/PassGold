using PassGold.ViewModels;

namespace PassGold.Views;

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
            vm.LoadProfileCommand.Execute(null);
        }
    }

    private async void FotoPerfil_Tapped(object sender, EventArgs e)
    {
        if (BindingContext is SettingsViewModel vm && vm.HasUserPhoto)
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(vm.UserPhotoUrl));
        }
    }

    private async void FotoPerfilEditable_Tapped(object sender, EventArgs e)
    {
        if (BindingContext is SettingsViewModel vm && vm.HasEditableFoto)
        {
            await Navigation.PushModalAsync(new PhotoViewerPage(vm.EditableFotoUrl));
        }
    }
}