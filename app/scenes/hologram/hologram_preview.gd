extends Node
## Plays the hologram through a whole turn without the bridge, to tune the shader: run this scene (F6).
## listening -> thinking -> speaking (a fake voice level) -> idle, then again.

const Hologram := preload("res://scenes/hologram/hologram.gd")

const TURN: Array[Array] = [["listening", 2.0], ["thinking", 2.5], ["speaking", 4.0], ["idle", 2.5]]

var _step: int = -1
var _left: float = 0.0
var _speaking_time: float = 0.0

@onready var _hologram: Hologram = $Hologram


func _process(delta: float) -> void:
	_left -= delta
	if _left <= 0.0:
		_step = (_step + 1) % TURN.size()
		_left = TURN[_step][1]
		_hologram.show_state(TURN[_step][0])
		if TURN[_step][0] == "thinking":
			_hologram.show_heard("Jarvis, jaka jest pogoda na jutro?")
		elif TURN[_step][0] == "speaking":
			_hologram.show_reply("Jutro w Warszawie pochmurno, około pięciu stopni, po południu może popadać.")
	if TURN[_step][0] == "speaking":
		_speaking_time += delta
		_hologram.show_level(absf(sin(_speaking_time * 7.0)) * (0.5 + 0.5 * sin(_speaking_time * 1.3)))
