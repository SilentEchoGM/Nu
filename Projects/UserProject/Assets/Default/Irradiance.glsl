#shader vertex
#version 410 core

uniform mat4 view;
uniform mat4 projection;

layout (location = 0) in vec3 position;

out vec3 outPosition;

void main()
{
    outPosition = position;  
    gl_Position = projection * view * vec4(outPosition, 1.0);
}

#shader fragment
#version 410 core

const float PI = 3.14159265359;
const float SAMPLE_DELTA = 0.025;
const float TONE_UNMAP_SCALAR = 1.6225;

in vec3 outPosition;

out vec4 frag;

uniform samplerCube cubeMap;

void main()
{
    // compute normal
    vec3 normal = normalize(outPosition);

    // calculate tangent space
    vec3 up = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(up, normal));
    up = normalize(cross(normal, right));

    // compute irradiance
    float sampleCount = 0.0;
    vec3 irradiance = vec3(0.0);
    for (float phi = 0.0; phi < 2.0 * PI; phi += SAMPLE_DELTA)
    {
        for (float theta = 0.0; theta < 0.5 * PI; theta += SAMPLE_DELTA)
        {
            vec3 sampleTangent = vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
            vec3 sampleVector = sampleTangent.x * right + sampleTangent.y * up + sampleTangent.z * normal;
            vec3 sampleColor = texture(cubeMap, sampleVector).rgb;
            vec3 sampleScaled = sampleColor * TONE_UNMAP_SCALAR; // tone unmap
            vec3 sampleSquared = sampleScaled * sampleScaled;
            irradiance += sampleSquared * cos(theta) * sin(theta);
            ++sampleCount;
        }
    }

    // normalize irradiance
    irradiance = PI * irradiance * (1.0 / float(sampleCount));
    frag = vec4(irradiance, 1.0);
}
