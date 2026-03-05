#define USE3D
// 3D shader using additional inputs (iPosition, iUV, iMesh, iNormal, uView, uProjection, uInverseView, uInverseProjection)
// First line always has to be the above define or it won't use the 3D renderer
// Also shows some basic cursor rendering when hovering the mesh

float pow5(float r) {
	float r2 = r * r;
	return r2 * r2 * r;
}

vec3 createDirection(float yaw, float pitch) {
	float yawSin = sin(yaw);
	float yawCos = cos(yaw);
	float pitchSin = sin(pitch);
	float pitchCos = cos(pitch);
	
	return normalize(vec3(yawSin * pitchCos, pitchSin, yawCos * pitchCos));
}

void mainImage(out vec4 fragColor, in vec2 fragCoord) {  
	// Sample the texture
	fragColor = texture(iChannel0, fragCoord / iResolution.xy);

	// Get light parameters from iChannel1 (premultiply the color and encode direction)
	vec4 lightColor = texelFetch(iChannel1, ivec2(0.0, 0.0), 0); //RGBA
	vec4 lightAngle = texelFetch(iChannel1, ivec2(1.0, 0.0), 0); // RG encoded direction B view fixing

	lightColor.rgb *= lightColor.a;
	lightAngle.rg = (lightAngle.rg - 0.5)* 6.28318530718;

	// Prepare for the light calculation (fix light to view if required)
	vec3 normal = normalize(iNormal);
	vec3 lightDir = createDirection(lightAngle.x, lightAngle.y);
	if(lightAngle.b > 0) lightDir = (uInverseView * vec4(lightDir, 0.0)).xyz;

	// Calculate light values
	float light = max(dot(normal, lightDir), 0.0);
	
	// Outline mesh has a flat light influence
	if(iMeshID == 1) light = 0.75;
		
	// Draw cursor from mouse input
	float isHovered = floor(fragCoord) == floor(iMouse.xy) ? 1.0 : 0.0;
	vec2 reflected = abs(fract(fragCoord) * 2.0 - 1.0);
	isHovered *= floor(max(reflected.x, reflected.y) + fwidth(fragCoord.x) * 4.0f) * ceil(min(reflected.x, reflected.y) - 0.5);

	// Light + Ambient + Fresnel + Backfaces
	fragColor = vec4(fragColor.rgb * vec3(light + 0.25) * lightColor.rgb, fragColor.a);
	fragColor = mix(fragColor, vec4(1.0 - fragColor.rgb, 1.0), isHovered);

	// Discard fully cut pixels and backfaces to reduce artifacts created by self transparency
	if(fragColor.a <= 0.5 || !gl_FrontFacing) discard;
}