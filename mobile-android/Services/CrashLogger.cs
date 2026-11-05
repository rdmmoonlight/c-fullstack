using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace AumoFinance.Services;

public static class CrashLogger
{
    private static string LogFilePath => Path.Combine(FileSystem.CacheDirectory, "aumo_crash_log.txt");

    public static void Install()
    {
        // 1. Tangkap unhandled exception C# standar (.NET)
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            WriteLog("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.IsTerminating);
        };

        // 2. Tangkap unobserved task exception (Async/Task)
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            WriteLog("TaskScheduler.UnobservedTaskException", e.Exception, isTerminating: false);
            e.SetObserved();
        };

#if ANDROID
        // 3. Tangkap unhandled exception spesifik dari Android Interop / Java Native
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) =>
        {
            WriteLog("AndroidEnvironment.UnhandledException", e.Exception, isTerminating: true);
            e.Handled = true; // Coba cegah crash langsung jika memungkinkan
        };
#endif
    }

    private static void WriteLog(string source, Exception? ex, bool isTerminating)
    {
        try
        {
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Source: {source} | Terminating: {isTerminating}\n\n{ex}\n";
            File.AppendAllText(LogFilePath, text + "\n----------------------------------------\n\n");
        }
        catch
        {
            // Abaikan error di dalam logger
        }
    }

    public static string? ReadAndClearLastCrash()
    {
        try
        {
            if (!File.Exists(LogFilePath))
            {
                return null;
            }

            var content = File.ReadAllText(LogFilePath);
            File.Delete(LogFilePath);
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch
        {
            return null;
        }
    }
}
