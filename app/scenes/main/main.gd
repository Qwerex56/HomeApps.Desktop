extends Node
## JARVIS on the computer (jarvis-satellites.md S-2c): starts the bridge, puts the window in the screen's
## corner (it never takes focus and lets clicks through, S5) and wires the bridge to the tray and the hologram.

const BridgeProcess := preload("res://common/components/bridge_process/bridge_process.gd")
const Tray := preload("res://scenes/tray/tray.gd")
const Hologram := preload("res://scenes/hologram/hologram.gd")
const Settings := preload("res://resources/bridge_settings.gd")

const SCREEN_MARGIN: int = 24

@onready var _bridge: BridgeProcess = $BridgeProcess
@onready var _tray: Tray = $Tray
@onready var _hologram: Hologram = $Hologram


func _ready() -> void:
	_place_window()
	_bridge.state_changed.connect(_on_state_changed)
	_bridge.level_changed.connect(_hologram.show_level)
	_bridge.heard.connect(_hologram.show_heard)
	_bridge.replied.connect(_hologram.show_reply)
	_bridge.error_reported.connect(_on_error_reported)
	_bridge.login_requested.connect(_on_login_requested)
	_tray.talk_requested.connect(_bridge.send.bind("talk"))
	_tray.mute_toggled.connect(_on_mute_toggled)
	_tray.settings_requested.connect(_on_settings_requested)
	_tray.restart_requested.connect(_bridge.restart)
	_tray.logout_requested.connect(_bridge.log_out)
	_tray.quit_requested.connect(_on_quit_requested)
	_bridge.start()


func _place_window() -> void:
	var window := get_window()
	window.mouse_passthrough = true
	var screen := DisplayServer.screen_get_usable_rect(window.current_screen)
	window.position = screen.end - window.size - Vector2i(SCREEN_MARGIN, SCREEN_MARGIN)


func _on_state_changed(state: String) -> void:
	_tray.show_state(state)
	_hologram.show_state(state)


func _on_error_reported(code: String, detail: String) -> void:
	push_warning("%s: %s" % [code, detail])
	if code.begins_with("app."):
		_tray.show_problem(detail)


func _on_login_requested(_url: String) -> void:
	_tray.show_problem("zaloguj się w przeglądarce, która się otworzyła")


func _on_mute_toggled(is_muted: bool) -> void:
	_bridge.send("mute" if is_muted else "unmute")


func _on_settings_requested() -> void:
	# Shown in Finder (or the file manager) rather than opened: a .cfg may have no app to open it.
	OS.shell_show_in_file_manager(Settings.user_file_path())


func _on_quit_requested() -> void:
	_bridge.quit()
	get_tree().quit()
