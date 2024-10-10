using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MapStudio.UI;
using Toolbox.Core;
using Toolbox.Core.ViewModels;
using OpenTK;
using GLFrameworkEngine;
using UIFramework;
using PartyStudioPlugin;
using ImGuiNET;

namespace PartyStudio.GCN
{
    public class GCNMapEditorPlugin : FileEditor, IFileFormat, IDisposable
    {
        public string[] Description => new string[] { "bin" };
        public string[] Extension => new string[] { "*.bin" };

        /// <summary>
        /// Whether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; }

        MPGCN BoardLoader;

        private BoardPathRenderer PathRender;
        private SpaceWindow SpaceWindow;
        private ViewportTopdown ViewportTopdown;

        FileEditorMode EditorMode = FileEditorMode.MapEditor;

        enum FileEditorMode
        {
            MapEditor,
            ModelEditor,
        }

        public bool Identify(File_Info fileInfo, Stream stream) {
            return false;

            return fileInfo.Extension == ".bin";
        }

        public void Load(Stream stream)
        {
            BoardLoader = new MPGCN();
            BoardLoader.LoadFile(this, stream, FileInfo.FilePath);

            SpaceTableWindow window = new SpaceTableWindow(BoardLoader);
            Workspace.Windows.Add(window);

            ViewportTopdown = new ViewportTopdown(this, Workspace);

            LoadFile(BoardLoader);
        }

        public void Save(Stream stream) {
            SaveFile();
            BoardLoader.SaveFile(this, stream);
        }

        public void Dispose() {
            this.PathRender?.Dispose();
        }

        public void LoadFile(BoardLoader boardLoader)
        {
            PathRender = new BoardPathRenderer(boardLoader);

            Runtime.DisplayBones = false;

            AddRender(PathRender);
            Root.AddChild(PathRender.UINode);

           // var window = new ToolWindow();
           // window.Editor = this;
            //this.ToolWindowDrawer = window;

            SpaceWindow = new SpaceWindow(Workspace, boardLoader);
            Workspace.ViewportWindow.DrawViewportMenuBar += delegate
            {
                DrawEditMenuBar();
            };

            ReloadCollision();

            ProcessLoading.Instance.Update(100, 100, "Finished!");

            /*   var camEditor = new CameraEditor();
               camEditor.Anims = ((MPSA)boardLoader).OpeningCameras;
               camEditor.Opened = true;
               Workspace.ActiveWorkspace.Windows.Add(camEditor);*/

            Workspace.WorkspaceTools.Add(new MenuItemModel(
            $"   {'\uf279'}    Map Editor", () =>
            {
              EditorMode = FileEditorMode.MapEditor;
              ReloadEditorMode();
            }));
            Workspace.WorkspaceTools.Add(new MenuItemModel(
               $"   {'\uf6d1'}    Model Editor", () =>
               {
                   EditorMode = FileEditorMode.ModelEditor;
                   ReloadEditorMode();
               }));
        }

        private void ReloadEditorMode()
        {
            Root.Children.Clear();
            if (EditorMode == FileEditorMode.MapEditor)
            {
                PathRender.IsVisible = true;
                Root.AddChild(PathRender.UINode);
            }
            if (EditorMode == FileEditorMode.ModelEditor)
            {
                PathRender.IsVisible = false;
                foreach (var node in BoardLoader.ModelEditor.MapModels)
                    Root.AddChild(node);
            }
        }

        public virtual void DrawViewportToolbar()
        {
            void DrawEditorSelection(string space, string tool_tip = "")
            {
                if (!IconManager.HasIcon(space))
                    return;

                bool selected = space == PartyStudioPlugin.BoardLoader.ActiveSpaceType;

                var btn_color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
                if (selected)
                    btn_color = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];

                btn_color.W = 0.8f; //adjust transparency


                //ImGui.PushFont(ImGuiController.FontIconBig);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0.1f));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(btn_color));

                var pos = ImGui.GetCursorPos();
                if (ImGui.Button($"##{space}", new System.Numerics.Vector2(28, 28)))
                    PartyStudioPlugin.BoardLoader.ActiveSpaceType = space;

                ImGui.SetCursorPos(pos);
                IconManager.DrawIcon(space, 28);

                ImGui.PopStyleColor(1);
                ImGui.PopStyleVar(2);

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{space}: {tool_tip}");
            }

            foreach (var space in BoardLoader.SpaceTypeList)
                DrawEditorSelection(space);
        }

        public override void DrawToolWindow()
        {
            if (ImGui.SliderFloat("Bightness", ref GCNRenderLibrary.Rendering.RenderGlobals.Bightness, 0, 1))
                GLContext.ActiveContext.UpdateViewport = true;

            if (ImGui.Checkbox("X Ray Spaces", ref PathRender.XRayMode))
                GLContext.ActiveContext.UpdateViewport = true;
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            SpaceWindow.ParentDock = Workspace.ViewportWindow;

            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ToolWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(SpaceWindow);
            windows.Add(ViewportTopdown);
            return windows;
        }

        public void SaveFile()
        {
            PathRender.BoardLoader.Spaces.Clear();
            foreach (BoardPathPoint point in PathRender.PathPoints)
            {
                //Apply SRT info for saving
                point.SpaceData.Position = point.Transform.Position / GLContext.PreviewScale;
                point.SpaceData.Rotation = point.Transform.RotationEuler;
                point.SpaceData.Scale = point.Transform.Scale;
                PathRender.BoardLoader.Spaces.Add(point.SpaceData);
            }
        }

        /// <summary>
        /// Reloads the collision plane used to drop objects onto the floor.
        /// </summary>
        public void ReloadCollision(float height = 0)
        {
            float size = 2000;

            //Make a big flat plane for placing spaces on.
            GLContext.ActiveContext.CollisionCaster.Clear();
            GLContext.ActiveContext.CollisionCaster.AddTri(
                new Vector3(-size, height, size),
                new Vector3(0, height, -(size * 2)),
                new Vector3(size * 2, height, 0));
            GLContext.ActiveContext.CollisionCaster.AddTri(
                new Vector3(-size, height, -size),
                new Vector3(size * 2, height, 0),
                new Vector3(size * 2, height, size * 2));
            GLContext.ActiveContext.CollisionCaster.UpdateCache();
        }

        public override void OnKeyDown(KeyEventInfo keyEventInfo, bool isRepeat)
        {
            GLContext context = GetActiveContext();

            ViewportTopdown.OnKeyDown(keyEventInfo, isRepeat);
        }

        public override void OnMouseDown(MouseEventInfo mouseInfo)
        {
        }

        public void DrawEditMenuBar()
        {
            var context = GetActiveContext();
            bool changed = Workspace.ActiveWorkspace.ViewportWindow.DrawPathDropdown();
            if (changed)
            {
                PathRender.DeselectAll();
                PathRender.IsSelected = false;
            }

            bool refreshScene = false;

            var h = ImGuiNET.ImGui.GetWindowHeight();
            var size = new System.Numerics.Vector2(h, h);

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.ADD_ICON}   ", "ADD", InputSettings.INPUT.Scene.Create))
                PathRender.AddSinglePoint(context);

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.DELETE_ICON}   ", "REMOVE", InputSettings.INPUT.Scene.Delete))
                PathRender.RemoveSelected();

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.DUPE_ICON}   ", "DUPLICATE", InputSettings.INPUT.Scene.Dupe))
                PathRender.DuplicateSelected(context);

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.LINK_ICON}   ", "CONNECT", InputSettings.INPUT.Scene.Fill))
                PathRender.FillSelectedPoints(context);

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.UNLINK_ICON}   ", "UNCONNECT"))
                PathRender.UnlinkSelectedPoints(context);

            if (refreshScene)
                GLContext.ActiveContext.UpdateViewport = true;
        }

        public GLContext GetActiveContext()
        {
            if (ViewportTopdown.IsFocused)
                return ViewportTopdown.Context;

            return GLContext.ActiveContext;
        }
    }
}
