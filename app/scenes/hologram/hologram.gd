extends Control
## JARVIS's face (jarvis-satellites.md S-2d): the orb shader fades in while he listens, thinks or speaks, pulses
## with his voice and sends arcs round its rim while he thinks. Invisible otherwise; clicks pass through.

const SHOWN_STATES: PackedStringArray = ["listening", "thinking", "speaking"]

@export var fade_seconds: float = 0.3
@export var hide_delay_seconds: float = 1.2
@export var level_smoothing: float = 15.0
@export var thinking_blend_speed: float = 4.0

var _level: float = 0.0
var _shown_level: float = 0.0
var _thinking: float = 0.0
var _state: String = "idle"
var _fade: Tween

@onready var _orb_material: ShaderMaterial = $Orb.material


func _ready() -> void:
	modulate.a = 0.0


func _process(delta: float) -> void:
	_shown_level = lerpf(_shown_level, _level, minf(1.0, delta * level_smoothing))
	var thinking_target := 1.0 if _state == "thinking" else 0.0
	_thinking = move_toward(_thinking, thinking_target, delta * thinking_blend_speed)
	_orb_material.set_shader_parameter("level", _shown_level)
	_orb_material.set_shader_parameter("thinking", _thinking)


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
