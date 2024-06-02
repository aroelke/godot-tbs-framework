shader_type canvas_item;

uniform vec4 overlay : source_color = vec4(1, 1, 1, 0);

varying vec4 v_vertex_color;

void vertex() {
	// Get the modulate color
	v_vertex_color = COLOR;
}

void fragment() {
	// Apply the color overlay
    vec4 texture_color = v_vertex_color*texture(TEXTURE, UV);
    COLOR = vec4(mix(texture_color.rgb, overlay.rgb, overlay.a), texture_color.a);
}