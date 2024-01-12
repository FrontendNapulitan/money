
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

uniform mat4 uMod;


void main() {	

// 	Sample the input pixels
    vec4 color = texture2D(uTex, texCoord) * uMod;
	
    gl_FragColor = color;
}