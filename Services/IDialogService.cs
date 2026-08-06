using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName);
    Task<string?> ShowOpenFileDialogAsync(string title, string filter);
}
