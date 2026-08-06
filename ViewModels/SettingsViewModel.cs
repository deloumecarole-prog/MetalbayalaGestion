using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MetalBayalaGestion.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private Company _company = new();

    [ObservableProperty]
    private string _dbPath = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly IBackupService _backupService;

    public SettingsViewModel(AppDbContext context, IDialogService dialogService, IBackupService backupService)
    {
        _context = context;
        _dialogService = dialogService;
        _backupService = backupService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        Company = _context.Companies.FirstOrDefault() ?? new Company();
        DbPath = _backupService.GetDatabasePath();
    }

    [RelayCommand]
    private async Task SaveCompany()
    {
        _context.Companies.Update(Company);
        await _context.SaveChangesAsync();
        await _dialogService.ShowInfoAsync("Succès", "Paramètres enregistrés.");
    }

    [RelayCommand]
    private async Task BackupDatabase()
    {
        var path = await _dialogService.ShowSaveFileDialogAsync("Sauvegarder la base", "SQLite DB|*.db", $"metalbayala_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        if (path == null) return;
        var success = await _backupService.BackupAsync(path);
        if (success)
            await _dialogService.ShowInfoAsync("Succès", "Sauvegarde effectuée.");
        else
            await _dialogService.ShowErrorAsync("Erreur", "Échec de la sauvegarde.");
    }

    [RelayCommand]
    private async Task RestoreDatabase()
    {
        if (!await _dialogService.ShowConfirmAsync("Attention", "La restauration remplacera toutes les données actuelles. Continuer ?")) return;
        var path = await _dialogService.ShowOpenFileDialogAsync("Restaurer une sauvegarde", "SQLite DB|*.db");
        if (path == null) return;
        var success = await _backupService.RestoreAsync(path);
        if (success)
            await _dialogService.ShowInfoAsync("Succès", "Restauration effectuée. Veuillez redémarrer l'application.");
        else
            await _dialogService.ShowErrorAsync("Erreur", "Échec de la restauration.");
    }
}
