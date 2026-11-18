class_name BridgeSettings
extends Resource
## How the app starts the .NET bridge (HomeApps.Desktop docs/bridge.md). The bridge itself keeps the login.
## The values here are defaults; each person's own live in ~/.homeapps/hologram/settings.cfg (S-2e), written with
## these defaults on the first run and read again every time the bridge starts.

## Where the exported app keeps its own bridge (S-2f): next to the executable on Linux, in Resources on macOS.
const BUNDLED_BRIDGE_MACOS: String = "../Resources/bridge/jarvis-voice-bridge"
const BUNDLED_BRIDGE_LINUX: String = "bridge/jarvis-voice-bridge"
## The bridge built by dotnet build, used when the app runs from the editor.
const DEV_BRIDGE: String = "../src/Jarvis.VoiceBridge/bin/Debug/net10.0/jarvis-voice-bridge"
## What an exported app starts with on its first run (the .tres defaults are for dev).
const PRODUCTION_SERVER: String = "https://home-apps.dev"
const USER_FOLDER: String = ".homeapps/hologram"
const USER_FILE: String = "settings.cfg"
const SECTION: String = "bridge"
const KEYS: PackedStringArray = [
	"server", "allows_dev_certificate", "has_local_speech", "speech_engine", "device_name", "bridge_path",
	"extra_arguments",
]

## HomeApps' address, e.g. https://home-apps.dev; in dev https://app.localhost.
@export var server: String = "https://app.localhost"
## Accept the self-signed certificate of a local dev server (the bridge's --insecure).
@export var allows_dev_certificate: bool = true
## Speech on this computer instead of on the server (the bridge's --speech local, S1: the default for this app).
@export var has_local_speech: bool = true
## The speech engine for local speech (OpenAI audio API, e.g. Speaches).
@export var speech_engine: String = "http://localhost:8000"
## How this computer is called in Konto → Komputery; empty: the computer's own name.
@export var device_name: String = ""
## Another bridge than the usual one; empty: the bridge inside the exported app, or the dev build from the editor.
## A relative path starts at this Godot project and counts only in the editor.
@export var bridge_path: String = ""
## Anything else for the bridge, e.g. ["--actions", "/Users/me/akcje.json"].
@export var extra_arguments: PackedStringArray = []


static func user_file_path() -> String:
	var home := OS.get_environment("HOME")
	if home.is_empty():
		home = OS.get_environment("USERPROFILE")
	return home.path_join(USER_FOLDER).path_join(USER_FILE)


## Reads the person's settings over the defaults; on the first run writes the defaults there to edit.
func load_user_file() -> void:
	var path := user_file_path()
	var file := ConfigFile.new()
	if file.load(path) != OK:
		if not OS.has_feature("editor"):
			server = PRODUCTION_SERVER
			allows_dev_certificate = false
		_save_user_file(path)
		return
	for key in KEYS:
		if file.has_section_key(SECTION, key):
			set(key, file.get_value(SECTION, key))


func resolved_bridge_path() -> String:
	if bridge_path.is_absolute_path():
		return bridge_path
	if OS.has_feature("editor"):
		var dev_path := DEV_BRIDGE if bridge_path.is_empty() else bridge_path
		return ProjectSettings.globalize_path("res://").path_join(dev_path).simplify_path()
	var bundled := BUNDLED_BRIDGE_MACOS if OS.get_name() == "macOS" else BUNDLED_BRIDGE_LINUX
	return OS.get_executable_path().get_base_dir().path_join(bundled).simplify_path()


func arguments() -> PackedStringArray:
	var args := PackedStringArray(["--ipc", "--link", "--server", server])
	if allows_dev_certificate:
		args.append("--insecure")
	if has_local_speech:
		args.append_array(["--speech", "local", "--engine", speech_engine])
	if not device_name.is_empty():
		args.append_array(["--device", device_name])
	args.append_array(extra_arguments)
	return args


## The bridge's --logout for the same server: it forgets the login and the pairing on this computer.
func logout_arguments() -> PackedStringArray:
	return PackedStringArray(["--logout", "--server", server])


func _save_user_file(path: String) -> void:
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var file := ConfigFile.new()
	for key in KEYS:
		file.set_value(SECTION, key, get(key))
	if file.save(path) != OK:
		push_warning("Nie mogę zapisać ustawień w %s" % path)
