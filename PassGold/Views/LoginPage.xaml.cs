using PassGold.ViewModels;
using Microsoft.Maui.Controls;

namespace PassGold.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = AuthViewModel.Instance;
    }
}