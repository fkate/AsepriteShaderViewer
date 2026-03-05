// Normal map from alpha shader with simple light testing

void mainImage(out vec4 fragColor, in vec2 fragCoord) {  
	ivec2 center = ivec2(fragCoord);

	// For this normal map just use the alpha channel to generate borders
	float l = texelFetch(iChannel0, center + ivec2(-1, 0), 0).a;
	float r = texelFetch(iChannel0, center + ivec2(1, 0), 0).a;
	float t = texelFetch(iChannel0, center + ivec2(0, -1), 0).a;
	float b = texelFetch(iChannel0, center + ivec2(0, 1), 0).a;

	// Look up differences between sampled values on both sides
	vec3 norm = vec3((l - r), (t - b), 1.0);
	norm = normalize(norm);
	
	// Test lighting on mouse click
	if(iMouse.z > 0.0 || iMouse.w > 0.0) {
		vec3 dir = iMouse.z > 0.0 ? vec3(iMouse.xy - fragCoord, 25.0) : vec3(normalize(iMouse.xy - iResolution.xy / 2.0), 1.0);
		dir = normalize(dir);
		
		vec4 col = texelFetch(iChannel0, center, 0);
		fragColor = vec4(col.rgb * (dot(dir, norm) + 0.25), col.a);
		
	} else {
		// Bring normalmap from [-1 ~ 1] to [0 ~ 1] range
		fragColor = vec4(norm.xy * 0.5 + 0.5, norm.z, 1.0);
	}
}