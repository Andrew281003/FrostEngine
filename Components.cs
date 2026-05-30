using System;
using System.Numerics;
using Raylib_cs;
using NLua;

namespace FrostEngine
{
    public class TransformComponent : Component
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero; // Euler angles in degrees
        public Vector3 Scale = Vector3.One;
    }

    public class PointLightComponent : Component
    {
        public Vector3 Color = new Vector3(1.0f, 1.0f, 1.0f);
        public float Intensity = 1.0f;
        public float Range = 10.0f;
    }

    // NEW: Added Cylinder and Cone shapes!
    public enum MeshType { Cube, Sphere, Plane, Cylinder, Cone }

    public class MeshRendererComponent : Component
    {
        public MeshType Type = MeshType.Cube;
        public Color Tint = Color.White;
        public string TexturePath = "";

        private Model model;
        private string loadedTexturePath = "";
        private MeshType loadedType;

        public MeshRendererComponent(MeshType type)
        {
            Type = type;
            LoadModel();
        }

        private void LoadModel()
        {
            Mesh mesh = Type switch
            {
                MeshType.Cube => Raylib.GenMeshCube(1, 1, 1),
                MeshType.Sphere => Raylib.GenMeshSphere(0.5f, 16, 16),
                MeshType.Plane => Raylib.GenMeshPlane(1, 1, 1, 1),
                MeshType.Cylinder => Raylib.GenMeshCylinder(0.5f, 1.0f, 16),
                MeshType.Cone => Raylib.GenMeshCone(0.5f, 1.0f, 16),
                _ => Raylib.GenMeshCube(1, 1, 1)
            };
            model = Raylib.LoadModelFromMesh(mesh);
            
            unsafe
            {
                model.Materials[0].Shader = Lighting.DefaultShader;
            }
            
            loadedType = Type;
        }

        public override void Draw()
        {
            if (Entity == null || Entity.Transform == null) return;

            if (Type != loadedType) LoadModel();

            if (TexturePath != loadedTexturePath)
            {
                loadedTexturePath = TexturePath;
                if (!string.IsNullOrEmpty(TexturePath) && System.IO.File.Exists(TexturePath))
                {
                    Texture2D tex = Raylib.LoadTexture(TexturePath);
                    unsafe
                    {
                        Material* matPtr = &model.Materials[0];
                        Raylib.SetMaterialTexture(matPtr, MaterialMapIndex.Albedo, tex);
                    }
                }
            }

            var t = Entity.Transform;
            
            Matrix4x4 matScale = Matrix4x4.CreateScale(t.Scale);
            Matrix4x4 matRot = Matrix4x4.CreateFromYawPitchRoll(t.Rotation.Y * MathF.PI / 180f, t.Rotation.X * MathF.PI / 180f, t.Rotation.Z * MathF.PI / 180f);
            Matrix4x4 matTrans = Matrix4x4.CreateTranslation(t.Position);
            
            Matrix4x4 finalTransform = matScale * matRot * matTrans;
            model.Transform = Matrix4x4.Transpose(finalTransform);
            
            Raylib.DrawModel(model, Vector3.Zero, 1.0f, Tint);
        }
    }

    public class ScriptComponent : Component
    {
        public string ScriptCode = "-- Lua Script\nfunction start()\nend\n\nfunction update()\nend";
        private Lua? luaState;

        public override void Update()
        {
            UpdatePlay();
        }

        public void StartPlay()
        {
            if (Entity == null) return;
            try
            {
                var state = new Lua();
                state["entity"] = Entity;
                state["transform"] = Entity.Transform;
                state.DoString(ScriptCode);
                var startFunc = state["start"] as LuaFunction;
                startFunc?.Call();
                luaState = state;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script Error (Start): {ex.Message}");
            }
        }

        public void UpdatePlay()
        {
            if (luaState != null)
            {
                try
                {
                    var updateFunc = luaState["update"] as LuaFunction;
                    updateFunc?.Call();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Script Error (Update): {ex.Message}");
                }
            }
        }

        public void StopPlay()
        {
            luaState?.Dispose();
            luaState = null;
        }
    }
}