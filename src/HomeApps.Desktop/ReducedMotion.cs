using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HomeApps.Desktop;

/// <summary>Reads the system's "reduce motion" setting (the desktop's prefers-reduced-motion).</summary>
public static class ReducedMotion
{
    /// <summary>HOMEAPPS_REDUCE_MOTION=1 or =0 overrides the system value.</summary>
    public static bool IsOn()
    {
        var forced = Environment.GetEnvironmentVariable("HOMEAPPS_REDUCE_MOTION");
        if (forced is not null)
        {
            return forced == "1";
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Run("defaults", "read com.apple.universalaccess reduceMotion") == "1";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Run("gsettings", "get org.gnome.desktop.interface enable-animations") == "false";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("reg", "query \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate") is { } v && v.EndsWith('0');
            }
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // The setting cannot be read: keep the animation.
        }

        return false;
    }

    private static string? Run(string file, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        var output = process?.StandardOutput.ReadToEnd().Trim();
        process?.WaitForExit(2000);
        return output?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    }
}
