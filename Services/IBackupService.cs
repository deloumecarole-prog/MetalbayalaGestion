using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public interface IBackupService
{
    Task<bool> BackupAsync(string destinationPath);
    Task<bool> RestoreAsync(string backupPath);
    string GetDatabasePath();
}
