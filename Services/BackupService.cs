using MetalBayalaGestion.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private readonly string _dbPath;

    public BackupService(AppDbContext context)
    {
        _context = context;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalBayalaGestion");
        _dbPath = Path.Combine(folder, "metalbayala.db");
    }

    public string GetDatabasePath() => _dbPath;

    public async Task<bool> BackupAsync(string destinationPath)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("VACUUM;");
            File.Copy(_dbPath, destinationPath, true);
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RestoreAsync(string backupPath)
    {
        try
        {
            _context.Database.CloseConnection();
            File.Copy(backupPath, _dbPath, true);
            _context.Database.OpenConnection();
            await _context.Database.MigrateAsync();
            return true;
        }
        catch { return false; }
    }
}
