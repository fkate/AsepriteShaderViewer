// Basic feature CRT shader sample

vec4 SampleBilinear(vec2 pos) {
	// Manual bilinear sampling
	vec4 c0 = texelFetch(iChannel0, ivec2(pos.x, pos.y), 0);
	vec4 c1 = texelFetch(iChannel0, ivec2(pos.x + 1.0, pos.y), 0);
	vec4 c2 = texelFetch(iChannel0, ivec2(pos.x, pos.y + 1.0), 0);
	vec4 c3 = texelFetch(iChannel0, ivec2(pos.x + 1.0, pos.y + 1.0), 0);
	
	// Change the weight distribution to achieve other sampling modes
	vec2 weight = fract(pos);
	
	vec4 res = mix(mix(c0, c1, weight.x), mix(c2, c3, weight.x), weight.y);	
	return res;
}

void mainImage(out vec4 fragColor, in vec2 fragCoord) {  
	// Pixel to normalized coordinates
	vec2 uv = fragCoord / iResolution.xy;

    // warp the fragment coordinates at the borders
    vec2 bd = abs(vec2(0.5) - uv);
	vec2 warp = vec2(0.15);
    bd *= bd;    
	uv = (uv - vec2(0.5)) * (vec2(1.0) + bd.yx * warp) + 0.5;

	// Normalized to pixel coordinates
	uv *= iResolution.xy;

	// Manual bilinear filtering due to iChannel0 always working on point filtering
	vec3 color = SampleBilinear(uv - 0.5).rgb;

	// Create the light grid
	vec2 gridCoord = uv;
	gridCoord.x *= 2;
	vec2 reflected = 1.0 - abs(fract(gridCoord) * 2.0 - 1.0);

	// Create a smooth falloff from the grid coordinate centers
	float box = reflected.x * reflected.y + 0.5; 
	color *= vec3(box);
	
	// Add scanlines
	color += color * sin(gridCoord.y * 16.0) * 0.25;

	fragColor = vec4( color, 1.0 );
}