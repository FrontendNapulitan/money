#define halfPI 1.5707963267948966
#define doublePI 6.283185307179586476925286766559

uniform vec3 ambientColor;
uniform vec3 directionalLightColor;
uniform vec3 directionalLightDirection;
uniform sampler2D specularMap;
uniform sampler2D metalMap;
uniform vec3 colorMod;

varying vec3 n;
varying vec3 v;
varying vec2 uv;
varying vec2 texCoord;
varying vec2 vUv;

uniform float uRot;
uniform float uTilt;
uniform float uEmerald;
uniform float uData[ 6 ];

uniform sampler2D uTex;
uniform sampler2D uTexBottomLines;
uniform sampler2D uTex_Emerald_Foil;
uniform sampler2D uTex_Image_Window;

uniform mat4 uMod;


/*
* 	Apply a band pass to an input color channel
*/

float band(float color, float pos, float size) {
    float rot = color - pos;
    return clamp (1.0 - rot * rot * size, 0.0, 0.6);
}

float square(float x) {
	return x * x;
}

// 	Standard gaussian function
float gaussian(float x, float mean, float amplitude, float std) {		
	return amplitude / (std * 2.50662827463) * exp(-0.5 * square((x-mean)/std));
}

// 	Standard sigmoid function
float sigmoid(float x, float mean, float std, float zeroPoint, float amplitude) {
	return amplitude / (1.0 + exp(-(x - mean) / std)) + (amplitude * -0.5 + zeroPoint);
}

float changeCoordinates(float z, float centre, float diameter){
	float radius = diameter / 2.0;
	return ( z - centre + radius ) / diameter;
}

// 	Adapted from https://github.com/mrdoob/three.js/issues/10766 
float atan2(float y, float x) {
    float sign = 1.0;
    if(y < 0.0) {
        sign = -1.0;
    }
    float absYandR = abs(y);
    float partSignX = 0.0;
    if(x < 0.0){
        partSignX = 2.0;
    }           
    float signX = 1.0 - partSignX;
    absYandR = (x - signX * absYandR) / (signX * x + absYandR);
    return ((partSignX + 1.0) * 0.7853981634 + (0.1821 * absYandR * absYandR - 0.9675) * absYandR) * sign;
}


/*
* 	Add a shiny light reflection
*/

vec4 reflect(vec4 color, float pos) {
    const float threshold = 10.0;
    float c = band(color.g, pos, threshold);
    return vec4(color.rgb * (1.0 + c * 0.2), color.a);
}

void main() {	
// 	vec2 pixelCoord = (uv * viewportSize) + viewOffset;
// 	texCoord = pixelCoord * inverseTileTextureSize * inverseTileSize;
	float safeUTilt = uTilt;
	float safeURot = mod(abs(uRot), halfPI*2.0)-halfPI;

// 	Lighting
	vec3 surfaceToLight = normalize(directionalLightDirection - v);
    vec3 surfaceToCamera = normalize(cameraPosition - v);

// 	Sample the input pixels
    vec4 color = texture2D(uTex, texCoord) * uMod;
    vec4 maskBottomLines = texture2D(uTexBottomLines, texCoord);
	vec4 mask_Emerald_Foil = texture2D(uTex_Emerald_Foil, texCoord);
	vec4 mask_Image_Window = texture2D(uTex_Image_Window, texCoord);

	vec3 l = directionalLightDirection;
	vec3 r = reflect(l, n);
	
	float diffuse = max(0.0, dot(n, l));
	vec4 diffuseTex = texture2D(uTex, texCoord) * uMod;
// 	float offset = (diffuseTex.r * 0.5) - 1.0;
// 	float theta = acos(max(0.0, dot(r + 0.2 * offset, v)));  // 0 < theta < doublePI / 2


/*
* 	Depending on the mask colour apply the effect
*/

// Silvery stripe or foil
	if (mask_Emerald_Foil.b > 0.4 && mask_Image_Window.g < 0.25 ) {
		vec3 specular = texture2D(metalMap, vec2(safeUTilt * -1.1 / halfPI + safeURot * 0.3 / halfPI, 0.5)).rgb;
		color.rgb = diffuseTex.rgb * directionalLightColor * diffuse * 0.1 + specular * 0.9 + color.rgb * 0.2;
	}

// Portrait window front
	if (mask_Image_Window.g > 0.25) {
		float shift = (mask_Image_Window.g)*1.0;

	// 	Create a radial rainbow
		vec2 center = vec2(0.904, 0.7);
		vec2 centerOffset = vec2((center.x - texCoord.x) * 1.7, center.y - texCoord.y);
		float distance = sqrt(centerOffset.x * centerOffset.x + centerOffset.y * centerOffset.y);
		
		vec3 specular = texture2D(metalMap, vec2(safeUTilt * -1.1 / halfPI+safeURot * 0.1 / halfPI, 0.5)).rgb;
		vec3 rainbow = texture2D(specularMap, vec2((safeUTilt * 0.9 + shift+distance*5.0) / halfPI + safeURot * 0.1 / halfPI, 1)).rgb;

		float normal_loc = texCoord.y + 0.35;
		float position_of_line = sigmoid(uTilt, 0.0, 0.25, 1.0, 1.5);
		float strength_for_current_position = gaussian(normal_loc, position_of_line, 0.75, 0.15);

		color.rgb = ((rainbow * 0.5 + specular * 0.3) * strength_for_current_position + color.rgb * (1.0 - strength_for_current_position));
		color.a = max(mask_Image_Window.g * strength_for_current_position / 1.0, color.a);
	}

// Main Image 	
	if (mask_Image_Window.b > 0.25 ){
	// 	The hologram with two opposing vertically moving bars is here
		float shift = (mask_Image_Window.b-0.25) / 0.73;		
		
		vec3 specular = texture2D(metalMap, vec2(safeUTilt * -1.1 / halfPI + safeURot * 0.3 / halfPI, 0.5)).rgb;
		vec3 rainbow = texture2D(specularMap, vec2((uTilt * 1.5+safeURot*0.1 + shift * 0.3) / halfPI+0.3, 1)).rgb;
		
		color.rgb = diffuseTex.rgb * directionalLightColor * diffuse * 0.4 + (specular*0.5 + (rainbow * 0.5)) * 0.8;
	}

// Emerald number
    if (mask_Emerald_Foil.r > 0.6) {
		float normal_loc = texCoord.y + uEmerald;
		float position_of_line = sigmoid(uTilt, 0.0, 2.0, 0.5, 1.0);
		float strength_for_current_position = gaussian(normal_loc, position_of_line, 0.05, 0.015);	        
		float alpha = min(mask_Emerald_Foil.r * 0.7, 1.0);

		vec4 effect = vec4(0.0196 + strength_for_current_position * 0.4549, 0.039 + strength_for_current_position * 0.8157, 0.0196 + strength_for_current_position * 0.333, color.a);
		color = vec4(effect.rgb * alpha + color.rgb * (1.0 - alpha + 0.3), effect.a);
	}

// Hologram Edges (lines and waves)
	if(maskBottomLines.b > 0.2){
		float shift = (maskBottomLines.b) / 3.0;		
		vec3 specular = texture2D(metalMap, vec2(safeUTilt * -1.1 / halfPI+safeURot * 0.1 / halfPI, 0.5)).rgb;
		vec3 rainbow = texture2D(specularMap, vec2((safeUTilt * -1.1) / halfPI + safeURot * 0.5 / halfPI+0.5, 1)).rgb;
		float normal_loc = texCoord.x + 0.35;

		//sigmoid(float x, float mean, float std, float zeroPoint, float amplitude) {
		float position_of_line = sigmoid(safeURot, 0.0, 2.0, 1.25, 1.5);
		float strength_for_current_position = gaussian(normal_loc, position_of_line, 0.05, 0.01);
		color.rgb =((rainbow * 0.5 + specular * 0.2) * strength_for_current_position + color.rgb * (1.0 - (rainbow * 0.5 + specular * 0.2) * strength_for_current_position));
		
	}
	
    gl_FragColor = color;
}