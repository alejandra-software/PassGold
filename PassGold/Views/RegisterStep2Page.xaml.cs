using PassGold.ViewModels;

namespace PassGold.Views;

public partial class RegisterStep2Page : ContentPage
{
    public RegisterStep2Page()
    {
        InitializeComponent();
        BindingContext = AuthViewModel.Instance;
    }
}
