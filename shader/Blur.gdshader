shader_type canvas_item;
uniform float lod: hint_range(0.0, 5) = 1.5;
uniform float darkness: hint_range(0.0, 5) = 2.5;
uniform sampler2D SCREEN_TEXTURE:hint_screen_texture, filter_linear_mipmap;

void fragment(){
	vec4 color = texture(SCREEN_TEXTURE, SCREEN_UV, lod);
	COLOR = vec4(color.rgb/darkness, color.a);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
