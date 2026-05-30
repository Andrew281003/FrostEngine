using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;

namespace FrostEngine
{
    public static class GameBuilder
    {
        public static void BuildGame(Scene currentScene)
        {
            try
            {
                string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Output");

                if (Directory.Exists(buildDir)) Directory.Delete(buildDir, true);
                Directory.CreateDirectory(buildDir);

                // Save the scene for the export (and fix texture paths to be relative)
                SceneData data = new SceneData();
                foreach (var ent in currentScene.GetEntities())
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

                        if (!string.IsNullOrEmpty(mesh.TexturePath) && File.Exists(mesh.TexturePath))
                        {
                            string texName = Path.GetFileName(mesh.TexturePath);
                            eData.TexturePath = texName; // Make relative for player

                            string dest = Path.Combine(buildDir, texName);
                            if (!File.Exists(dest)) File.Copy(mesh.TexturePath, dest);
                        }
                    }

                    var script = ent.GetComponent<ScriptComponent>();
                    if (script != null)
                    {
                        eData.HasScript = true;
                        eData.ScriptCode = script.ScriptCode;
                    }

                    data.Entities.Add(eData);
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(buildDir, "scene.json"), json);

                Console.WriteLine("Building executable...");
                ProcessStartInfo psi = new ProcessStartInfo("dotnet", "publish -c Release -o \"" + buildDir + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                var proc = Process.Start(psi);
                proc?.WaitForExit();

                // Create a launch script for convenience
                File.WriteAllText(Path.Combine(buildDir, "PlayGame.bat"), "@echo off\ntitle FrostEngine Game\nFrostEngine.exe --play\n");

                Console.WriteLine("Build Complete!");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error occured, please try again.\n\n\n\n" + ex);
            }
        }
    }
}
