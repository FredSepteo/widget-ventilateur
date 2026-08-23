using System.Diagnostics;
using System.IO;

namespace FanWidget.Services;

public static class StartupService
{
    private const string TaskName = "FanWidget";

    public static bool IsEnabled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{TaskName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return false;

            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
            CreateTask();
        else
            DeleteTask();
    }

    public static string GetExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            return processPath;

        return Path.Combine(AppContext.BaseDirectory, "FanWidget.exe");
    }

    private static void CreateTask()
    {
        var exe = GetExecutablePath();
        var args = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\"\" /SC ONLOGON /RL HIGHEST /F";
        RunSchtasks(args);
    }

    private static void DeleteTask()
    {
        RunSchtasks($"/Delete /TN \"{TaskName}\" /F");
    }

    private static void RunSchtasks(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Non-critical
        }
    }
}
