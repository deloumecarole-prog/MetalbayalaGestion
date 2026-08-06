using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;

namespace MetalBayalaGestion.Services;

public class DialogService : IDialogService
{
    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        return Task.FromResult(result);
    }

    public Task ShowInfoAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName)
    {
        var dlg = new SaveFileDialog { Title = title, Filter = filter, FileName = defaultFileName };
        return Task.FromResult(dlg.ShowDialog() == true ? dlg.FileName : null);
    }

    public Task<string?> ShowOpenFileDialogAsync(string title, string filter)
    {
        var dlg = new OpenFileDialog { Title = title, Filter = filter };
        return Task.FromResult(dlg.ShowDialog() == true ? dlg.FileName : null);
    }
}
