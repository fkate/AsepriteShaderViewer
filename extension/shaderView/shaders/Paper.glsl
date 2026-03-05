// Shader that uses a noise texture to deform coordinates and colors to give a paper like effect

void mainImage(out vec4 fragColor, in vec2 fragCoord) {
	// Get pixel uv and offset it depending on the frame for a secondary animation
    vec2 uv = fragCoord / iChannelResolution[1].xy;
	uv += iFrame * 0.3;

	// Sample noise from channel 1 to create a bidirectional offset
    vec2 offset = vec2(texture(iChannel1, uv * 0.37).r, texture(iChannel1, (uv + 0.5) * 0.37).r) * 0.4 - 0.2;

	// Sample second time as color noise
    float pattern = texture(iChannel1, uv * 11.3).b;	

	// Fetch pixel and change alpha to white
    vec4 color = texture(iChannel0, (fragCoord + offset) / iResolution.xy);
	color.rgb = mix(vec3(1.0), color.rgb, color.a);
	
	// Add pattern noise as white spots
    color.rgb = mix(color.rgb, vec3(0.9), pattern * 0.25);
    
	// Get rough grid from repeating coordinates
    float grid = max(fract(fragCoord.x), fract(fragCoord.y));
	
	// Mix the slightly blue tinted lines with the main color
    color.rgb = mix(color.rgb, vec3(0.0, 0.0, 0.2), floor(grid + 0.1) * 0.1);

    fragColor = vec4(color.rgb, 1.0);
}