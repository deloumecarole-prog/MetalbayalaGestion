using MetalBayalaGestion.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MetalBayalaGestion.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel _viewModel = null!;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.Password = ((PasswordBox)sender).Password;
    }
}
