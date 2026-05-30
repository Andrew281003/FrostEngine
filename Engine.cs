using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;
using rlImGui_cs;

namespace FrostEngine
{
    // Class matching our settings layout
    public class EngineSettings
    {
        public string ProjectPath { get; set; } = "./Projects";
        public string UserName { get; set; } = "Admin";
    }

    public class Engine
    {
        public Scene CurrentScene { get; set; }
        public string ProjectFolder { get; set; } = "./Projects";

        public Engine()
        {
            CurrentScene = new Scene();
        }

        public void Run()
        {
            // NEW: Define the standardized Program Files system directory path
            string programFilesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FrostEngine");
            string globalSettingsPath = Path.Combine(programFilesFolder, "settings.json");

            // Look for settings inside C:\Program Files\...
            if (File.Exists(globalSettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(globalSettingsPath);
                    var settings = JsonSerializer.Deserialize<EngineSettings>(json);
                    if (settings != null && !string.IsNullOrWhiteSpace(settings.ProjectPath))
                    {
                        ProjectFolder = settings.ProjectPath;
                    }
                }
                catch { /* Fallback to default if configuration layout reads broken */ }
            }

            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(1280, 720, "FrostEngine");
            Raylib.SetTargetFPS(240);

            Lighting.Initialize();
            rlImGui.Setup(true);

            Editor editor = new Editor(this);
            Editor.Log($"FrostEngine runtime active. Project directory: {ProjectFolder}");

            while (!Raylib.WindowShouldClose())
            {
                editor.Update();

                if (editor.IsPlaying)
                {
                    CurrentScene.Update();
                }

                // Clear and re-populate point lights every frame
                Lighting.ClearPointLights();

                foreach (var entity in CurrentScene.GetEntities())
                {
                    var light = entity.GetComponent<PointLightComponent>();
                    if (light != null)
                    {
                        Lighting.AddPointLight(entity.Transform.Position, light.Color, light.Intensity, light.Range);
                    }
                }

                // Send everything to the GPU
                Lighting.Update(editor.EditorCamera);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Lighting.SkyColor);

                editor.Begin3D();
                CurrentScene.Draw();

                // Render little indicator spheres exactly where your lights are in space
                // Removed to clean up the scene view

                editor.End3D();

                rlImGui.Begin();
                editor.DrawUI();
                rlImGui.End();

                Raylib.EndDrawing();
            }
        }
    }
}