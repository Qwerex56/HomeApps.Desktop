using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace HomeApps.Desktop;

public sealed class App : Application
{
    private TrayIconManager? _icons;
    private MuteSwitch? _mute;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            StartTray(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var tray = new TrayIcon { ToolTipText = "JARVIS" };
        _icons = new TrayIconManager(ReducedMotion.IsOn(), key => tray.Icon = TrayIcons.For(key));
        var muteItem = new NativeMenuItem("Wycisz mikrofon");
        _mute = new MuteSwitch(muted =>
        {
            muteItem.Header = muted ? "Włącz mikrofon" : "Wycisz mikrofon";
            _icons.SetState(muted ? TrayState.Muted : TrayState.Idle);
        });
        muteItem.Click += (_, _) => _mute.Toggle();
        var quit = new NativeMenuItem("Zakończ");
        quit.Click += (_, _) => desktop.Shutdown();
        tray.Menu = [muteItem, new NativeMenuItemSeparator(), quit];

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) => _icons.Tick();
        timer.Start();

        _icons.SetState(TrayState.Idle);
        tray.IsVisible = true;
    }
}

/// <summary>Draws the tray icons: a colored disc per state, its ring pulsing with the animation frame.</summary>
internal static class TrayIcons
{
    private static readonly Dictionary<string, WindowIcon> Cache = [];

    public static WindowIcon For(string key)
    {
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var state = key.Split('-')[0];
        var frame = key.Contains('-') ? int.Parse(key.Split('-')[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
        var color = state switch
        {
            "listening" => Colors.LimeGreen,
            "thinking" => Colors.Orange,
            "speaking" => Colors.DeepSkyBlue,
            "muted" => Colors.IndianRed,
            "connecting" => Colors.Gray,
            _ => Colors.SteelBlue,
        };
        const int size = 32;
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size));
        using (var context = bitmap.CreateDrawingContext())
        {
            var radius = 9 + frame;
            context.DrawEllipse(new SolidColorBrush(color), null, new Point(size / 2.0, size / 2.0), radius, radius);
        }

        var icon = new WindowIcon(bitmap);
        Cache[key] = icon;
        return icon;
    }
}
