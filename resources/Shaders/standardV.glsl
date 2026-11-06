#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_explicit_uniform_location : enable

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 textureCoordinate;
out vec2 vs_textureCoordinate;

layout(location = 20) uniform mat4 projection;
layout(location = 21) uniform mat4 modelView;

out vec3 vPosition;

void main(void)
{
    vs_textureCoordinate = textureCoordinate;

    gl_Position = projection * modelView * vec4(position, 1.0);
    vPosition = gl_Position.xyz;
}
