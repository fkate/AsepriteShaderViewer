// Custom shader using 2x2 pixel blocks and rotating them by 45 degrees to get a triangle pattern

const mat2x2 DEG45 = mat2x2(0.70710678118, -0.70710678118, 0.70710678118, 0.70710678118);
const float DIAG = 1.41421356237;

void mainImage(out vec4 fragColor, in vec2 fragCoord) {
	// The shader works in two pixel blocks
	vec2 uv = fragCoord * 0.5;
	vec2 pos = floor(uv);
	
	// Transform block to centered coordinates and rotate it by 45 degrees
	vec2 rel = (fract(uv) - vec2(0.5)) * DIAG;
	rel = DEG45 * rel + vec2(1.0);

	// Bring transformed coordinates to 0-1 uv space
	uv = pos * vec2(2.0) + rel;
	uv *= vec2(1.0) / iResolution.xy;

	fragColor = texture(iChannel0, uv);
}