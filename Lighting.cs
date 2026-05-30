using System;
using System.Numerics;
using Raylib_cs;

namespace FrostEngine
{
    public static class Lighting
    {
        public static Vector3 AmbientColor = new Vector3(0.15f, 0.15f, 0.15f);
        public static Vector3 LightDirection = new Vector3(-1.0f, -1.0f, -1.0f);
        public static Vector3 LightColor = new Vector3(1.0f, 1.0f, 0.9f);
        public static Color SkyColor = new Color(20, 20, 25, 255);

        public static Shader DefaultShader;

        private static int ambientLoc;
        private static int lightDirLoc;
        private static int lightColLoc;
        private static int viewPosLoc;

        public const int MAX_LIGHTS = 8;
        public static int ActiveLightCount = 0;

        // Flattened light array buffers for perfect Raylib compatibility
        private static float[] lightPositions = new float[MAX_LIGHTS * 3];
        private static float[] lightColors = new float[MAX_LIGHTS * 3];
        private static float[] lightIntensities = new float[MAX_LIGHTS];
        private static float[] lightRanges = new float[MAX_LIGHTS];

        public static unsafe void Initialize()
        {
            DefaultShader = Raylib.LoadShaderFromMemory(VertexShader, FragmentShader);

            viewPosLoc = Raylib.GetShaderLocation(DefaultShader, "viewPos");
            ambientLoc = Raylib.GetShaderLocation(DefaultShader, "ambientColor");
            lightDirLoc = Raylib.GetShaderLocation(DefaultShader, "lightDirection");
            lightColLoc = Raylib.GetShaderLocation(DefaultShader, "lightColor");
        }

        public static void ClearPointLights()
        {
            ActiveLightCount = 0;
        }

        public static void AddPointLight(Vector3 position, Vector3 color, float intensity, float range)
        {
            if (ActiveLightCount < MAX_LIGHTS)
            {
                int index3 = ActiveLightCount * 3;

                lightPositions[index3 + 0] = position.X;
                lightPositions[index3 + 1] = position.Y;
                lightPositions[index3 + 2] = position.Z;

                lightColors[index3 + 0] = color.X;
                lightColors[index3 + 1] = color.Y;
                lightColors[index3 + 2] = color.Z;

                lightIntensities[ActiveLightCount] = intensity;
                lightRanges[ActiveLightCount] = range;

                ActiveLightCount++;
            }
        }

        // Helper to pull positions back out safely for debug drawing
        public static Vector3 GetLightPosition(int index)
        {
            if (index < 0 || index >= ActiveLightCount) return Vector3.Zero;
            return new Vector3(lightPositions[index * 3], lightPositions[index * 3 + 1], lightPositions[index * 3 + 2]);
        }

        public static unsafe void Update(Camera3D camera)
        {
            // Update scene basic uniforms
            Vector3 camPos = camera.Position;
            Raylib.SetShaderValue(DefaultShader, viewPosLoc, &camPos, ShaderUniformDataType.Vec3);
            Vector3 ambColor = AmbientColor;
            Raylib.SetShaderValue(DefaultShader, ambientLoc, &ambColor, ShaderUniformDataType.Vec3);

            Vector3 normalizedDir = Vector3.Normalize(LightDirection);
            Raylib.SetShaderValue(DefaultShader, lightDirLoc, &normalizedDir, ShaderUniformDataType.Vec3);
            Vector3 lightCol = LightColor;
            Raylib.SetShaderValue(DefaultShader, lightColLoc, &lightCol, ShaderUniformDataType.Vec3);

            int countLoc = Raylib.GetShaderLocation(DefaultShader, "activeLightCount");

            // FIX 1: Removed the '&' pointer! 
            // Now it passes the actual number (e.g., 2) instead of a massive memory address.
            int activeCount = ActiveLightCount;
            Raylib.SetShaderValue(DefaultShader, countLoc, &activeCount, ShaderUniformDataType.Int);

            if (ActiveLightCount > 0)
            {
                // FIX 2: Added '[0]' to guarantee OpenGL finds the array base pointers across all drivers
                int posLoc = Raylib.GetShaderLocation(DefaultShader, "lightPositions[0]");
                int colLoc = Raylib.GetShaderLocation(DefaultShader, "lightColors[0]");
                int intLoc = Raylib.GetShaderLocation(DefaultShader, "lightIntensities[0]");
                int rngLoc = Raylib.GetShaderLocation(DefaultShader, "lightRanges[0]");

                fixed (float* pPos = lightPositions)
                    Raylib.SetShaderValueV(DefaultShader, posLoc, pPos, ShaderUniformDataType.Vec3, ActiveLightCount);

                fixed (float* pCol = lightColors)
                    Raylib.SetShaderValueV(DefaultShader, colLoc, pCol, ShaderUniformDataType.Vec3, ActiveLightCount);

                fixed (float* pInt = lightIntensities)
                    Raylib.SetShaderValueV(DefaultShader, intLoc, pInt, ShaderUniformDataType.Float, ActiveLightCount);

                fixed (float* pRng = lightRanges)
                    Raylib.SetShaderValueV(DefaultShader, rngLoc, pRng, ShaderUniformDataType.Float, ActiveLightCount);
            }
        }

        private const string VertexShader = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragPosition;
out vec2 fragTexCoord;
out vec3 fragNormal;
out vec4 fragColor;

void main()
{
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    fragTexCoord = vertexTexCoord;
    fragNormal = normalize(vec3(matModel * vec4(vertexNormal, 0.0)));
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
";

        private const string FragmentShader = @"#version 330
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

uniform vec3 ambientColor;
uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 viewPos;

#define MAX_LIGHTS 8
uniform vec3 lightPositions[MAX_LIGHTS];
uniform vec3 lightColors[MAX_LIGHTS];
uniform float lightIntensities[MAX_LIGHTS];
uniform float lightRanges[MAX_LIGHTS];
uniform int activeLightCount;

out vec4 finalColor;

void main()
{
    // Base color from texture and material tint
    vec4 texelColor = texture(texture0, fragTexCoord) * colDiffuse;
    
    // Fallback: If vertex colors are present and not fully transparent/black, blend them
    if (fragColor.a > 0.01) {
        texelColor *= fragColor;
    }
    
    vec3 normal = normalize(fragNormal);
    vec3 viewDir = normalize(viewPos - fragPosition);
    
    vec3 diffuseLighting = ambientColor;
    vec3 specularLighting = vec3(0.0);
    
    // Global Directional Light
    vec3 sunDir = normalize(-lightDirection);
    float sunDiff = max(dot(normal, sunDir), 0.0);
    diffuseLighting += sunDiff * lightColor;
    
    vec3 sunReflectDir = reflect(-sunDir, normal);
    float sunSpec = pow(max(dot(viewDir, sunReflectDir), 0.0), 32.0);
    specularLighting += 0.6 * sunSpec * lightColor;
    
    // Point Lights Processor
    for (int i = 0; i < activeLightCount; i++) {
        vec3 lightVector = lightPositions[i] - fragPosition;
        float distance = length(lightVector);
        
        if (distance < lightRanges[i]) {
            vec3 lightDir = normalize(lightVector);
            
            // Diffuse
            float diff = max(dot(normal, lightDir), 0.0);
            
            // Specular
            vec3 reflectDir = reflect(-lightDir, normal);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
            
            // Attenuation
            float attenuation = 1.0 / (1.0 + (2.0 / lightRanges[i]) * distance + (1.0 / (lightRanges[i] * lightRanges[i])) * (distance * distance));
            attenuation *= lightIntensities[i];
            
            diffuseLighting += (diff * lightColors[i]) * attenuation;
            specularLighting += (spec * lightColors[i] * 0.6) * attenuation;
        }
    }
    
    // Specular should be added AFTER the base color is multiplied by diffuse
    vec3 finalRGB = (diffuseLighting * texelColor.rgb) + specularLighting;
    
    // Simple tone mapping / clamp to keep it from washing out completely
    finalRGB = min(finalRGB, vec3(1.0));
    
    finalColor = vec4(finalRGB, texelColor.a);
}
";
    }
}