using System;
using System.Numerics;
using Raylib_cs;

namespace FrostEngine
{
    public static class Lighting
    {
        public static Shader DefaultShader;
        
        public static Vector3 AmbientColor = new Vector3(0.2f, 0.2f, 0.2f);
        public static Vector3 LightDirection = new Vector3(-1, -1, -1);
        public static Vector3 LightColor = new Vector3(1.0f, 1.0f, 1.0f);
        public static Color SkyColor = new Color(135, 206, 235, 255);

        private static int ambientLoc;
        private static int lightDirLoc;
        private static int lightColorLoc;

        private const string VertexShader = @"
#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;

out vec3 fragPosition;
out vec2 fragTexCoord;
out vec3 fragNormal;
out vec4 fragColor;

void main()
{
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    fragTexCoord = vertexTexCoord;
    fragNormal = normalize(vec3(matNormal * vec4(vertexNormal, 1.0)));
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
";

        private const string FragmentShader = @"
#version 330
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

uniform vec3 ambient;
uniform vec3 lightDir;
uniform vec3 lightCol;

out vec4 finalColor;

void main()
{
    vec4 texelColor = texture(texture0, fragTexCoord);
    
    vec3 normal = normalize(fragNormal);
    vec3 light = normalize(-lightDir);
    
    float NdotL = max(dot(normal, light), 0.0);
    vec3 diffuse = lightCol * NdotL;
    
    vec3 lighting = ambient + diffuse;
    
    finalColor = texelColor * colDiffuse * vec4(lighting, 1.0);
}
";

        public static void Initialize()
        {
            DefaultShader = Raylib.LoadShaderFromMemory(VertexShader, FragmentShader);
            ambientLoc = Raylib.GetShaderLocation(DefaultShader, "ambient");
            lightDirLoc = Raylib.GetShaderLocation(DefaultShader, "lightDir");
            lightColorLoc = Raylib.GetShaderLocation(DefaultShader, "lightCol");
        }

        public static void UpdateShader()
        {
            Raylib.SetShaderValue(DefaultShader, ambientLoc, AmbientColor, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(DefaultShader, lightDirLoc, LightDirection, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(DefaultShader, lightColorLoc, LightColor, ShaderUniformDataType.Vec3);
        }
    }
}
