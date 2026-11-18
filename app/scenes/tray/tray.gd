extends StatusIndicator
## JARVIS's icon in the menu bar (S5): a left click is "Mów"; the right-click menu also mutes, opens the settings,
## restarts the bridge, signs in again (S-2e) and quits. The tooltip says what JARVIS is doing.

signal talk_requested
signal mute_toggled(is_muted: bool)
signal settings_requested
signal restart_requested
signal logout_requested
signal quit_requested

enum Item { TALK = 0, MUTE = 1, SETTINGS = 3, RESTART = 4, LOGOUT = 5, QUIT = 7 }

const STATE_NAMES: Dictionary = {
	"connecting": "łączę z HomeApps…",
	"idle": "czeka na „Hey Jarvis”",
	"listening": "słucha",
	"thinking": "myśli",
	"speaking": "mówi",
	"muted": "mikrofon wyciszony",
}

@onready var _menu: PopupMenu = $Menu


func _ready() -> void:
	_menu.id_pressed.connect(_on_menu_id_pressed)
	pressed.connect(_on_pressed)
	show_state("connecting")


func show_state(state: String) -> void:
	tooltip = "JARVIS: %s" % STATE_NAMES.get(state, state)


func show_problem(detail: String) -> void:
	tooltip = "JARVIS: %s" % detail


func _on_pressed(mouse_button: int, _mouse_position: Vector2i) -> void:
	if mouse_button == MOUSE_BUTTON_LEFT:
		talk_requested.emit()


func _on_menu_id_pressed(id: int) -> void:
	match id:
		Item.TALK:
			talk_requested.emit()
		Item.MUTE:
			var index := _menu.get_item_index(Item.MUTE)
			var is_muted := not _menu.is_item_checked(index)
			_menu.set_item_checked(index, is_muted)
			mute_toggled.emit(is_muted)
		Item.SETTINGS:
			settings_requested.emit()
		Item.RESTART:
			restart_requested.emit()
		Item.LOGOUT:
			logout_requested.emit()
		Item.QUIT:
			quit_requested.emit()
