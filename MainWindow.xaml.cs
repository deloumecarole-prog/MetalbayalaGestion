using MetalBayalaGestion.ViewModels;
using System.Windows;

namespace MetalBayalaGestion;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
