using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;
using OpenTK.Graphics.OpenGL;
using PartyStudio.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIFramework;
using static Toolbox.Core.Runtime;

namespace PartyStudioPlugin
{
    public class ViewportTopdown : DockWindow
    {
        public override string Name => "Top Down";

        public GLContext Context;
        Framebuffer Framebuffer;

        DrawableInfiniteFloor _floor;
        ObjectLinkDrawer _objectLinkDrawer;
        OrientationGizmo _orientationGizmo;
        DrawableBackground _background;

        FileEditor MapEditor;

        public ViewportTopdown(FileEditor mapEditor, Workspace workspace) : base(workspace)
        {
            MapEditor = mapEditor;
            this.DockDirection = ImGuiNET.ImGuiDir.None;
            Context = new GLContext();
            Context.Camera = new GLFrameworkEngine.Camera();
            SetupCamera();

            GLContext.ActiveContext.TransformTools.TransformChanged += delegate
            {
                Context.TransformTools.UpdateOrigin();
                Context.UpdateViewport = true;
            };

            Context.TransformTools.TransformChanged += delegate
            {
                GLContext.ActiveContext.TransformTools.UpdateOrigin();
                GLContext.ActiveContext.UpdateViewport = true;
            };


            GLContext.ActiveContext.TransformTools.TransformListChanged += delegate
            {
                Context.TransformTools.ActiveTransforms = GLContext.ActiveContext.TransformTools.ActiveTransforms;
                Context.TransformTools.UpdateOrigin();
                Context.UpdateViewport = true;
            };

            Context.TransformTools.TransformListChanged += delegate
            {
                GLContext.ActiveContext.TransformTools.ActiveTransforms = Context.TransformTools.ActiveTransforms;
                GLContext.ActiveContext.TransformTools.UpdateOrigin();
                GLContext.ActiveContext.UpdateViewport = true;
            };

            Context.Scene = GLContext.ActiveContext.Scene;
            Context.SelectionTools.IsSelectionMode = true;
            Context.TransformTools.TransformSettings.UseY = false;
            Context.TransformTools.TransformSettings.TransformMode = TransformSettings.TransformSpace.World;
            Context.TransformTools.TransformSettings.MinGizmoSize = 100;

            Framebuffer = new Framebuffer(FramebufferTarget.Framebuffer, 4, 4);

            _background = new DrawableBackground();
            _floor = new DrawableInfiniteFloor();
            _objectLinkDrawer = new ObjectLinkDrawer();
            _orientationGizmo = new OrientationGizmo();
        }

        public override void Render()
        {
            DrawMenubar();

            ImGui.BeginChild("viewportTopdown");

            //Store the focus state for handling key events
            IsFocused = ImGui.IsWindowFocused();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered() && !IsFocused)
            {
                IsFocused = true;
                ImGui.FocusWindow(ImGui.GetCurrentWindow());
            }
            var size = ImGui.GetWindowSize();
            DrawViewport((int)size.X, (int)size.Y);

            var top_left_pos = ImGui.GetCursorScreenPos();

            var id = ((GLTexture)Framebuffer.Attachments[0]).ID;
            ImGui.Image((IntPtr)id, size, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
            ImGui.SetItemAllowOverlap();

            ImGui.SetCursorScreenPos(top_left_pos);

            if (MapEditor is GCNMapEditorPlugin)
                ((GCNMapEditorPlugin)MapEditor).DrawViewportToolbar();

            ImGui.EndChild();
        }

        private void DrawMenubar()
        {
            ImGui.BeginChild("viewport2dMenu", new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23),
                false, ImGuiWindowFlags.MenuBar);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.Button($"   {IconManager.CAMERA_ICON}   "))
                {
                    SetupCamera();
                }
                ImGui.SameLine();
                if (ImGui.Button($"   {IconManager.UNDO_ICON}    "))
                {
                    GLContext.ActiveContext.Scene.Undo();
                }
                ImGui.SameLine();
                if (ImGui.Button($"   {IconManager.REDO_ICON}    "))
                {
                    GLContext.ActiveContext.Scene.Redo();
                }
                ImGui.SameLine();

                ImGui.EndMenuBar();
            }


            ImGui.EndChild();
        }

        private void SetupCamera()
        {
            Context.Camera.ResetTransform();
            Context.Camera.ZNear = -1000000;
            Context.Camera.ZFar = 1000000;
            Context.Camera.TargetDistance = 5000;
            Context.Camera.IsOrthographic = true;
            Context.Camera.LockRotation = true;
            Context.Camera.Direction = GLFrameworkEngine.Camera.FaceDirection.Top;
            Context.Camera.UpdateMatrices();
        }

        private void DrawViewport(int width, int height)
        {
            Context.Scene = GLContext.ActiveContext.Scene;
            ResourceTracker.ResetStats();

            if (Framebuffer.Width != width || Framebuffer.Height != height)
                OnResize(width, height);

            UpdateCamera(Context);

            Framebuffer.Bind();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Viewport(0, 0, width, height);

            //Background
            _background.Draw(Context, Pass.OPAQUE);

            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            var scene = Workspace.ActiveWorkspace.ViewportWindow.Pipeline;
            foreach (var drawable in scene._context.Scene.Objects)
            {
                if (!drawable.IsVisible)
                    continue;

                if (!(drawable is RenderablePath))
                    drawable.DrawModel(Context, Pass.OPAQUE);
            }
            foreach (var drawable in scene._context.Scene.Objects)
            {
                if (!drawable.IsVisible)
                    continue;

                if (!(drawable is RenderablePath))
                    drawable.DrawModel(Context, Pass.TRANSPARENT);
            }

            Draw2D();
            DrawExtra();

            Framebuffer.Unbind();
        }

        private void Draw2D()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            foreach (var drawable in Context.Scene.Objects)
            {
                if (!drawable.IsVisible)
                    continue;

                if (drawable is RenderablePath)
                    drawable.DrawModel(Context, Pass.OPAQUE);
            }

            foreach (var drawable in Context.Scene.Objects)
            {
                if (drawable is RenderablePath)
                    drawable.DrawModel(Context, Pass.TRANSPARENT);
            }

            GL.Enable(EnableCap.DepthTest);
        }

        private void DrawExtra()
        {
            Context.CurrentShader = null;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.Blend);

            //Draw ui
            Context.UIDrawer.Render(Context);

            Context.Scene.SpawnMarker?.DrawModel(Context, Pass.OPAQUE);

           // _floor.Draw(Context);
            _orientationGizmo.Draw(Context);
            Context.TransformTools.Draw(Context);

            _objectLinkDrawer.Draw(Context);

            Context.SelectionTools.Render(Context,
                Context.CurrentMousePoint.X,
               Context.CurrentMousePoint.Y);

            Context.LinkingTools.Render(Context,
                Context.CurrentMousePoint.X,
               Context.CurrentMousePoint.Y);

            Context.BoxCreationTool.Render(Context);

            GL.Enable(EnableCap.DepthTest);
        }

        private bool _mouseDown;

        private void UpdateCamera(GLContext context)
        {
            var mouseInfo = InputState.CreateMouseState();
            var keyInfo = InputState.CreateKeyState();
            KeyEventInfo.State = keyInfo;

            if (ImGui.IsAnyMouseDown() && !_mouseDown && IsFocused && ImGui.IsWindowHovered())
            {
                context.OnMouseDown(mouseInfo, keyInfo);
                _mouseDown = true;

                Workspace.ActiveWorkspace?.OnMouseDown(mouseInfo);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                context.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (IsFocused)
                context.OnMouseMove(mouseInfo, keyInfo, _mouseDown);

            if (ImGuiController.ApplicationHasFocus && ImGui.IsWindowHovered())
                context.OnMouseWheel(mouseInfo, keyInfo);
            else
                context.ResetPrevious();

            if (this.IsFocused)
                context.Camera.Controller.KeyPress(keyInfo);
        }

        private void OnResize(int width, int height)
        {
            Context.Width = width; Context.Height = height;
            Context.Camera.Width = width;
            Context.Camera.Height = height;
            Context.Camera.UpdateMatrices();

            Framebuffer.Resize(width, height);
        }

        public void OnKeyDown(KeyEventInfo keyInfo, bool isRepeat)
        {
            if (IsFocused)
                Context.OnKeyDown(keyInfo, isRepeat, true);
        }
    }
}
