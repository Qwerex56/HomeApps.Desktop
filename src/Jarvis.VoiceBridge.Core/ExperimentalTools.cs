namespace Jarvis.VoiceBridge;

/// <summary>
/// The experimental computer tools of the tray app (requirement 2.10): six, off by default, each a toggle in the settings. A tool
/// that is off is not announced to the server, so JARVIS never sees it. macOS and Windows only; no clipboard, screenshots or
/// system shortcuts. Input reaches the commands as <c>JARVIS_&lt;FIELD&gt;</c> and is checked there, never put in the command text.
/// </summary>
internal static class ExperimentalTools
{
    public static readonly IReadOnlyList<string> Ids = ["open_app", "open_url", "media_control", "now_playing", "volume", "timer"];

    private const string WindowsKeys =
        "Add-Type -Namespace J -Name K -MemberDefinition '[System.Runtime.InteropServices.DllImport(\"user32.dll\")] public static extern void keybd_event(byte v, byte s, int f, int e);'; " +
        "function Press($k) { [J.K]::keybd_event($k, 0, 0, 0); [J.K]::keybd_event($k, 0, 2, 0) }; ";

    private const string MacPlayer =
        "app=''; for p in Spotify Music; do if [ \"$(osascript -e \"application \\\"$p\\\" is running\")\" = true ]; then app=$p; break; fi; done; " +
        "[ -n \"$app\" ] || { echo 'Nic nie gra: brak Spotify i Muzyki.' >&2; exit 1; }; ";

    public static string Label(string id) => id switch
    {
        "open_app" => "Otwieranie aplikacji",
        "open_url" => "Otwieranie stron (https)",
        "media_control" => "Sterowanie odtwarzaniem",
        "now_playing" => "Odczyt: co gra",
        "volume" => "Głośność",
        "timer" => "Minutnik i powiadomienia",
        _ => id,
    };

    public static IReadOnlyList<ComputerAction> Actions(IEnumerable<string> enabled) =>
        Actions(enabled, OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "");

    /// <summary>The announced actions of the enabled tools on <paramref name="system"/> ("macos" or "windows"; anything else has none).</summary>
    public static IReadOnlyList<ComputerAction> Actions(IEnumerable<string> enabled, string system) =>
        system is "macos" or "windows"
            ? Ids.Where(enabled.Contains).Select(id => Build(id, system == "windows")).ToArray()
            : [];

    private static ComputerAction Build(string id, bool windows) => id switch
    {
        "open_app" => new(id, "Otwiera aplikację o podanej nazwie na tym komputerze.", windows
            ? "if ([string]::IsNullOrWhiteSpace($env:JARVIS_APP)) { Write-Error 'Podaj nazwę aplikacji.'; exit 1 }; Start-Process -FilePath $env:JARVIS_APP"
            : "[ -n \"$JARVIS_APP\" ] || { echo 'Podaj nazwę aplikacji.' >&2; exit 1; }; open -a \"$JARVIS_APP\"",
            Schema("app", "string"), PowerShell: windows),
        "open_url" => new(id, "Otwiera adres https:// w przeglądarce na tym komputerze.", windows
            ? "if ($env:JARVIS_URL -like 'https://*') { Start-Process $env:JARVIS_URL } else { Write-Error 'Tylko adresy https://'; exit 1 }"
            : "case \"$JARVIS_URL\" in https://*) open \"$JARVIS_URL\";; *) echo 'Tylko adresy https://' >&2; exit 1;; esac",
            Schema("url", "string"), PowerShell: windows),
        "media_control" => new(id, "Steruje odtwarzaniem na tym komputerze: command to play_pause, next albo previous.", windows
            ? WindowsKeys + "switch ($env:JARVIS_COMMAND) { 'play_pause' { Press 0xB3 } 'next' { Press 0xB0 } 'previous' { Press 0xB1 } default { Write-Error 'command: play_pause, next lub previous'; exit 1 } }"
            : MacPlayer + "case \"$JARVIS_COMMAND\" in play_pause) c=playpause;; next) c='next track';; previous) c='previous track';; *) echo 'command: play_pause, next lub previous' >&2; exit 1;; esac; osascript -e \"tell application \\\"$app\\\" to $c\"",
            Schema("command", "string"), PowerShell: windows),
        "now_playing" => new(id, "Podaje, co teraz gra na tym komputerze (tytuł i wykonawca); tylko odczyt.", windows
            ? "$t = Get-Process Spotify -ErrorAction SilentlyContinue | Where-Object MainWindowTitle | Select-Object -First 1 -ExpandProperty MainWindowTitle; if ($t -and $t -ne 'Spotify' -and $t -ne 'Spotify Free') { $t } else { 'Nic nie gra.' }"
            : MacPlayer + "[ \"$(osascript -e \"tell application \\\"$app\\\" to player state as string\")\" = playing ] || { echo 'Nic nie gra.'; exit 0; }; osascript -e \"tell application \\\"$app\\\" to (name of current track) & \\\" - \\\" & (artist of current track)\"",
            null, ReadOnly: true, Confirm: false, PowerShell: windows),
        "volume" => new(id, "Ustawia głośność tego komputera na level od 0 do 100.", windows
            ? WindowsKeys + "$l = 0; if (-not [int]::TryParse($env:JARVIS_LEVEL, [ref]$l) -or $l -lt 0 -or $l -gt 100) { Write-Error 'level: 0-100'; exit 1 }; 1..50 | ForEach-Object { Press 0xAE }; 1..[int]($l / 2) | ForEach-Object { Press 0xAF }"
            : "case \"$JARVIS_LEVEL\" in ''|*[!0-9]*) echo 'level: 0-100' >&2; exit 1;; esac; [ \"$JARVIS_LEVEL\" -le 100 ] || { echo 'level: 0-100' >&2; exit 1; }; osascript -e \"set volume output volume $JARVIS_LEVEL\"",
            Schema("level", "integer"), PowerShell: windows),
        "timer" => new(id, "Ustawia minutnik: po minutes minutach pokazuje powiadomienie z tekstem text.", windows
            ? "$m = 0; if (-not [int]::TryParse($env:JARVIS_MINUTES, [ref]$m) -or $m -lt 1 -or $m -gt 1440) { Write-Error 'minutes: 1-1440'; exit 1 }; " +
              "Start-Process powershell -WindowStyle Hidden -ArgumentList '-NoProfile','-Command','Start-Sleep -Seconds ([int]$env:JARVIS_MINUTES * 60); Add-Type -AssemblyName System.Windows.Forms; $n = New-Object System.Windows.Forms.NotifyIcon; $n.Icon = [System.Drawing.SystemIcons]::Information; $n.Visible = $true; $n.ShowBalloonTip(10000, \"JARVIS\", $(if ($env:JARVIS_TEXT) { $env:JARVIS_TEXT } else { \"Minutnik\" }), \"Info\"); Start-Sleep -Seconds 12'"
            : "case \"$JARVIS_MINUTES\" in ''|*[!0-9]*|0*) echo 'minutes: 1-1440' >&2; exit 1;; esac; [ \"$JARVIS_MINUTES\" -le 1440 ] || { echo 'minutes: 1-1440' >&2; exit 1; }; " +
              "nohup sh -c 'sleep $((JARVIS_MINUTES * 60)); osascript -e \"on run argv\" -e \"display notification (item 1 of argv) with title \\\"JARVIS\\\"\" -e \"end run\" \"${JARVIS_TEXT:-Minutnik}\"' >/dev/null 2>&1 &",
            Schema("minutes", "integer", ("text", "string")), PowerShell: windows),
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    private static System.Text.Json.JsonElement Schema(string required, string type, params (string Name, string Type)[] optional)
    {
        var properties = new Dictionary<string, object> { [required] = new { type } };
        foreach (var (name, kind) in optional)
        {
            properties[name] = new { type = kind };
        }

        return System.Text.Json.JsonSerializer.SerializeToElement(new { type = "object", properties, required = new[] { required } });
    }
}
