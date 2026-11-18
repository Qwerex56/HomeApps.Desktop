extends Control
## JARVIS's face until the shader (S-2d): a ring that fades in while he listens, thinks or speaks,
## pulses with his voice and slowly turns while he thinks. Invisible otherwise; clicks pass through.

const SHOWN_STATES: PackedStringArray = ["listening", "thinking", "speaking"]

@export var color: Color = Color(0.3, 0.85, 1.0)
@export var base_radius: float = 70.0
@export var pulse_radius: float = 40.0
@export var ring_width: float = 6.0
@export var fade_seconds: float = 0.3
@export var hide_delay_seconds: float = 1.2

var _level: float = 0.0
var _shown_level: float = 0.0
var _turn: float = 0.0
var _state: String = "idle"
var _fade: Tween


func _ready() -> void:
	modulate.a = 0.0


func _process(delta: float) -> void:
	_shown_level = lerpf(_shown_level, _level, minf(1.0, delta * 15.0))
	if _state == "thinking":
		_turn = wrapf(_turn + delta * 2.0, 0.0, TAU)
	queue_redraw()


func _draw() -> void:
	var center := size / 2.0
	var radius := base_radius + pulse_radius * _shown_level
	draw_circle(center, radius * 0.35, Color(color, 0.25 + 0.5 * _shown_level))
	if _state == "thinking":
		draw_arc(center, radius, _turn, _turn + TAU * 0.75, 48, color, ring_width, true)
	else:
		draw_arc(center, radius, 0.0, TAU, 64, color, ring_width, true)


func show_state(state: String) -> void:
	_state = state
	if state in SHOWN_STATES:
		_fade_to(1.0, 0.0)
	else:
		_level = 0.0
		_fade_to(0.0, hide_delay_seconds)


func show_level(level: float) -> void:
	_level = clampf(level, 0.0, 1.0)


func _fade_to(alpha: float, delay: float) -> void:
	if _fade != null:
		_fade.kill()
	_fade = create_tween()
	_fade.tween_property(self, "modulate:a", alpha, fade_seconds).set_delay(delay)
