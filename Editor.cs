using System;
using System.Numerics;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace FrostEngine
{
    public class Editor
    {
        private Engine engine;
        private Entity? selectedEntity;
        public bool IsPlaying { get; private set; } = false;
        private Camera3D editorCamera;

        private bool isDragging = false;
        private Vector3 dragOffset = Vector3.Zero;
        private bool showSettingsWindow = false;
        private bool isLooking = false;

        public Editor(Engine engine)
        {
            this.engine = engine;
            editorCamera = new Camera3D();
            editorCamera.Position = new Vector3(5, 5, 5);
            editorCamera.Target = new Vector3(0, 0, 0);
            editorCamera.Up = new Vector3(0, 1, 0);
            editorCamera.FovY = 45.0f;
            editorCamera.Projection = CameraProjection.Perspective;
        }

        public void Update()
        {
            if (!IsPlaying)
            {
                Vector3 movement = Vector3.Zero;
                Vector3 rotation = Vector3.Zero;
                float zoom = Raylib.GetMouseWheelMove() * -2.0f;

                // Only start looking if right-click was pressed while not over an ImGui window
                if (Raylib.IsMouseButtonPressed(MouseButton.Right) && !ImGui.GetIO().WantCaptureMouse && Raylib.IsWindowFocused())
                {
                    isLooking = true;
                }

                if (isLooking)
                {
                    if (Raylib.IsMouseButtonDown(MouseButton.Right) && Raylib.IsWindowFocused())
                    {
                        Raylib.DisableCursor();
                        
                        Vector2 delta = Raylib.GetMouseDelta();
                        rotation.X = delta.X * 0.1f;
                        rotation.Y = delta.Y * 0.1f;

                        float speed = 0.2f;
                        if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) speed = 0.6f;
                        
                        if (Raylib.IsKeyDown(KeyboardKey.W)) movement.X = speed;
                        if (Raylib.IsKeyDown(KeyboardKey.S)) movement.X = -speed;
                        if (Raylib.IsKeyDown(KeyboardKey.D)) movement.Y = speed;
                        if (Raylib.IsKeyDown(KeyboardKey.A)) movement.Y = -speed;
                        if (Raylib.IsKeyDown(KeyboardKey.E)) movement.Z = speed;
                        if (Raylib.IsKeyDown(KeyboardKey.Q)) movement.Z = -speed;
                    }
                    else
                    {
                        isLooking = false;
                        Raylib.EnableCursor();
                    }
                }
                else
                {
                    Raylib.EnableCursor();

                    // Viewport Selection and Dragging
                    if (!ImGui.GetIO().WantCaptureMouse)
                    {
                        Ray ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), editorCamera);

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            if (Raylib.IsKeyDown(KeyboardKey.LeftShift) && selectedEntity != null)
                            {
                                float h = selectedEntity.Transform.Position.Y;
                                if (MathF.Abs(ray.Direction.Y) > 0.0001f)
                                {
                                    float t = (h - ray.Position.Y) / ray.Direction.Y;
                                    if (t >= 0)
                                    {
                                        Vector3 hitPoint = ray.Position + ray.Direction * t;
                                        dragOffset = selectedEntity.Transform.Position - hitPoint;
                                        isDragging = true;
                                    }
                                }
                            }
                            else
                            {
                                Entity? closestEntity = null;
                                float closestDist = float.MaxValue;

                                foreach (var entity in engine.CurrentScene.GetEntities())
                                {
                                    Vector3 halfScale = entity.Transform.Scale * 0.5f;
                                    BoundingBox box = new BoundingBox(entity.Transform.Position - halfScale, entity.Transform.Position + halfScale);
                                    RayCollision col = Raylib.GetRayCollisionBox(ray, box);
                                    if (col.Hit && col.Distance < closestDist)
                                    {
                                        closestDist = col.Distance;
                                        closestEntity = entity;
                                    }
                                }

                                if (closestEntity != null)
                                {
                                    selectedEntity = closestEntity;
                                }
                            }
                        }

                        if (isDragging && selectedEntity != null)
                        {
                            if (Raylib.IsMouseButtonDown(MouseButton.Left))
                            {
                                float h = selectedEntity.Transform.Position.Y;
                                if (MathF.Abs(ray.Direction.Y) > 0.0001f)
                                {
                                    float t = (h - ray.Position.Y) / ray.Direction.Y;
                                    if (t >= 0)
                                    {
                                        Vector3 hitPoint = ray.Position + ray.Direction * t;
                                        selectedEntity.Transform.Position = hitPoint + dragOffset;
                                    }
                                }
                            }
                            else
                            {
                                isDragging = false;
                            }
                        }
                    }
                }

                Raylib.UpdateCameraPro(ref editorCamera, movement, rotation, zoom);
            }
        }

        public void Begin3D()
        {
            Raylib.BeginMode3D(editorCamera);
            
            // Infinite Workspace Grid trick (draw grid around camera X/Z)
            Rlgl.PushMatrix();
            Rlgl.Translatef((float)Math.Round(editorCamera.Position.X), 0, (float)Math.Round(editorCamera.Position.Z));
            Raylib.DrawGrid(100, 1.0f);
            Rlgl.PopMatrix();
        }

        public void End3D()
        {
            Raylib.EndMode3D();
        }

        public void DrawUI()
        {
            DrawMainMenuBar();
            DrawToolbar();
            DrawHierarchy();
            DrawInspector();
            if (showSettingsWindow) DrawSettingsWindow();
        }

        private void DrawMainMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Settings & Lighting")) showSettingsWindow = true;
                    if (ImGui.MenuItem("Exit")) Environment.Exit(0);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("GameObject"))
                {
                    if (ImGui.MenuItem("Create Empty"))
                    {
                        engine.CurrentScene.AddEntity(new Entity("New Entity"));
                    }
                    if (ImGui.MenuItem("Create Cube"))
                    {
                        var e = new Entity("Cube");
                        e.AddComponent(new MeshRendererComponent(MeshType.Cube));
                        engine.CurrentScene.AddEntity(e);
                    }
                    if (ImGui.MenuItem("Create Sphere"))
                    {
                        var e = new Entity("Sphere");
                        e.AddComponent(new MeshRendererComponent(MeshType.Sphere));
                        engine.CurrentScene.AddEntity(e);
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

        private void DrawToolbar()
        {
            ImGui.SetNextWindowPos(new Vector2(0, 20));
            ImGui.SetNextWindowSize(new Vector2(Raylib.GetScreenWidth(), 40));
            ImGui.Begin("Toolbar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
            
            float buttonWidth = 80;
            ImGui.SetCursorPosX((Raylib.GetScreenWidth() - buttonWidth * 2) * 0.5f);
            
            if (!IsPlaying)
            {
                if (ImGui.Button("Play", new Vector2(buttonWidth, 25)))
                {
                    IsPlaying = true;
                    foreach (var entity in engine.CurrentScene.GetEntities())
                    {
                        var script = entity.GetComponent<ScriptComponent>();
                        script?.StartPlay();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Build Game", new Vector2(buttonWidth, 25)))
                {
                    GameBuilder.BuildGame(engine.CurrentScene);
                }
            }
            else
            {
                if (ImGui.Button("Stop", new Vector2(buttonWidth, 25)))
                {
                    IsPlaying = false;
                    foreach (var entity in engine.CurrentScene.GetEntities())
                    {
                        var script = entity.GetComponent<ScriptComponent>();
                        script?.StopPlay();
                    }
                }
            }
            
            ImGui.End();
        }

        private void DrawHierarchy()
        {
            ImGui.SetNextWindowPos(new Vector2(0, 60));
            ImGui.SetNextWindowSize(new Vector2(250, Raylib.GetScreenHeight() - 60));
            ImGui.Begin("Scene Hierarchy", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            foreach (var entity in engine.CurrentScene.GetEntities())
            {
                bool isSelected = selectedEntity == entity;
                if (ImGui.Selectable(entity.Name, isSelected))
                {
                    selectedEntity = entity;
                }
            }

            ImGui.End();
        }

        private void DrawInspector()
        {
            ImGui.SetNextWindowPos(new Vector2(Raylib.GetScreenWidth() - 300, 60));
            ImGui.SetNextWindowSize(new Vector2(300, Raylib.GetScreenHeight() - 60));
            ImGui.Begin("Inspector", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            if (selectedEntity != null)
            {
                string name = selectedEntity.Name;
                if (ImGui.InputText("Name", ref name, 100))
                {
                    selectedEntity.Name = name;
                }

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.DragFloat3("Position", ref selectedEntity.Transform.Position, 0.1f);
                    ImGui.DragFloat3("Rotation", ref selectedEntity.Transform.Rotation, 1.0f);
                    ImGui.DragFloat3("Scale", ref selectedEntity.Transform.Scale, 0.1f);
                }

                foreach (var component in selectedEntity.GetComponents())
                {
                    if (component is TransformComponent) continue;

                    if (component is MeshRendererComponent meshRenderer)
                    {
                        if (ImGui.CollapsingHeader("Mesh Renderer", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            int currentType = (int)meshRenderer.Type;
                            string[] types = { "Cube", "Sphere", "Plane" };
                            if (ImGui.Combo("Type", ref currentType, types, types.Length))
                            {
                                meshRenderer.Type = (MeshType)currentType;
                            }

                            Vector4 color = new Vector4(meshRenderer.Tint.R / 255f, meshRenderer.Tint.G / 255f, meshRenderer.Tint.B / 255f, meshRenderer.Tint.A / 255f);
                            if (ImGui.ColorEdit4("Tint", ref color))
                            {
                                meshRenderer.Tint = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));
                            }

                            string texPath = meshRenderer.TexturePath ?? "";
                            if (ImGui.InputText("Texture Path", ref texPath, 500))
                            {
                                meshRenderer.TexturePath = texPath;
                            }
                        }
                    }
                    else if (component is ScriptComponent scriptComp)
                    {
                        if (ImGui.CollapsingHeader("Script", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.InputTextMultiline("Code", ref scriptComp.ScriptCode, 10000, new Vector2(-1, 200));
                        }
                    }
                }

                ImGui.Separator();
                if (ImGui.Button("Add Component", new Vector2(-1, 30)))
                {
                    ImGui.OpenPopup("AddComponentPopup");
                }

                if (ImGui.BeginPopup("AddComponentPopup"))
                {
                    if (ImGui.MenuItem("Mesh Renderer"))
                    {
                        if (selectedEntity.GetComponent<MeshRendererComponent>() == null)
                            selectedEntity.AddComponent(new MeshRendererComponent(MeshType.Cube));
                    }
                    if (ImGui.MenuItem("Script Component"))
                    {
                        if (selectedEntity.GetComponent<ScriptComponent>() == null)
                            selectedEntity.AddComponent(new ScriptComponent());
                    }
                    ImGui.EndPopup();
                }
            }

            ImGui.End();
        }

        private void DrawSettingsWindow()
        {
            ImGui.Begin("Settings & Lighting", ref showSettingsWindow);
            
            if (ImGui.CollapsingHeader("Ambient Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.ColorEdit3("Ambient Color", ref Lighting.AmbientColor);
            }
            
            if (ImGui.CollapsingHeader("Directional Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.DragFloat3("Direction", ref Lighting.LightDirection, 0.05f, -1.0f, 1.0f);
                ImGui.ColorEdit3("Light Color", ref Lighting.LightColor);
            }
            
            if (ImGui.CollapsingHeader("Environment", ImGuiTreeNodeFlags.DefaultOpen))
            {
                Vector4 sky = new Vector4(Lighting.SkyColor.R / 255f, Lighting.SkyColor.G / 255f, Lighting.SkyColor.B / 255f, Lighting.SkyColor.A / 255f);
                if (ImGui.ColorEdit4("Sky Color", ref sky))
                {
                    Lighting.SkyColor = new Color((byte)(sky.X * 255), (byte)(sky.Y * 255), (byte)(sky.Z * 255), (byte)(sky.W * 255));
                }
            }

            if (ImGui.CollapsingHeader("Editor Camera", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.DragFloat("FOV", ref editorCamera.FovY, 1.0f, 10.0f, 120.0f);
            }

            ImGui.End();
        }
    }
}
