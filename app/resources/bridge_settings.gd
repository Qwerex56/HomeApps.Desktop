class_name BridgeSettings
extends Resource
## How the app starts the .NET bridge (HomeApps.Desktop docs/bridge.md). The bridge itself keeps the login.

## HomeApps' address, e.g. https://home-apps.dev; in dev https://app.localhost.
@export var server: String = "https://app.localhost"
## Accept the self-signed certificate of a local dev server (the bridge's --insecure).
@export var allows_dev_certificate: bool = true
## Speech on this computer (Speaches on localhost:8000) instead of on the server (the bridge's --speech local).
@export var has_local_speech: bool = false
## The built bridge (dotnet build); a relative path starts at this Godot project.
@export var bridge_path: String = "../src/Jarvis.VoiceBridge/bin/Debug/net10.0/jarvis-voice-bridge"
## Anything else for the bridge, e.g. ["--actions", "/Users/me/akcje.json"].
@export var extra_arguments: PackedStringArray = []


func resolved_bridge_path() -> String:
	if bridge_path.is_absolute_path():
		return bridge_path
	return ProjectSettings.globalize_path("res://").path_join(bridge_path).simplify_path()


func arguments() -> PackedStringArray:
	var args := PackedStringArray(["--ipc", "--server", server])
	if allows_dev_certificate:
		args.append("--insecure")
	if has_local_speech:
		args.append_array(["--speech", "local"])
	args.append_array(extra_arguments)
	return args
