#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_explicit_uniform_location : enable

in vec2 vs_textureCoordinate;
uniform sampler2D textureObject;
out vec4 color;

in vec4 gl_FragCoord;

in vec3 vPosition;

void main(void)
{
    // color = texture(textureObject, vs_textureCoordinate);
    vec3 nor = -normalize(vec3(dFdx(vPosition.z), dFdy(vPosition.z), 1.0));
    float brightness = dot(nor, normalize(vec3(0.5, -0.5, -1.0)));
    brightness = 0.8 * max(0, brightness);
    color = texture(textureObject, vs_textureCoordinate) * vec4(brightness, brightness, brightness, 1.0);
}
