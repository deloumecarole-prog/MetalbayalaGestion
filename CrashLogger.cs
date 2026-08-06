using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace MetalBayalaGestion;

internal static class CrashLogger
{
    [ModuleInitializer]
    public static void Init()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            WriteLog(args.ExceptionObject as Exception, "AppDomain (tres precoce)");
        };
    }

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

    public static void WriteLog(Exception? ex, string source)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalBayalaGestion");
            Directory.CreateDirectory(folder);
            var logPath = Path.Combine(folder, "crash.log");
            var text = ex != null ? BuildErrorText(ex) : "(exception nulle)";
            File.AppendAllText(logPath, $"[{DateTime.Now}] Source: {source}\n{text}\n\n");

            try
            {
                MessageBox.Show($"Erreur :\n\nSource: {source}\n\n{text}", "Erreur fatale", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }
        catch { }
    }
}
