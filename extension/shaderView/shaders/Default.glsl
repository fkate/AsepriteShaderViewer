void mainImage(out vec4 fragColor, in vec2 fragCoord) {  
	// Transform fragCoord to uv space and output the texture
	fragColor = texture(iChannel0, fragCoord / iResolution.xy);
}