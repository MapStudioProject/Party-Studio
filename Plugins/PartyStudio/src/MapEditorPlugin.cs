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

namespace PartyStudio
{
    public class MapEditorPlugin : FileEditor, IFileFormat, IDisposable
    {
        public string[] Description => new string[] { "BEA" };
        public string[] Extension => new string[] { "*.bea"};

        /// <summary>
        /// Whether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; }

        public BoardLoader BoardLoader;

        private BoardPathRenderer PathRender;
        private SpaceWindow SpaceWindow;

        FileEditorMode EditorMode = FileEditorMode.MapEditor;

        enum FileEditorMode
        {
            MapEditor,
            ModelEditor,
        }

        public bool Identify(File_Info fileInfo, Stream stream) {
            return fileInfo.Extension == ".bea";
        }

        public void Load(Stream stream)
        {
            BoardLoader = new MPSA();
            BoardLoader.LoadFile(this, stream, FileInfo.FilePath);

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
            AddRender(PathRender);
            Root.AddChild(PathRender.UINode);

            var window = new ToolWindow();
            window.Editor = this;
            this.ToolWindowDrawer = window;

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
        /*    Workspace.WorkspaceTools.Add(new MenuItemModel(
               $"   {'\uf6d1'}    Model Editor", () =>
               {
                   EditorMode = FileEditorMode.ModelEditor;
                   ReloadEditorMode();
               }));*/
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
            }
        }

        public override void DrawToolWindow()
        {
            ToolWindowDrawer.Render();
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

        public override void OnMouseDown(MouseEventInfo mouseInfo)
        {
            if (KeyEventInfo.State.KeyAlt)
                PathRender.AddSinglePoint();
        }

        public void DrawEditMenuBar()
        {
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
                PathRender.AddSinglePoint();

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.DELETE_ICON}   ", "REMOVE", InputSettings.INPUT.Scene.Delete))
                PathRender.RemoveSelected();

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.DUPE_ICON}   ", "DUPLICATE", InputSettings.INPUT.Scene.Dupe))
                PathRender.DuplicateSelected();

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.LINK_ICON}   ", "CONNECT", InputSettings.INPUT.Scene.Fill))
                PathRender.FillSelectedPoints();

            if (ImguiCustomWidgets.MenuItemTooltip($"   {IconManager.UNLINK_ICON}   ", "UNCONNECT"))
                PathRender.UnlinkSelectedPoints();

            if (refreshScene)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
