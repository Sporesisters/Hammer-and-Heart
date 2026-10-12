// Draws a WorldBar on a single quad: an icon badge on the left and a rounded, segmented bar
// with a gradient fill, a trail behind recent changes and a glossy top. Everything is drawn from
// distance fields, so it stays crisp at any distance from the camera.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_test_disabled, blend_mix, shadows_disabled, fog_disabled;

// Size of the quad in metres.
uniform vec2 size = vec2(1.1, 0.26);
uniform float fill : hint_range(0.0, 1.0) = 1.0;
uniform float trail : hint_range(0.0, 1.0) = 1.0;
uniform vec4 fill_top : source_color = vec4(1.0, 0.55, 0.5, 1.0);
uniform vec4 fill_bottom : source_color = vec4(0.88, 0.2, 0.3, 1.0);
uniform vec4 trail_color : source_color = vec4(1.0, 0.92, 0.85, 0.85);
uniform vec4 icon_color : source_color = vec4(0.75, 0.82, 0.9, 1.0);
// 0 none, 1 gear, 2 heart, 3 hammer.
uniform int icon = 0;
uniform int segments = 5;
// 0..1: white flash right after a change.
uniform float flash = 0.0;
// 0..1: urgency beat, low health or a monster about to be calmed.
uniform float pulse = 0.0;
// Gear rotation in radians.
uniform float spin = 0.0;
uniform float alpha = 1.0;

// Constants are in linear space, like the converted source_color uniforms.
const vec3 INK = vec3(0.003, 0.002, 0.005);
const vec3 TRACK = vec3(0.022, 0.016, 0.03);

float dot2(vec2 v) { return dot(v, v); }

// Inigo Quilez's heart: tip at the origin, lobes up to y = 1.1.
float sd_heart(vec2 p) {
	p.x = abs(p.x);
	if (p.y + p.x > 1.0) return sqrt(dot2(p - vec2(0.25, 0.75))) - sqrt(2.0) / 4.0;
	return sqrt(min(dot2(p - vec2(0.0, 1.0)), dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
}

float sd_box(vec2 p, vec2 half_size) {
	vec2 d = abs(p) - half_size;
	return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// A pill around the horizontal segment of half length `half_length`, centred at the origin.
float sd_pill(vec2 p, float half_length, float radius) {
	return length(vec2(max(abs(p.x) - half_length, 0.0), p.y)) - radius;
}

vec2 rotate(vec2 p, float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// Coverage of a distance field, antialiased over one pixel.
float cover(float d, float aa) { return clamp(0.5 - d / aa, 0.0, 1.0); }

// Premultiplied "over".
void over(inout vec4 acc, vec3 color, float a) {
	acc = vec4(color * a, a) + acc * (1.0 - a);
}

void fragment() {
	vec2 p = (UV - 0.5) * size;
	p.y = -p.y;
	float aa = max(length(fwidth(p)), 1e-5);

	bool has_icon = icon != 0;
	float h = size.y;
	float radius = h * 0.5;
	vec2 icon_centre = vec2(-size.x * 0.5 + radius, 0.0);

	float border = has_icon ? h * 0.075 : h * 0.17;
	float bar_half = has_icon ? h * 0.25 : h * 0.5 - border;
	float bar_left = has_icon ? icon_centre.x + radius * 0.4 : -size.x * 0.5 + border;
	float bar_right = size.x * 0.5 - border;

	// The pill's straight part runs between its two round caps.
	float a = bar_left + bar_half;
	float b = bar_right - bar_half;
	vec2 q = p - vec2((a + b) * 0.5, 0.0);
	float half_length = max((b - a) * 0.5, 0.0);

	// Fill is measured from where the bar comes out from under the icon.
	float start = has_icon ? icon_centre.x + radius * 0.9 : bar_left;
	float span = bar_right - start;
	float t = (p.x - start) / span;

	vec4 acc = vec4(0.0);

	float outline = sd_pill(q, half_length, bar_half + border);
	over(acc, INK, cover(outline, aa) * 0.9);

	float inside = sd_pill(q, half_length, bar_half);
	float in_bar = cover(inside, aa);
	over(acc, TRACK, in_bar);

	float trail_edge = start + trail * span;
	over(acc, trail_color.rgb, in_bar * trail_color.a * clamp(0.5 - (p.x - trail_edge) / aa, 0.0, 1.0));

	float fill_edge = start + fill * span;
	float lit = clamp(p.y / bar_half * 0.5 + 0.5, 0.0, 1.0);
	vec3 fill_rgb = mix(fill_bottom.rgb, fill_top.rgb, lit);
	fill_rgb = mix(fill_rgb, vec3(1.0), flash * 0.8) * (1.0 + pulse * 0.4);
	over(acc, fill_rgb, in_bar * (fill > 0.0005 ? clamp(0.5 - (p.x - fill_edge) / aa, 0.0, 1.0) : 0.0));

	// Dark notches between segments.
	if (segments > 1 && t > 0.001 && t < 0.999) {
		float f = fract(t * float(segments));
		float gap = min(f, 1.0 - f) * span / float(segments);
		over(acc, INK, in_bar * (1.0 - smoothstep(0.0, aa * 1.5, gap - h * 0.012)) * 0.45);
	}

	// Gloss along the top half.
	over(acc, vec3(1.0), in_bar * smoothstep(bar_half * 0.05, bar_half * 0.6, p.y) * 0.13);

	if (has_icon) {
		vec2 c = p - icon_centre;
		float glow = pulse * 0.6;

		over(acc, INK, cover(length(c) - radius, aa) * 0.92);

		float disc = length(c) - (radius - border * 1.1);
		vec3 disc_rgb = mix(icon_color.rgb * 0.16, icon_color.rgb * 0.42, clamp(c.y / radius * 0.5 + 0.5, 0.0, 1.0));
		over(acc, disc_rgb, cover(disc, aa));

		vec3 bright = mix(icon_color.rgb, vec3(1.0), flash * 0.7 + glow * 0.3);

		if (icon == 1) {
			vec2 g = rotate(c, spin);
			float teeth = smoothstep(-0.3, 0.3, cos(8.0 * atan(g.y, g.x)));
			float gear = length(g) - (radius * 0.4 + radius * 0.13 * teeth);
			gear = max(gear, -(length(g) - radius * 0.17));
			over(acc, bright, cover(gear, aa));
		} else if (icon == 2) {
			float scale = radius * (0.92 + pulse * 0.12);
			vec2 hp = c / scale + vec2(0.0, 0.55);
			float heart = sd_heart(hp) * scale;
			float in_heart = cover(heart, aa);

			// The heart fills up from the bottom with the bar, with a little wave on top.
			float wave = fill > 0.001 && fill < 0.999 ? sin(c.x * 60.0 + TIME * 5.0) * radius * 0.04 : 0.0;
			float level = -0.55 * scale + fill * 1.12 * scale + wave;
			float filled = clamp(0.5 - (c.y - level) / aa, 0.0, 1.0);

			over(acc, icon_color.rgb * 0.12 + vec3(0.02), in_heart);
			over(acc, bright, in_heart * filled);
			over(acc, vec3(1.0), cover(length(c - vec2(-0.3, 0.22) * scale) - scale * 0.1, aa) * filled * 0.6);
		} else if (icon == 3) {
			vec2 m = rotate(c, -0.6) + vec2(0.0, radius * 0.08);
			float shaft = sd_box(m - vec2(0.0, -radius * 0.18), vec2(radius * 0.07, radius * 0.42));
			float head = sd_box(m - vec2(0.0, radius * 0.3), vec2(radius * 0.4, radius * 0.17));
			over(acc, bright * 0.55, cover(shaft, aa));
			over(acc, bright, cover(head, aa));
		}
	}

	ALBEDO = acc.rgb / max(acc.a, 1e-4);
	ALPHA = acc.a * alpha;
}
