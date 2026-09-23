shader_type canvas_item;

// Multiplies whatever is already on screen, so the brightness option affects the whole game
// without every level needing its own environment tweak.
uniform sampler2D screen_texture : hint_screen_texture, filter_linear;
uniform float brightness : hint_range(0.0, 2.0) = 1.0;

void fragment()
{
	vec3 screen_colour = texture(screen_texture, SCREEN_UV).rgb;
	COLOR = vec4(screen_colour * brightness, 1.0);
}
