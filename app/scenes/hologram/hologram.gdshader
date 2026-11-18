shader_type canvas_item;
// JARVIS's hologram (jarvis-satellites.md S-2d): a glowing orb with a Fresnel-like rim, moving scan lines and a
// short glitch now and then. `level` makes it pulse with his voice, `thinking` sends arcs round the rim.

uniform vec4 tint : source_color = vec4(0.3, 0.85, 1.0, 1.0);
uniform float level : hint_range(0.0, 1.0) = 0.0;
uniform float thinking : hint_range(0.0, 1.0) = 0.0;
uniform float radius : hint_range(0.05, 0.5) = 0.3;
uniform float pulse : hint_range(0.0, 0.2) = 0.07;
uniform float rim_power : hint_range(1.0, 8.0) = 3.5;
uniform float scanline_count = 70.0;
uniform float scanline_speed = 1.2;
uniform float glitch_strength : hint_range(0.0, 1.0) = 0.4;
uniform float glitch_rate = 8.0;

float hash(float n) {
	return fract(sin(n) * 43758.5453);
}

void fragment() {
	vec2 uv = UV;

	// Glitch: in a few random frames a few horizontal bands slide sideways.
	float frame = floor(TIME * glitch_rate);
	float band = floor(uv.y * 28.0);
	float glitch = step(0.9, hash(frame * 13.1 + band)) * step(0.75, hash(frame));
	uv.x += (hash(band + frame * 7.3) - 0.5) * 0.08 * glitch * glitch_strength;

	vec2 p = uv - 0.5;
	float r = radius + pulse * level;
	float d = length(p) / r;
	float disc = 1.0 - smoothstep(0.96, 1.0, d);

	// Bright rim, faint core that lights up with his voice, soft halo outside.
	float rim = pow(clamp(d, 0.0, 1.0), rim_power);
	float core = (1.0 - d) * (0.15 + 0.6 * level);
	float halo = exp(-max(d - 1.0, 0.0) * 9.0) * (1.0 - disc) * (0.35 + 0.4 * level);

	// While thinking: three arcs run round the rim.
	float angle = atan(p.y, p.x);
	float arcs = thinking * smoothstep(0.55, 1.0, sin(angle * 3.0 - TIME * 4.0)) * smoothstep(0.8, 1.0, d) * disc;

	float scan = 0.7 + 0.3 * sin((uv.y * scanline_count - TIME * scanline_speed) * 6.2831853);
	float alpha = (disc * (rim * 0.9 + core + arcs) + halo) * scan;

	vec3 color = tint.rgb + glitch * vec3(0.4, -0.2, 0.2) * glitch_strength;
	color += vec3(rim * disc * 0.35);
	COLOR = vec4(color, clamp(alpha, 0.0, 1.0) * tint.a);
}
