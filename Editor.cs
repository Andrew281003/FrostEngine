using System;
using System.Numerics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

namespace FrostEngine
{
    // Icon font definitions mapped cleanly to match FontAwesome 7 Solid
    public static class Icons
    {
        public const string Play = "\uf04b";
        public const string Stop = "\uf04d";
        public const string Build = "\uf0ad";
        public const string Save = "\uf0c7";
        public const string Folder = "\uf07b";
        public const string Cube = "\uf1b2";
        public const string Trash = "\uf1f8";
        public const string Settings = "\uf013";
        public const string Box = "\uf466";
        public const string Search = "\uf002";
        public const string Terminal = "\uf120";
    }

    public class Editor
    {
        private Engine engine;
        private Entity? selectedEntity;

        public bool IsPlaying { get; private set; } = false;
        private Camera3D editorCamera;
        public Camera3D EditorCamera => editorCamera;

        private bool isDragging = false;
        private bool showSettingsWindow = false;
        private bool showConsoleLog = false; // Optional layout toggle
        private bool isLooking = false;
        private Vector2 savedMousePos;
        private Vector3 dragOffset = Vector3.Zero;
        private bool isRotatingGizmo = false;
        private bool isScalingGizmo = false;
        private bool themeSet = false;
        private string saveSceneName = "my_scene.json";

        private enum GizmoAxis { None, X, Y, Z }
        private GizmoAxis activeAxis = GizmoAxis.None;

        // FIXED: Kept exactly one clean declaration pair for your wizard inputs!
        private string wizardPathInput = "C:/FrostEngineProjects";
        private string wizardNameInput = "Developer";
        private string errorMessage = ""; 

        // Custom Embedded Console Log Storage
        private static List<string> logMessages = new List<string> { "FrostEngine initialized successfully." };

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

        public static void Log(string message)
        {
            logMessages.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
        }

        private void SetupUnityTheme()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 4.0f;
            style.FrameRounding = 3.0f;
            style.GrabRounding = 3.0f;
            style.PopupRounding = 4.0f;
            style.WindowPadding = new Vector2(8, 8);
            style.FramePadding = new Vector2(4, 3);
            style.ItemSpacing = new Vector2(8, 4);

            var colors = style.Colors;
            colors[(int)ImGuiCol.Text] = new Vector4(0.85f, 0.85f, 0.85f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.28f, 0.28f, 0.28f, 1.00f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.35f, 0.35f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.50f, 0.80f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.28f, 0.28f, 0.28f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.22f, 0.50f, 0.80f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.22f, 0.50f, 0.80f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.22f, 0.50f, 0.80f, 1.00f);

            themeSet = true;
        }

        public void Update()
        {
            if (!IsPlaying)
            {
                Vector3 movement = Vector3.Zero;
                Vector3 rotation = Vector3.Zero;
                float zoom = Raylib.GetMouseWheelMove() * -2.0f;

                if (Raylib.IsMouseButtonPressed(MouseButton.Right) && !ImGui.GetIO().WantCaptureMouse && Raylib.IsWindowFocused())
                {
                    isLooking = true;
                    savedMousePos = Raylib.GetMousePosition();
                    Raylib.DisableCursor();
                }

                if (isLooking)
                {
                    if (Raylib.IsMouseButtonDown(MouseButton.Right) && Raylib.IsWindowFocused())
                    {
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
                        Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
                    }
                }
                else
                {
                    if (!ImGui.GetIO().WantCaptureMouse)
                    {
                        Ray ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), editorCamera);

                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            bool hitGizmo = false;
                            if (selectedEntity != null)
                            {
                                Vector3 pos = selectedEntity.Transform.Position;

                                // Tight bounding boxes around the arrows for accurate collision selection
                                BoundingBox boxX = new BoundingBox(pos + new Vector3(0.1f, -0.1f, -0.1f), pos + new Vector3(2.5f, 0.1f, 0.1f));
                                BoundingBox boxY = new BoundingBox(pos + new Vector3(-0.1f, 0.1f, -0.1f), pos + new Vector3(0.1f, 2.5f, 0.1f));
                                BoundingBox boxZ = new BoundingBox(pos + new Vector3(-0.1f, -0.1f, 0.1f), pos + new Vector3(0.1f, 0.1f, 2.5f));

                                if (Raylib.GetRayCollisionBox(ray, boxX).Hit) { activeAxis = GizmoAxis.X; isDragging = true; hitGizmo = true; }
                                else if (Raylib.GetRayCollisionBox(ray, boxY).Hit) { activeAxis = GizmoAxis.Y; isDragging = true; hitGizmo = true; }
                                else if (Raylib.GetRayCollisionBox(ray, boxZ).Hit) { activeAxis = GizmoAxis.Z; isDragging = true; hitGizmo = true; }

                                // SUCCESS: Calculate initial drag offset to prevent instant snapping jumps!
                                if (hitGizmo)
                                {
                                    isRotatingGizmo = Raylib.IsKeyDown(KeyboardKey.LeftShift);
                                    isScalingGizmo = Raylib.IsKeyDown(KeyboardKey.LeftControl);
                                    
                                    if (!isRotatingGizmo && !isScalingGizmo)
                                    {
                                        float t = 0;
                                        if (activeAxis == GizmoAxis.X || activeAxis == GizmoAxis.Z)
                                        {
                                            if (MathF.Abs(ray.Direction.Y) > 0.0001f)
                                            {
                                                t = (pos.Y - ray.Position.Y) / ray.Direction.Y;
                                                Vector3 initialHitPoint = ray.Position + ray.Direction * t;
                                                dragOffset = pos - initialHitPoint; 
                                            }
                                        }
                                        else if (activeAxis == GizmoAxis.Y)
                                        {
                                            if (MathF.Abs(ray.Direction.Z) > 0.0001f)
                                            {
                                                t = (pos.Z - ray.Position.Z) / ray.Direction.Z;
                                                Vector3 initialHitPoint = ray.Position + ray.Direction * t;
                                                dragOffset = pos - initialHitPoint;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!hitGizmo)
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
                                selectedEntity = closestEntity;
                                activeAxis = GizmoAxis.None;
                            }
                        }
                    }

                    if (isDragging && selectedEntity != null)
                    {
                        if (Raylib.IsMouseButtonDown(MouseButton.Left))
                        {
                            if (isRotatingGizmo)
                            {
                                Vector2 mouseDelta = Raylib.GetMouseDelta();
                                float rotSpeed = 0.5f;
                                if (activeAxis == GizmoAxis.X) selectedEntity.Transform.Rotation.X += mouseDelta.Y * rotSpeed;
                                if (activeAxis == GizmoAxis.Y) selectedEntity.Transform.Rotation.Y += mouseDelta.X * rotSpeed;
                                if (activeAxis == GizmoAxis.Z) selectedEntity.Transform.Rotation.Z -= mouseDelta.Y * rotSpeed;
                            }
                            else if (isScalingGizmo)
                            {
                                Vector2 mouseDelta = Raylib.GetMouseDelta();
                                float scaleSpeed = 0.02f;
                                if (activeAxis == GizmoAxis.X) selectedEntity.Transform.Scale.X += mouseDelta.X * scaleSpeed;
                                if (activeAxis == GizmoAxis.Y) selectedEntity.Transform.Scale.Y -= mouseDelta.Y * scaleSpeed;
                                if (activeAxis == GizmoAxis.Z) selectedEntity.Transform.Scale.Z -= mouseDelta.Y * scaleSpeed;
                                
                                // Prevent negative scale flip
                                if (selectedEntity.Transform.Scale.X < 0.01f) selectedEntity.Transform.Scale.X = 0.01f;
                                if (selectedEntity.Transform.Scale.Y < 0.01f) selectedEntity.Transform.Scale.Y = 0.01f;
                                if (selectedEntity.Transform.Scale.Z < 0.01f) selectedEntity.Transform.Scale.Z = 0.01f;
                            }
                            else
                            {
                                Ray ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), editorCamera);
                                float t = 0;
                                Vector3 hitPoint = Vector3.Zero;

                                if (activeAxis == GizmoAxis.X || activeAxis == GizmoAxis.Z)
                                {
                                    if (MathF.Abs(ray.Direction.Y) > 0.0001f)
                                    {
                                        // Use the entity's *current* position anchor to evaluate the plane intersection line
                                        t = (selectedEntity.Transform.Position.Y - ray.Position.Y) / ray.Direction.Y;
                                        hitPoint = ray.Position + ray.Direction * t;

                                        // Apply new ray projection coordinate alongside our pre-calculated click offset handle
                                        if (activeAxis == GizmoAxis.X) selectedEntity.Transform.Position.X = hitPoint.X + dragOffset.X;
                                        if (activeAxis == GizmoAxis.Z) selectedEntity.Transform.Position.Z = hitPoint.Z + dragOffset.Z;
                                    }
                                }
                                else if (activeAxis == GizmoAxis.Y)
                                {
                                    if (MathF.Abs(ray.Direction.Z) > 0.0001f)
                                    {
                                        t = (selectedEntity.Transform.Position.Z - ray.Position.Z) / ray.Direction.Z;
                                        hitPoint = ray.Position + ray.Direction * t;
                                        selectedEntity.Transform.Position.Y = hitPoint.Y + dragOffset.Y;
                                    }
                                }
                            }
                        }
                        else
                        {
                            isDragging = false;
                            activeAxis = GizmoAxis.None;
                        }
                    }
                }

                Raylib.UpdateCameraPro(ref editorCamera, movement, rotation, zoom);
            }
        }

        public void Begin3D()
        {
            Raylib.BeginMode3D(editorCamera);
            Lighting.Update(editorCamera);

            Rlgl.PushMatrix();
            Rlgl.Translatef((float)Math.Round(editorCamera.Position.X), 0, (float)Math.Round(editorCamera.Position.Z));
            Raylib.DrawGrid(100, 1.0f);
            Rlgl.PopMatrix();

            DrawGizmos();
        }

        public void End3D() { Raylib.EndMode3D(); }

        public void DrawGizmos()
        {
            if (selectedEntity != null && !IsPlaying)
            {
                Vector3 pos = selectedEntity.Transform.Position;
                float length = 2.0f;
                float radius = 0.15f;

                if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
                {
                    int segments = 32;
                    float r = 2.0f;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle1 = (i / (float)segments) * MathF.PI * 2;
                        float angle2 = ((i + 1) / (float)segments) * MathF.PI * 2;
                        
                        // X axis (Red) - circle in YZ plane
                        Vector3 p1X = pos + new Vector3(0, MathF.Cos(angle1) * r, MathF.Sin(angle1) * r);
                        Vector3 p2X = pos + new Vector3(0, MathF.Cos(angle2) * r, MathF.Sin(angle2) * r);
                        Raylib.DrawLine3D(p1X, p2X, Color.Red);
                        
                        // Y axis (Green) - circle in XZ plane
                        Vector3 p1Y = pos + new Vector3(MathF.Cos(angle1) * r, 0, MathF.Sin(angle1) * r);
                        Vector3 p2Y = pos + new Vector3(MathF.Cos(angle2) * r, 0, MathF.Sin(angle2) * r);
                        Raylib.DrawLine3D(p1Y, p2Y, Color.Green);
                        
                        // Z axis (Blue) - circle in XY plane
                        Vector3 p1Z = pos + new Vector3(MathF.Cos(angle1) * r, MathF.Sin(angle1) * r, 0);
                        Vector3 p2Z = pos + new Vector3(MathF.Cos(angle2) * r, MathF.Sin(angle2) * r, 0);
                        Raylib.DrawLine3D(p1Z, p2Z, Color.Blue);
                    }
                    
                    // Add small clickable spheres along the principal drag handles
                    Raylib.DrawSphere(pos + new Vector3(r, 0, 0), 0.2f, Color.Red);
                    Raylib.DrawSphere(pos + new Vector3(0, r, 0), 0.2f, Color.Green);
                    Raylib.DrawSphere(pos + new Vector3(0, 0, r), 0.2f, Color.Blue);
                }
                else if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
                {
                    float cubeSize = 0.3f;
                    Raylib.DrawLine3D(pos, pos + new Vector3(length, 0, 0), Color.Red);
                    Raylib.DrawCube(pos + new Vector3(length, 0, 0), cubeSize, cubeSize, cubeSize, Color.Red);

                    Raylib.DrawLine3D(pos, pos + new Vector3(0, length, 0), Color.Green);
                    Raylib.DrawCube(pos + new Vector3(0, length, 0), cubeSize, cubeSize, cubeSize, Color.Green);

                    Raylib.DrawLine3D(pos, pos + new Vector3(0, 0, length), Color.Blue);
                    Raylib.DrawCube(pos + new Vector3(0, 0, length), cubeSize, cubeSize, cubeSize, Color.Blue);
                }
                else
                {
                    Raylib.DrawLine3D(pos, pos + new Vector3(length, 0, 0), Color.Red);
                    Raylib.DrawCylinderEx(pos + new Vector3(length, 0, 0), pos + new Vector3(length + 0.5f, 0, 0), radius, 0.0f, 8, Color.Red);

                    Raylib.DrawLine3D(pos, pos + new Vector3(0, length, 0), Color.Green);
                    Raylib.DrawCylinderEx(pos + new Vector3(0, length, 0), pos + new Vector3(0, length + 0.5f, 0), radius, 0.0f, 8, Color.Green);

                    Raylib.DrawLine3D(pos, pos + new Vector3(0, 0, length), Color.Blue);
                    Raylib.DrawCylinderEx(pos + new Vector3(0, 0, length), pos + new Vector3(0, 0, length + 0.5f), radius, 0.0f, 8, Color.Blue);
                }
            }
        }

        public void DrawUI()
        {
            if (!themeSet) SetupUnityTheme();

            // Check configuration directly out of Program Files path mapping
            string programFilesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"FrostEngine");
            string globalSettingsPath = Path.Combine(programFilesFolder, "settings.json");

            if (!File.Exists(globalSettingsPath))
            {
                DrawImGuiSetupWizard(programFilesFolder, globalSettingsPath);
                return; 
            }

            DrawMainMenuBar();
            DrawToolbar();
            DrawHierarchy();
            DrawInspector();
            DrawProjectBrowser();
            if (showSettingsWindow) DrawSettingsWindow();
            if (showConsoleLog) DrawConsoleLogWindow();
        }

        private void DrawImGuiSetupWizard(string targetFolder, string targetJsonPath)
        {
            ImGui.SetNextWindowPos(new Vector2(Raylib.GetScreenWidth() / 2f - 200, Raylib.GetScreenHeight() / 2f - 140), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(400, 280), ImGuiCond.Always);

            ImGui.Begin("FrostEngine Setup Wizard", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

            ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.8f, 1.0f), "=========================================");
            // FIXED: Replaced bad emoji symbols with pristine custom unified font icon calls!
            ImGui.Text($"       {Icons.Box} WELCOME TO FROSTENGINE {Icons.Box}");
            ImGui.Text("         by QuakeSome2000");
            ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.8f, 1.0f), "=========================================");
            ImGui.Spacing();

            ImGui.Text("User-Pickable Workspace Project Path:");
            ImGui.InputText("##PathInput", ref wizardPathInput, 500);
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Where scenes & assets will be stored.");
            ImGui.Spacing();

            ImGui.Text("Developer Profile Name:");
            ImGui.InputText("##NameInput", ref wizardNameInput, 100);
            ImGui.Spacing();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1.0f), errorMessage);
                ImGui.Spacing();
            }
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Initialize Workspace & Start Engine", new Vector2(-1, 35)))
            {
                if (string.IsNullOrWhiteSpace(wizardPathInput)) wizardPathInput = "C:/FrostEngineProjects";
                if (string.IsNullOrWhiteSpace(wizardNameInput)) wizardNameInput = "Admin";

                try
                {
                    if (!Directory.Exists(wizardPathInput))
                    {
                        Directory.CreateDirectory(wizardPathInput);
                    }

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    var configObject = new { ProjectPath = wizardPathInput, UserName = wizardNameInput };
                    string json = System.Text.Json.JsonSerializer.Serialize(configObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    
                    File.WriteAllText(targetJsonPath, json);

                    engine.ProjectFolder = wizardPathInput;
                    Log("Workspace loaded safely. Global config written to Program Files folder tree.");
                }
                catch (UnauthorizedAccessException)
                {
                    errorMessage = "Error: Please run the engine once as Admin to install.";
                    Log("Permission error writing configuration block to protected System Root.");
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error: {ex.Message}";
                    Log($"Error writing context initialization configuration: {ex.Message}");
                }
            }

            ImGui.End();
        }

        private void DrawMainMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New Scene"))
                    {
                        engine.CurrentScene = new Scene();
                        selectedEntity = null;
                        Log("Created a clean 3D scene blueprint.");
                    }
                    if (ImGui.MenuItem($"{Icons.Settings} Settings & Lighting")) showSettingsWindow = true;
                    if (ImGui.MenuItem("Exit")) Environment.Exit(0);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("GameObject"))
                {
                    if (ImGui.MenuItem("Create Empty")) { var e = new Entity("New Entity"); e.Transform.Position = editorCamera.Target; engine.CurrentScene.AddEntity(e); Log("Spawned Empty Entity."); }
                    if (ImGui.MenuItem("Create Cube")) { var e = new Entity("Cube"); e.Transform.Position = editorCamera.Target; e.AddComponent(new MeshRendererComponent(MeshType.Cube)); engine.CurrentScene.AddEntity(e); Log("Spawned Core Cube Mesh."); }
                    if (ImGui.MenuItem("Create Sphere")) { var e = new Entity("Sphere"); e.Transform.Position = editorCamera.Target; e.AddComponent(new MeshRendererComponent(MeshType.Sphere)); engine.CurrentScene.AddEntity(e); Log("Spawned High-Gloss Sphere Primitive."); }
                    if (ImGui.MenuItem("Create Cylinder")) { var e = new Entity("Cylinder"); e.Transform.Position = editorCamera.Target; e.AddComponent(new MeshRendererComponent(MeshType.Cylinder)); engine.CurrentScene.AddEntity(e); Log("Spawned Cylinder Primitive."); }
                    if (ImGui.MenuItem("Create Cone")) { var e = new Entity("Cone"); e.Transform.Position = editorCamera.Target; e.AddComponent(new MeshRendererComponent(MeshType.Cone)); engine.CurrentScene.AddEntity(e); Log("Spawned Cone Primitive."); }
                    if (ImGui.MenuItem("Create Point Light")) { var e = new Entity("Point Light"); e.Transform.Position = editorCamera.Target; e.AddComponent(new PointLightComponent()); engine.CurrentScene.AddEntity(e); Log("Spawned Point Light Entity."); }
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

        private void DrawToolbar()
        {
            ImGui.SetNextWindowPos(new Vector2(0, 20));
            ImGui.SetNextWindowSize(new Vector2(Raylib.GetScreenWidth(), 40));
            ImGui.Begin("Toolbar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus);

            float buttonWidth = 90;
            ImGui.SetCursorPosX((Raylib.GetScreenWidth() - buttonWidth * 2) * 0.5f);

            if (!IsPlaying)
            {
                if (ImGui.Button($"{Icons.Play} Play", new Vector2(buttonWidth, 25)))
                {
                    IsPlaying = true;
                    Log("Runtime simulator context started.");
                    foreach (var entity in engine.CurrentScene.GetEntities())
                    {
                        var script = entity.GetComponent<ScriptComponent>();
                        script?.StartPlay();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.Build} Build", new Vector2(buttonWidth, 25)))
                {
                    Log("Compiling standalone presentation distribution builds...");
                    GameBuilder.BuildGame(engine.CurrentScene);
                }
            }
            else
            {
                if (ImGui.Button($"{Icons.Stop} Stop", new Vector2(buttonWidth, 25)))
                {
                    IsPlaying = false;
                    Log("Runtime simulation suspended.");
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
            ImGui.SetNextWindowSize(new Vector2(250, Raylib.GetScreenHeight() - 60 - 250));
            ImGui.Begin($"{Icons.Box} Hierarchy", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus);

            foreach (var entity in engine.CurrentScene.GetEntities().ToList())
            {
                bool isSelected = selectedEntity == entity;
                if (ImGui.Selectable(entity.Name, isSelected))
                {
                    selectedEntity = entity;
                }

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.Selectable($"{Icons.Trash} Delete"))
                    {
                        Log($"Removed Entity: {entity.Name}");
                        engine.CurrentScene.RemoveEntity(entity);
                        if (selectedEntity == entity) selectedEntity = null;
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.End();
        }

        private void DrawProjectBrowser()
        {
            ImGui.SetNextWindowPos(new Vector2(0, Raylib.GetScreenHeight() - 250));
            ImGui.SetNextWindowSize(new Vector2(Raylib.GetScreenWidth() - 300, 250));
            ImGui.Begin($"{Icons.Folder} Asset Browser", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus);

            if (ImGui.BeginTabBar("BottomTabs"))
            {
                if (ImGui.BeginTabItem("Folder Project"))
                {
                    ImGui.Spacing();
                    ImGui.InputText("Scene Name", ref saveSceneName, 100);
                    ImGui.SameLine();
                    if (ImGui.Button($"{Icons.Save} Save Scene"))
                    {
                        if (!saveSceneName.EndsWith(".json")) saveSceneName += ".json";

                        string fullSavePath = Path.Combine(engine.ProjectFolder, saveSceneName);
                        SceneSerializer.Save(engine.CurrentScene, fullSavePath);
                        Log($"Saved scene configuration down to your directory: {saveSceneName}");
                    }

                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"Available Scenes in workspace folder:");

                    var files = Directory.GetFiles(engine.ProjectFolder, "*.json");
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName == "settings.json") continue;

                        ImGui.BulletText(fileName);
                        ImGui.SameLine(300);
                        if (ImGui.Button("Load##" + fileName))
                        {
                            engine.CurrentScene = SceneSerializer.Load(file);
                            selectedEntity = null;
                            Log($"Loaded active scene array sequence file maps via: {fileName}");
                        }
                    }

                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Available Prefabs in workspace folder:");

                    var prefabs = Directory.GetFiles(engine.ProjectFolder, "*.prefab");
                    foreach (var file in prefabs)
                    {
                        string fileName = Path.GetFileName(file);
                        ImGui.BulletText(fileName);
                        ImGui.SameLine(300);
                        if (ImGui.Button("Instantiate##" + fileName))
                        {
                            engine.CurrentScene.AddEntity(SceneSerializer.LoadPrefab(file));
                            Log($"Instantiated prefab from: {fileName}");
                        }
                    }

                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }

        private void DrawInspector()
        {
            ImGui.SetNextWindowPos(new Vector2(Raylib.GetScreenWidth() - 300, 60));
            ImGui.SetNextWindowSize(new Vector2(300, Raylib.GetScreenHeight() - 60));
            ImGui.Begin($"{Icons.Search} Inspector", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus);

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
                            string[] types = { "Cube", "Sphere", "Plane", "Cylinder", "Cone" };
                            if (ImGui.Combo("Type", ref currentType, types, types.Length))
                            {
                                meshRenderer.Type = (MeshType)currentType;
                            }

                            Vector4 color = new Vector4(meshRenderer.Tint.R / 255f, meshRenderer.Tint.G / 255f, meshRenderer.Tint.B / 255f, meshRenderer.Tint.A / 255f);
                            if (ImGui.ColorEdit4("Tint", ref color))
                            {
                                meshRenderer.Tint = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));
                            }

                            var files = Directory.GetFiles(engine.ProjectFolder, "*.*")
                                .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                .ToArray();

                            string currentFile = Path.GetFileName(meshRenderer.TexturePath ?? "");
                            if (string.IsNullOrEmpty(currentFile)) currentFile = "None";

                            if (ImGui.BeginCombo("Texture", currentFile))
                            {
                                if (ImGui.Selectable("None", currentFile == "None")) meshRenderer.TexturePath = "";
                                foreach (var f in files)
                                {
                                    string fname = Path.GetFileName(f);
                                    if (ImGui.Selectable(fname, currentFile == fname))
                                    {
                                        meshRenderer.TexturePath = f;
                                    }
                                }
                                ImGui.EndCombo();
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
                    else if (component is PointLightComponent lightComp)
                    {
                        if (ImGui.CollapsingHeader("Point Light", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.ColorEdit3("Color", ref lightComp.Color);
                            ImGui.DragFloat("Intensity", ref lightComp.Intensity, 0.1f, 0.0f, 100.0f);
                            ImGui.DragFloat("Range", ref lightComp.Range, 0.5f, 0.1f, 1000.0f);
                        }
                    }
                }

                ImGui.Separator();

                if (ImGui.Button($"{Icons.Box} Save as Prefab", new Vector2(-1, 30)))
                {
                    string fullPrefabPath = Path.Combine(engine.ProjectFolder, selectedEntity.Name + ".prefab");
                    SceneSerializer.SavePrefab(selectedEntity, fullPrefabPath);
                    Log($"Compiled template map to folder: {selectedEntity.Name}.prefab");
                }
                ImGui.Spacing();

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
                    if (ImGui.MenuItem("Point Light"))
                    {
                        if (selectedEntity.GetComponent<PointLightComponent>() == null)
                            selectedEntity.AddComponent(new PointLightComponent());
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.End();
        }

        private void DrawSettingsWindow()
        {
            ImGui.SetNextWindowPos(new Vector2(Raylib.GetScreenWidth() / 2f - 150, Raylib.GetScreenHeight() / 2f - 250), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(300, 450), ImGuiCond.FirstUseEver);

            ImGui.Begin($"{Icons.Settings} Settings & Lighting", ref showSettingsWindow);

            if (ImGui.CollapsingHeader("Interface Customization", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Checkbox($"{Icons.Terminal} Enable Dev Console Window", ref showConsoleLog);
            }

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

        private void DrawConsoleLogWindow()
        {
            ImGui.SetNextWindowPos(new Vector2(260, Raylib.GetScreenHeight() - 250), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(Raylib.GetScreenWidth() - 560, 200), ImGuiCond.FirstUseEver);

            ImGui.Begin($"{Icons.Terminal} Live Diagnostic Console", ref showConsoleLog);
            if (ImGui.Button("Clear History"))
            {
                logMessages.Clear();
            }
            ImGui.Separator();

            ImGui.BeginChild("LogScrollRegion", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            foreach (var log in logMessages)
            {
                Vector4 color = new Vector4(0.85f, 0.85f, 0.85f, 1.0f);
                if (log.Contains("Error")) color = new Vector4(0.9f, 0.3f, 0.3f, 1.0f);
                else if (log.Contains("Saved") || log.Contains("Blueprint")) color = new Vector4(0.3f, 0.8f, 0.8f, 1.0f);
                else if (log.Contains("context started")) color = new Vector4(0.4f, 0.9f, 0.4f, 1.0f);

                ImGui.TextColored(color, log);
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();
            ImGui.End();
        }
    }
}