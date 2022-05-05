using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using MapStudio.UI;
using ImGuiNET;
using GLFrameworkEngine;
using CafeLibrary.Rendering;

namespace PartyStudio
{
    public class ToolWindow : IToolWindowDrawer
    {
        public static float CollisionHeight = 0;
        public static bool ShowModels = true;

        public MapEditorPlugin Editor;

        public void Render()
        {
            var settings = GLContext.ActiveContext.TransformTools.TransformSettings;
            var width = ImGui.GetWindowWidth();
            bool updateViewport = false;


            if (ImGui.CollapsingHeader("Visuals", ImGuiTreeNodeFlags.DefaultOpen))
            {
                updateViewport |= ImGui.Checkbox("Show Map Model", ref ShowModels);
                updateViewport |= ImGui.DragFloat("Brightness", ref BfresMaterialRender.Brightness, 0.05f, 0, 1.0f);
            }

            if (ImGui.CollapsingHeader("Space Edit", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Align X"))
                {
                    Editor.BoardLoader.PathRender.AlignAxis(0);
                    updateViewport = true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Align Y"))
                {
                    Editor.BoardLoader.PathRender.AlignAxis(1);
                    updateViewport = true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Align Z"))
                {
                    Editor.BoardLoader.PathRender.AlignAxis(2);
                    updateViewport = true;
                }

                ImGuiHelper.InputFromBoolean("Snap Transform", settings, "SnapTransform");
                if (settings.SnapTransform)
                {
                    var snapFactor = settings.TranslateSnapFactor;
                    var vec = new Vector3(snapFactor.X, snapFactor.Y, snapFactor.Z);
                    if (ImGui.InputFloat3("Snap Movement", ref vec))
                        settings.TranslateSnapFactor = new OpenTK.Vector3(vec.X, vec.Y, vec.Z);
                }
                ImGui.Checkbox("Drop To Collision", ref GLContext.ActiveContext.EnableDropToCollision);
                if (GLContext.ActiveContext.EnableDropToCollision)
                {
                    if (ImGui.DragFloat("Collision Drop Height", ref CollisionHeight, 0.1F))
                    {
                        Editor.ReloadCollision(CollisionHeight);
                        updateViewport = true;
                    }
                }
            }

            if (updateViewport)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
