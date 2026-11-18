extends Label
## Subtitles under the hologram (jarvis-satellites.md S-2d): what JARVIS heard, then what he answered.
## Long answers are cut to a few lines; the full text is in the account's JARVIS conversation.

@export var heard_color: Color = Color(0.75, 0.9, 1.0, 0.7)
@export var reply_color: Color = Color(0.85, 0.97, 1.0, 1.0)


func show_heard(heard_text: String) -> void:
	_show(heard_text.strip_edges(), heard_color)


func show_reply(reply_text: String) -> void:
	_show(reply_text.strip_edges(), reply_color)


func clear() -> void:
	text = ""


func _show(shown_text: String, shown_color: Color) -> void:
	text = shown_text
	add_theme_color_override("font_color", shown_color)
