using PassGold.ViewModels;

namespace PassGold.Views;

public partial class RegisterStep1Page : ContentPage
{
    public RegisterStep1Page()
    {
        InitializeComponent();
        BindingContext = AuthViewModel.Instance;
    }
}
