using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

public sealed class App : Application, IDisposable
{
    private TrayIconManager? _icons;
    private MuteSwitch? _mute;
    private GlobalHotkey? _hotkey;
    private VoiceBridgeHost? _host;
    private readonly CancellationTokenSource _stop = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public void Dispose()
    {
        _hotkey?.Dispose();
        _stop.Dispose();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            StartTray(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task RunBridgeAsync(string[] args)
    {
        try
        {
            await _host!.RunAsync(args, _stop.Token);
        }
        catch (OperationCanceledException)
        {
            // Quitting.
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Most głosowy się zatrzymał: {exception.Message}");
            Dispatcher.UIThread.Post(() => _icons?.SetState(TrayState.Connecting));
        }
    }

    private void StartTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var tray = new TrayIcon { ToolTipText = "JARVIS" };
        _icons = new TrayIconManager(ReducedMotion.IsOn(), key => tray.Icon = TrayIcons.For(key));
        var muteItem = new NativeMenuItem("Wycisz mikrofon");
        _mute = new MuteSwitch(muted =>
        {
            _ = _host?.SetMutedAsync(muted);
            muteItem.Header = muted ? "Włącz mikrofon" : "Wycisz mikrofon";
            if (muted)
            {
                _icons.SetState(TrayState.Muted);
            }

            // Unmuting: the bridge reports the state it is really in.
        });
        muteItem.Click += (_, _) => _mute.Toggle();
        var quit = new NativeMenuItem("Zakończ");
        quit.Click += (_, _) => desktop.Shutdown();
        desktop.ShutdownRequested += (_, _) =>
        {
            _stop.Cancel();
            _hotkey?.Dispose();
            _hotkey = null;
        };
        tray.Menu = [muteItem, new NativeMenuItemSeparator(), quit];

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) => _icons.Tick();
        timer.Start();

        _icons.SetState(TrayState.Connecting);
        tray.IsVisible = true;

        _hotkey = new GlobalHotkey(() => Dispatcher.UIThread.Post(_mute.Toggle));
        _hotkey.Start();

        var icons = _icons;
        _host = new VoiceBridgeHost(state => Dispatcher.UIThread.Post(() => icons.SetState(state)));
        _ = RunBridgeAsync(desktop.Args ?? []);
    }
}
