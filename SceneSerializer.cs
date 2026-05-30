using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

namespace FrostEngine
{
    public class SceneData
    {
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
    }

    public class EntityData
    {
        public string Name { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        
        public bool HasMesh { get; set; }
        public int MeshType { get; set; }
        public string TexturePath { get; set; } = string.Empty;
        
        public bool HasScript { get; set; }
        public string ScriptCode { get; set; } = string.Empty;
        
        public bool HasLight { get; set; }
        public Vector3 LightColor { get; set; }
        public float LightIntensity { get; set; }
        public float LightRange { get; set; }
    }

    public static class SceneSerializer
    {
        public static void Save(Scene scene, string path)
        {
            SceneData data = new SceneData();
            foreach (var ent in scene.GetEntities())
            {
                var eData = new EntityData
                {
                    Name = ent.Name,
                    Position = ent.Transform.Position,
                    Rotation = ent.Transform.Rotation,
                    Scale = ent.Transform.Scale
                };

                var mesh = ent.GetComponent<MeshRendererComponent>();
                if (mesh != null)
                {
                    eData.HasMesh = true;
                    eData.MeshType = (int)mesh.Type;
                    eData.TexturePath = mesh.TexturePath;
                }

                var script = ent.GetComponent<ScriptComponent>();
                if (script != null)
                {
                    eData.HasScript = true;
                    eData.ScriptCode = script.ScriptCode;
                }

                var light = ent.GetComponent<PointLightComponent>();
                if (light != null)
                {
                    eData.HasLight = true;
                    eData.LightColor = light.Color;
                    eData.LightIntensity = light.Intensity;
                    eData.LightRange = light.Range;
                }

                data.Entities.Add(eData);
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static Scene Load(string path)
        {
            if (!File.Exists(path)) return new Scene();

            string json = File.ReadAllText(path);
            SceneData? data = JsonSerializer.Deserialize<SceneData>(json);
            
            Scene scene = new Scene();
            if (data?.Entities != null)
            {
                foreach (var eData in data.Entities)
                {
                    Entity ent = new Entity(eData.Name);
                    ent.Transform.Position = eData.Position;
                    ent.Transform.Rotation = eData.Rotation;
                    ent.Transform.Scale = eData.Scale;

                    if (eData.HasMesh)
                    {
                        var mesh = new MeshRendererComponent((MeshType)eData.MeshType);
                        mesh.TexturePath = eData.TexturePath;
                        ent.AddComponent(mesh);
                    }

                    if (eData.HasScript)
                    {
                        var script = new ScriptComponent();
                        script.ScriptCode = eData.ScriptCode;
                        ent.AddComponent(script);
                    }

                    if (eData.HasLight)
                    {
                        var light = new PointLightComponent();
                        light.Color = eData.LightColor;
                        light.Intensity = eData.LightIntensity;
                        light.Range = eData.LightRange;
                        ent.AddComponent(light);
                    }

                    scene.AddEntity(ent);
                }
            }
            return scene;
        }

        // Paste this inside the SceneSerializer class
                public static void SavePrefab(Entity entity, string path)
                {
                    var eData = new EntityData
                    {
                        Name = entity.Name,
                        Position = entity.Transform.Position,
                        Rotation = entity.Transform.Rotation,
                        Scale = entity.Transform.Scale
                    };
        
                    var mesh = entity.GetComponent<MeshRendererComponent>();
                    if (mesh != null)
                    {
                        eData.HasMesh = true;
                        eData.MeshType = (int)mesh.Type;
                        eData.TexturePath = mesh.TexturePath;
                    }
        
                    var script = entity.GetComponent<ScriptComponent>();
                    if (script != null)
                    {
                        eData.HasScript = true;
                        eData.ScriptCode = script.ScriptCode;
                    }
        
                    var light = entity.GetComponent<PointLightComponent>();
                    if (light != null)
                    {
                        eData.HasLight = true;
                        eData.LightColor = light.Color;
                        eData.LightIntensity = light.Intensity;
                        eData.LightRange = light.Range;
                    }
        
                    string json = JsonSerializer.Serialize(eData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, json);
                }
        
                public static Entity LoadPrefab(string path)
                {
                    if (!File.Exists(path)) return new Entity("Broken Prefab");
        
                    string json = File.ReadAllText(path);
                    EntityData? eData = JsonSerializer.Deserialize<EntityData>(json);
                    
                    Entity ent = new Entity(eData?.Name ?? "New Prefab");
                    if (eData != null)
                    {
                        ent.Transform.Position = eData.Position;
                        ent.Transform.Rotation = eData.Rotation;
                        ent.Transform.Scale = eData.Scale;
        
                        if (eData.HasMesh)
                        {
                            var mesh = new MeshRendererComponent((MeshType)eData.MeshType);
                            mesh.TexturePath = eData.TexturePath;
                            ent.AddComponent(mesh);
                        }
        
                        if (eData.HasScript)
                        {
                            var script = new ScriptComponent();
                            script.ScriptCode = eData.ScriptCode;
                            ent.AddComponent(script);
                        }
        
                        if (eData.HasLight)
                        {
                            var light = new PointLightComponent();
                            light.Color = eData.LightColor;
                            light.Intensity = eData.LightIntensity;
                            light.Range = eData.LightRange;
                            ent.AddComponent(light);
                        }
                    }
                    return ent;
                }
    }
}
