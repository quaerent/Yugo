using System.Diagnostics;

namespace Yugo.Game;

public static class FileDialog
{
    public static string? OpenLevelFile()
    {
        return TryOpenFile("Yugo Level (*.xml)|*.xml|All Files (*.*)|*.*");
    }

    public static string? SaveLevelFile()
    {
        return TrySaveFile("Yugo Level (*.xml)|*.xml|All Files (*.*)|*.*");
    }

    private static string? TryOpenFile(string filter)
    {
        try
        {
            var dialogType = Type.GetType(
                "System.Windows.Forms.OpenFileDialog, System.Windows.Forms"
            );
            if (dialogType == null)
                return TryOpenFileMac();

            using var dialog = Activator.CreateInstance(dialogType) as IDisposable;
            if (dialog == null)
                return null;

            dialogType.GetProperty("Filter")?.SetValue(dialog, filter);
            dialogType.GetProperty("Multiselect")?.SetValue(dialog, false);

            var result = dialogType.GetMethod("ShowDialog", Type.EmptyTypes)?.Invoke(dialog, null);
            var ok = result?.ToString()?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true;
            if (!ok)
                return null;

            return dialogType.GetProperty("FileName")?.GetValue(dialog) as string;
        }
        catch
        {
            return TryOpenFileMac();
        }
    }

    private static string? TrySaveFile(string filter)
    {
        try
        {
            var dialogType = Type.GetType(
                "System.Windows.Forms.SaveFileDialog, System.Windows.Forms"
            );
            if (dialogType == null)
                return TrySaveFileMac();

            using var dialog = Activator.CreateInstance(dialogType) as IDisposable;
            if (dialog == null)
                return null;

            dialogType.GetProperty("Filter")?.SetValue(dialog, filter);
            dialogType.GetProperty("AddExtension")?.SetValue(dialog, true);
            dialogType.GetProperty("DefaultExt")?.SetValue(dialog, "xml");

            var result = dialogType.GetMethod("ShowDialog", Type.EmptyTypes)?.Invoke(dialog, null);
            var ok = result?.ToString()?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true;
            if (!ok)
                return null;

            return dialogType.GetProperty("FileName")?.GetValue(dialog) as string;
        }
        catch
        {
            return TrySaveFileMac();
        }
    }

    private static string? TryOpenFileMac()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        return RunOsascript(
            "POSIX path of (choose file of type {\"public.xml\"} with prompt \"Select Yugo level\")"
        );
    }

    private static string? TrySaveFileMac()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        return RunOsascript("POSIX path of (choose file name with prompt \"Save Yugo level\")");
    }

    private static string? RunOsascript(string script)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                ArgumentList = { "-e", script },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                return null;

            var path = output.Trim();
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }
}
