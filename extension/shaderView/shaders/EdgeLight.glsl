// Simple shader using offseted sampling to draw lights and shadows via the alpha channel

const vec2 OFFSET = vec2(1.0, -1.0);

void mainImage(out vec4 fragColor, in vec2 fragCoord) {  
	// Fetch main image in pixel space
	vec4 color = texelFetch(iChannel0, ivec2(fragCoord), 0);
	
	// Fetch mask by offsetting the image alpha channel and inverting it
	float mask = 1.0 - ceil(texelFetch(iChannel0, ivec2(fragCoord.x + OFFSET.x, fragCoord.y + OFFSET.y), 0).a);
	
	// Overlaps are treated as lit while differences are treated as shadow
	color.rgb += vec3(mask * 0.25);
	color.a = max(1.0 - mask, color.a);

	fragColor = color;
}