extends Node
## Starts the exported app when the person logs in (jarvis-satellites.md S-2f): a LaunchAgent on macOS, an XDG
## autostart entry on Linux. Off by default; the menu-bar item turns it on. Not available from the editor.

const LAUNCH_AGENT: String = "Library/LaunchAgents/dev.home-apps.jarvis.plist"
const XDG_ENTRY: String = ".config/autostart/jarvis.desktop"


func is_supported() -> bool:
	return not OS.has_feature("editor") and OS.get_name() in ["macOS", "Linux"]


func is_enabled() -> bool:
	return is_supported() and FileAccess.file_exists(_entry_path())


func set_enabled(enabled: bool) -> void:
	if not is_supported():
		return
	var path := _entry_path()
	if not enabled:
		DirAccess.remove_absolute(path)
		return
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_warning("Nie mogę zapisać %s" % path)
		return
	file.store_string(_entry_text(OS.get_executable_path()))


func _entry_path() -> String:
	var relative := LAUNCH_AGENT if OS.get_name() == "macOS" else XDG_ENTRY
	return OS.get_environment("HOME").path_join(relative)


func _entry_text(executable: String) -> String:
	if OS.get_name() == "macOS":
		return """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>Label</key>
	<string>dev.home-apps.jarvis</string>
	<key>ProgramArguments</key>
	<array>
		<string>%s</string>
	</array>
	<key>RunAtLoad</key>
	<true/>
	<key>ProcessType</key>
	<string>Interactive</string>
</dict>
</plist>
""" % executable.xml_escape()
	return """[Desktop Entry]
Type=Application
Name=JARVIS
Comment=JARVIS na komputerze (HomeApps)
Exec="%s"
X-GNOME-Autostart-enabled=true
""" % executable
