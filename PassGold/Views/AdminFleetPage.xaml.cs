namespace PassGold.Views;

public partial class AdminFleetPage : ContentPage
{
    public AdminFleetPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.AdminFleetViewModel vm)
        {
            //  Le quitamos la palabra "Async" al comando
            vm.LoadFleetDataCommand.Execute(null);
        }
    }
}
