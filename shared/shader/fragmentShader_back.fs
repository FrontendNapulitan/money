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
uniform sampler2D uTexRainbow;	

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
   
    vec4 maskRainbow = texture2D(uTexRainbow, texCoord);
	
	vec3 l = directionalLightDirection;
	vec3 r = reflect(l, n);
	
	float diffuse = max(0.0, dot(n, l));
	vec4 diffuseTex = texture2D(uTex, texCoord) * uMod;
// 	float offset = (diffuseTex.r * 0.5) - 1.0;
// 	float theta = acos(max(0.0, dot(r + 0.2 * offset, v)));  // 0 < theta < doublePI / 2

/*
* 	Depending on the mask colour apply the effect
*/

// Portrait window back
	if (maskRainbow.b > 0.4) {
		vec3 specular = texture2D(metalMap, vec2(safeUTilt * 1.1 / halfPI + safeURot * 0.1 / halfPI, 0.5)).rgb;
		vec3 rainbow = texture2D(specularMap, vec2((safeUTilt * 1.1 + (texCoord.y - 0.1) * 1.75) / halfPI + safeURot * 0.1 / halfPI, 1)).rgb;

		float normal_loc = texCoord.y + 0.35;
		float position_of_line = sigmoid(uTilt, 0.0, 0.5, 0.9, 1.0);
		float strength_for_current_position = gaussian(normal_loc, position_of_line, 0.3, 0.05);
		
		color.rgb = diffuseTex.rgb * directionalLightColor * diffuse * 0.1 + ((rainbow * strength_for_current_position) + color.rgb * (1.0 - strength_for_current_position)) * 0.75;
		color.a = max(maskRainbow.b * strength_for_current_position / 2.0, color.a);
	}

	
	
    gl_FragColor = color;
}