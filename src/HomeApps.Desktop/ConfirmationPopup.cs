using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Tak / Nie on the screen for a waiting action: a small always-on-top window next to the tray, the same on every system.</summary>
internal sealed class ConfirmationPopup : INotifier
{
    public Task<bool?> AskAsync(ActionView action, CancellationToken cancellationToken)
    {
        var answer = new TaskCompletionSource<bool?>();
        Dispatcher.UIThread.Post(() =>
        {
            var window = new Window
            {
                Title = "JARVIS prosi o potwierdzenie",
                Width = 380,
                SizeToContent = SizeToContent.Height,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            var yes = new Button { Content = "Tak" };
            var no = new Button { Content = "Nie" };
            yes.Click += (_, _) => { answer.TrySetResult(true); window.Close(); };
            no.Click += (_, _) => { answer.TrySetResult(false); window.Close(); };
            window.Closed += (_, _) => answer.TrySetResult(null);
            window.Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = action.Description, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { yes, no } },
                },
            };
            window.Show();
        });
        return answer.Task.WaitAsync(cancellationToken);
    }
}
