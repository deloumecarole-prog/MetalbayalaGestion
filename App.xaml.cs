using MetalBayalaGestion.Data;
using MetalBayalaGestion.Services;
using MetalBayalaGestion.ViewModels;
using MetalBayalaGestion.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace MetalBayalaGestion;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Capture toute exception non geree pour diagnostic
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            LogCrash(args.ExceptionObject as Exception, "AppDomain");
        };
        DispatcherUnhandledException += (s, args) =>
        {
            LogCrash(args.Exception, "Dispatcher");
            // Evite qu'une sauvegarde en echec ne "pollue" les ecrans suivants
            // pour le reste de la session (voir MainViewModel.ClearChangeTrackerAfterError).
            try { ServiceProvider?.GetService<MainViewModel>()?.ClearChangeTrackerAfterError(); } catch { }
            MessageBox.Show($"Erreur :\n\n{BuildErrorText(args.Exception)}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // evite le crash total pour pouvoir lire le message
        };

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalBayalaGestion");
            Directory.CreateDirectory(folder);
            var dbPath = Path.Combine(folder, "metalbayala.db");

            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var withDemo = e.Args.Contains("--demo");
            DbInitializer.Initialize(context, withDemo);

            // Fenetre de connexion
            var loginVm = scope.ServiceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);
            if (loginWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            LogCrash(ex, "OnStartup");
            MessageBox.Show($"Erreur au demarrage :\n\n{BuildErrorText(ex)}", "Erreur fatale", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalBayalaGestion");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "metalbayala.db");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INumberingService, NumberingService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
    }

    // Construit un message d'erreur complet en remontant toute la chaine
    // d'InnerException, indispensable pour les erreurs EF Core / SQLite qui
    // cachent la vraie cause dans l'InnerException plutot que dans ex.Message.
    private static string BuildErrorText(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        int level = 0;
        while (current != null)
        {
            var prefix = level == 0 ? "" : new string(' ', (level - 1) * 2) + "-> Cause : ";
            sb.AppendLine($"{prefix}{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
            level++;
        }
        sb.AppendLine();
        sb.AppendLine("Stack trace :");
        sb.AppendLine(ex.StackTrace);
        return sb.ToString();
    }

    private void LogCrash(Exception? ex, string source)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalBayalaGestion");
            Directory.CreateDirectory(folder);
            var logPath = Path.Combine(folder, "crash.log");
            var text = ex != null ? BuildErrorText(ex) : "(exception nulle)";
            File.AppendAllText(logPath, $"[{DateTime.Now}] Source: {source}\n{text}\n\n");
        }
        catch { }
    }
}
