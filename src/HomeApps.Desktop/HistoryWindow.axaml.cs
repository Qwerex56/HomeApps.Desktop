using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Requirement 2.5: the account's conversation with JARVIS from the tray menu. Closing it does not stop the listening.</summary>
public sealed partial class HistoryWindow : Window
{
    private readonly HistoryModel? _model;

    public HistoryWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    internal HistoryWindow(JarvisApi api) : this()
    {
        _model = new HistoryModel(api);
        Control<TextBox>("Search").KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                await RunAsync(() => _model.SearchAsync(Control<TextBox>("Search").Text, default));
            }
        };
        Control<CalendarDatePicker>("DayPicker").SelectedDateChanged += (_, _) => Refilter();
        Control<ComboBox>("ChannelBox").SelectionChanged += (_, _) => Refilter();
        Control<Button>("Clear").Click += (_, _) =>
        {
            Control<CalendarDatePicker>("DayPicker").SelectedDate = null;
            Control<ComboBox>("ChannelBox").SelectedIndex = 0;
        };
        Control<ListBox>("Conversations").SelectionChanged += async (_, _) =>
        {
            if (Control<ListBox>("Conversations").SelectedItem is ConversationView picked && picked.Id != _model.Selected?.Id)
            {
                await RunAsync(() => _model.SelectAsync(picked, default));
            }
        };
        Control<Button>("SendButton").Click += async (_, _) => await SendAsync();
        Control<TextBox>("Input").KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                await SendAsync();
            }
        };
        Opened += async (_, _) => await RunAsync(() => _model.SearchAsync(null, default));
    }

    private T Control<T>(string name) where T : Control => this.FindControl<T>(name)!;

    private async Task SendAsync()
    {
        var input = Control<TextBox>("Input");
        var text = input.Text;
        input.Text = string.Empty;
        await RunAsync(() => _model!.SendAsync(text ?? string.Empty, default));
    }

    private async Task RunAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            Title = $"JARVIS: historia rozmów ({exception.Message})";
        }

        Render();
    }

    private void Refilter()
    {
        _model!.Day = Control<CalendarDatePicker>("DayPicker").SelectedDate is { } d ? DateOnly.FromDateTime(d) : null;
        _model.Channel = (Control<ComboBox>("ChannelBox").SelectedItem as ComboBoxItem)?.Tag as string;
        Render();
    }

    private void Render()
    {
        var list = Control<ListBox>("Conversations");
        if (!ReferenceEquals(list.ItemsSource, _model!.Conversations))
        {
            list.ItemsSource = _model.Conversations;
            list.SelectedItem = _model.Selected;
        }

        var panel = Control<StackPanel>("Messages");
        panel.Children.Clear();
        foreach (var message in _model.Visible)
        {
            var who = message.Role == MessageRole.Person ? "Ty" : "JARVIS";
            panel.Children.Add(new TextBlock
            {
                Text = $"{message.SentAt.ToLocalTime():yyyy-MM-dd HH:mm} [{message.Channel}] {who}: {message.Text}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            });
            foreach (var action in message.Actions)
            {
                panel.Children.Add(ActionLine(action));
            }
        }
    }

    private StackPanel ActionLine(ActionView action)
    {
        var line = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(16, 0, 0, 0) };
        line.Children.Add(new TextBlock { Text = $"{action.Tool}: {action.Description} ({Result(action.Status)})" });
        if (action.Status == ToolCallStatus.NeedsConfirmation)
        {
            line.Children.Add(Decision("Tak", action.Id, true));
            line.Children.Add(Decision("Nie", action.Id, false));
        }

        return line;
    }

    private Button Decision(string label, Guid id, bool approve)
    {
        var button = new Button { Content = label };
        button.Click += async (_, _) => await RunAsync(() => _model!.DecideAsync(id, approve, default));
        return button;
    }

    private static string Result(ToolCallStatus status) => status switch
    {
        ToolCallStatus.Done => "wykonano",
        ToolCallStatus.Failed => "błąd",
        ToolCallStatus.NeedsConfirmation => "czeka na potwierdzenie",
        ToolCallStatus.Denied => "odrzucono",
        _ => "wygasło",
    };
}
