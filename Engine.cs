using System;
using System.Numerics;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace FrostEngine
{
    public class Engine
    {
        public const int WindowWidth = 1280;
        public const int WindowHeight = 720;
        public Scene CurrentScene { get; private set; } = new Scene();
        private Editor? editor;
        private bool isPlayerMode;
        private Camera3D playerCamera;

        public Engine(bool playerMode = false)
        {
            isPlayerMode = playerMode;
        }

        public void Run()
        {
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(WindowWidth, WindowHeight, "FrostEngine - Editor");
            Raylib.SetTargetFPS(60);
            
            Lighting.Initialize();
            
            // CurrentScene is initialized in constructor or property, but we can clear it here if needed
            CurrentScene = new Scene();
            
            if (!isPlayerMode)
            {
                rlImGui.Setup(true);
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                editor = new Editor(this);
                
                var cube = new Entity("Default Cube");
                cube.AddComponent(new MeshRendererComponent(MeshType.Cube));
                CurrentScene.AddEntity(cube);
            }
            else
            {
                // In player mode, load the saved scene
                CurrentScene = SceneSerializer.Load("scene.json");
                
                // Initialize a basic static camera or find a camera component
                playerCamera = new Camera3D();
                playerCamera.Position = new Vector3(5, 5, 5);
                playerCamera.Target = new Vector3(0, 0, 0);
                playerCamera.Up = new Vector3(0, 1, 0);
                playerCamera.FovY = 45.0f;
                playerCamera.Projection = CameraProjection.Perspective;

                // Start all scripts
                foreach (var entity in CurrentScene.GetEntities())
                {
                    var script = entity.GetComponent<ScriptComponent>();
                    script?.StartPlay();
                }
            }

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            Raylib.EnableCursor();
            if (!isPlayerMode) rlImGui.Shutdown();
            Raylib.CloseWindow();
        }

        private void Update()
        {
            if (isPlayerMode || (editor != null && editor.IsPlaying))
            {
                foreach (var entity in CurrentScene.GetEntities())
                {
                    var script = entity.GetComponent<ScriptComponent>();
                    script?.UpdatePlay();
                }
            }
            
            CurrentScene.Update();
            if (!isPlayerMode) editor?.Update();
        }

        private void Draw()
        {
            Lighting.UpdateShader();
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Lighting.SkyColor);

            if (isPlayerMode)
            {
                Raylib.BeginMode3D(playerCamera);
                CurrentScene.Draw();
                Raylib.EndMode3D();
            }
            else
            {
                // 3D Scene Rendering
                editor?.Begin3D();
                CurrentScene.Draw();
                editor?.End3D();

                // UI Rendering
                rlImGui.Begin();
                ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
                editor?.DrawUI();
                rlImGui.End();
            }

            Raylib.EndDrawing();
        }
    }
}
