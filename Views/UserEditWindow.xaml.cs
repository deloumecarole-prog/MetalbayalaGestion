using MetalBayalaGestion.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MetalBayalaGestion.Views;

public partial class UserEditWindow : Window
{
    private UserEditViewModel _viewModel = null!;

    public UserEditWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            _viewModel = (UserEditViewModel)DataContext;
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PlainPassword = ((PasswordBox)sender).Password;
    }

    private void ConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
    }
}
