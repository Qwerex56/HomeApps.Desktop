class_name BridgeProcess
extends Node
## Runs the .NET bridge with --ipc and turns its JSON lines into signals (HomeApps.Desktop docs/bridge.md).
## stdout carries the events, stderr the bridge's own log (printed here with a [most] prefix). A bridge that
## dies on its own is started again a few times; quitting the app closes it.

signal state_changed(state: String)
signal level_changed(level: float)
signal heard(text: String)
signal replied(text: String)
signal confirm_requested
signal error_reported(code: String, detail: String)
signal login_requested(url: String)

const RESTART_DELAY_SECONDS: float = 3.0
const MAX_RESTARTS: int = 5
const QUIT_WAIT_MSEC: int = 1500

@export var settings: BridgeSettings

var _pid: int = -1
var _stdio: FileAccess
var _stderr: FileAccess
var _events_thread: Thread
var _log_thread: Thread
var _is_quitting: bool = false
var _restarts: int = 0


func _ready() -> void:
	start()


func _exit_tree() -> void:
	quit()


func start() -> void:
	var path := settings.resolved_bridge_path()
	if not FileAccess.file_exists(path):
		error_reported.emit("app.bridge_missing", "Nie ma mostu w %s; zbuduj go: dotnet build" % path)
		return
	var pipe := OS.execute_with_pipe(path, settings.arguments())
	if pipe.is_empty():
		error_reported.emit("app.bridge_failed", "Nie mogę uruchomić %s" % path)
		return
	_pid = pipe["pid"]
	_stdio = pipe["stdio"]
	_stderr = pipe["stderr"]
	_is_quitting = false
	_events_thread = Thread.new()
	_events_thread.start(_read_events)
	_log_thread = Thread.new()
	_log_thread.start(_read_log)


## Sends one command: talk, stop, mute, unmute or quit.
func send(command: String) -> void:
	if _stdio == null or not OS.is_process_running(_pid):
		return
	_stdio.store_line(JSON.stringify({"type": command}))
	_stdio.flush()


func quit() -> void:
	if _pid < 0:
		return
	_is_quitting = true
	send("quit")
	var deadline := Time.get_ticks_msec() + QUIT_WAIT_MSEC
	while OS.is_process_running(_pid) and Time.get_ticks_msec() < deadline:
		OS.delay_msec(50)
	if OS.is_process_running(_pid):
		OS.kill(_pid)
	_join_threads()
	_pid = -1


func _read_events() -> void:
	while true:
		var line := _stdio.get_line()
		if line.is_empty():
			if _stdio.get_error() != OK or not OS.is_process_running(_pid):
				break
			continue
		_handle_event.call_deferred(line)
	_on_bridge_exited.call_deferred()


func _read_log() -> void:
	while true:
		var line := _stderr.get_line()
		if line.is_empty():
			if _stderr.get_error() != OK or not OS.is_process_running(_pid):
				break
			continue
		print("[most] ", line)


func _handle_event(line: String) -> void:
	var event: Variant = JSON.parse_string(line)
	if typeof(event) != TYPE_DICTIONARY:
		push_warning("Most wypisał coś, co nie jest zdarzeniem: %s" % line)
		return
	match String(event.get("type", "")):
		"state":
			state_changed.emit(String(event.get("state", "")))
		"level":
			level_changed.emit(float(event.get("level", 0.0)))
		"heard":
			heard.emit(String(event.get("text", "")))
		"reply":
			replied.emit(String(event.get("text", "")))
		"confirm":
			confirm_requested.emit()
		"error":
			error_reported.emit(str(event.get("code", "")), str(event.get("detail", "")))
		"login":
			login_requested.emit(String(event.get("url", "")))


func _on_bridge_exited() -> void:
	_join_threads()
	_pid = -1
	if _is_quitting:
		return
	_restarts += 1
	if _restarts > MAX_RESTARTS:
		error_reported.emit("app.bridge_dead", "Most padł %d razy; sprawdź log [most]." % MAX_RESTARTS)
		return
	state_changed.emit("connecting")
	await get_tree().create_timer(RESTART_DELAY_SECONDS).timeout
	if not _is_quitting:
		start()


func _join_threads() -> void:
	for thread: Thread in [_events_thread, _log_thread]:
		if thread != null and thread.is_started():
			thread.wait_to_finish()
	_events_thread = null
	_log_thread = null
