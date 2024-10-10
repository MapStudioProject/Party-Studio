using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using MapStudio.UI;
using static GCNRenderLibrary.Rendering.GX;

namespace PartyStudio.GCN
{
    internal class ObjectNodeUI
    {
        HSF.ObjectNodeWrapper Wrapper;
        HSFObjectData ObjectData;
        HSFLight LightData;
        HSFCamera CameraData;

        HSFObject Node;

        TabControl TabControl = new TabControl("objectnode_menu1");
        MaterialUI MaterialUI = new MaterialUI();

        public ObjectNodeUI()
        {
            TabControl.TabMode = TabControl.Mode.Horizontal_Tabs;
            TabControl.Pages.Clear();
            TabControl.Pages.Add(new TabPage($"   {'\uf0ad'}    Info", DrawNodeUI));
            TabControl.Pages.Add(new TabPage($"   {'\uf302'}    Materials", DrawMaterialData));
        }

        public void Init(HSF.ObjectNodeWrapper wrapper, HSFObject node)
        {
            TabControl.Pages.Clear();
            TabControl.Pages.Add(new TabPage($"   {'\uf0ad'}    Info", DrawNodeUI));
            if (node.Data.Type == ObjectType.Mesh)
                TabControl.Pages.Add(new TabPage($"   {'\uf302'}    Materials", DrawMaterialData));

            Wrapper = wrapper;
            ObjectData = node.Data;
            Node = node;
            LightData = node.LightData;
            CameraData = node.CameraData;

            if (node.MeshData != null)
            {
                var poly = node.MeshData.GXMeshes.Values.FirstOrDefault();
                //MaterialUI.Init(wrapper.MaterialWrapper, (Material)poly.SceneNode.Material);
            }
        }

        public void Render()
        {
            TabControl.Render();
        }

        private void DrawMaterialData()
        {
          //  MaterialUI.Render();
        }

        private void DrawNodeUI()
        {
            string name = Wrapper.Header;

            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("Info_properties");


                if (ImguiPropertyColumn.Text("Name", ref name))
                    Wrapper.Header = name;

                ImguiPropertyColumn.Combo("Type", ref ObjectData.Type);

                ImguiPropertyColumn.InputInt("Render Flags", ref ObjectData.RenderFlags);

                ImguiPropertyColumn.End();
            }

            switch (this.Node.Data.Type)
            {
                case ObjectType.Light:
                    DrawLightUI();
                    break;
                case ObjectType.Camera:
                    DrawCameraUI();
                    break;
                default:
                    DrawObjectData();
                    break;
            }
        }

        private void DrawLightUI()
        {
            if (ImGui.CollapsingHeader("Light Data", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("light_properties");

                var color = new Vector4(
                    LightData.R / 255f,
                    LightData.G / 255f,
                    LightData.B / 255f, 1f);

                ImguiPropertyColumn.Combo("Type", ref LightData.Type, ImGuiComboFlags.None);

                var pos = new Vector3(LightData.Position.X, LightData.Position.Y, LightData.Position.Z);
                if (ImguiPropertyColumn.DragFloat3("Position", ref pos))
                {
                    LightData.Position = new Vector3XYZ(pos.X, pos.Y, pos.Z);
                    Wrapper.UpdateRender();
                }

                var target = new Vector3(LightData.Target.X, LightData.Target.Y, LightData.Target.Z);
                if (ImguiPropertyColumn.DragFloat3("Target", ref target))
                {
                    LightData.Target = new Vector3XYZ(target.X, target.Y, target.Z);
                    Wrapper.UpdateRender();
                }

                if (LightData.Type == LightType.Point)
                {
                    ImguiPropertyColumn.DragFloat("Radius", ref ObjectData.BaseTransform.Scale.X);
                    ImguiPropertyColumn.DragFloat("Rotate", ref ObjectData.BaseTransform.Rotate.Z);
                }

                if (ImguiPropertyColumn.ColorEdit4("Color", ref color, ImGuiColorEditFlags.NoInputs))
                {
                    LightData.R = (byte)(color.X * 255);
                    LightData.G = (byte)(color.Y * 255);
                    LightData.B = (byte)(color.Z * 255);
                    Wrapper.UpdateRender();
                }

                ImguiPropertyColumn.DragFloat("Cutoff", ref LightData.cutoff);
                ImguiPropertyColumn.DragFloat("Intensity", ref LightData.ref_brightness);
                ImguiPropertyColumn.DragFloat("Distance", ref LightData.ref_distance);
                ImguiPropertyColumn.DragFloat("Unknown", ref LightData.unk2C);

                ImguiPropertyColumn.End();
            }
        }

        private void DrawCameraUI()
        {
            if (ImGui.CollapsingHeader("Camera Data", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("camera_properties");

                var pos = new Vector3(CameraData.Position.X, CameraData.Position.Y, CameraData.Position.Z);
                if (ImguiPropertyColumn.DragFloat3("Position", ref pos))
                {
                    CameraData.Position = new Vector3XYZ(pos.X, pos.Y, pos.Z);
                    Wrapper.UpdateRender();
                }

                var target = new Vector3(CameraData.Target.X, CameraData.Target.Y, CameraData.Target.Z);
                if (ImguiPropertyColumn.DragFloat3("Target", ref target))
                {
                    CameraData.Target = new Vector3XYZ(target.X, target.Y, target.Z);
                    Wrapper.UpdateRender();
                }

                ImguiPropertyColumn.DragFloat("Near", ref CameraData.Near);
                ImguiPropertyColumn.DragFloat("Far", ref CameraData.Far);
                ImguiPropertyColumn.DragFloat("Fov", ref CameraData.Fov);
                ImguiPropertyColumn.DragFloat("Aspect Ratio (Unused)", ref CameraData.AspectRatio);

                ImguiPropertyColumn.End();
            }
        }

        private void DrawObjectData()
        {
            var pos = new Vector3(ObjectData.BaseTransform.Translate.X,
                                 ObjectData.BaseTransform.Translate.Y,
                                 ObjectData.BaseTransform.Translate.Z);
            var sca = new Vector3(ObjectData.BaseTransform.Scale.X,
                                 ObjectData.BaseTransform.Scale.Y,
                                 ObjectData.BaseTransform.Scale.Z);
            var rot = new Vector3(ObjectData.BaseTransform.Rotate.X,
                                 ObjectData.BaseTransform.Rotate.Y,
                                 ObjectData.BaseTransform.Rotate.Z);

            var cull_min = new Vector3(ObjectData.CullBoxMin.X,
                                      ObjectData.CullBoxMin.Y,
                                      ObjectData.CullBoxMin.Z);
            var cull_max = new Vector3(ObjectData.CullBoxMax.X,
                                      ObjectData.CullBoxMax.Y,
                                      ObjectData.CullBoxMax.Z);

            bool updateTransform = false;

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("obj_prop");

                updateTransform |= ImguiPropertyColumn.DragFloat3("Scale", ref sca);
                updateTransform |= ImguiPropertyColumn.DragFloat3("Rotate", ref rot);
                updateTransform |= ImguiPropertyColumn.DragFloat3("Translate", ref pos);

                ImguiPropertyColumn.End();
            }
            if (ImGui.CollapsingHeader("Bounding", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("obj_prop");

                updateTransform |= ImguiPropertyColumn.DragFloat3("Min", ref cull_min);
                updateTransform |= ImguiPropertyColumn.DragFloat3("Max", ref cull_max);

                ImguiPropertyColumn.End();
            }

            if (ImGui.CollapsingHeader("Mesh Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("obj_prop");

                void DrawText(string label, string text)
                {
                    ImGui.Text(label); ImGui.NextColumn();
                    ImGui.Text(text); ImGui.NextColumn();
                }

                DrawText("AttributeIndex", $"{ObjectData.AttributeIndex}");
                DrawText("VertexIndex", $"{ObjectData.VertexIndex}");
                DrawText("NormalIndex", $"{ObjectData.NormalIndex}");
                DrawText("TexCoordIndex", $"{ObjectData.TexCoordIndex}");
                DrawText("ColorIndex", $"{ObjectData.ColorIndex}");
                DrawText("CenvIndex", $"{ObjectData.CenvIndex}");
                DrawText("CenvCount", $"{ObjectData.CenvCount}");
                DrawText("ClusterPositionsOffset", $"{ObjectData.ClusterPositionsOffset}");
                DrawText("ClusterNormalsOffset", $"{ObjectData.ClusterNormalsOffset}");
                DrawText("SymbolIndex", $"{ObjectData.SymbolIndex}");
                DrawText("ChildrenCount", $"{ObjectData.ChildrenCount}");
                DrawText("ParentIndex", $"{ObjectData.ParentIndex}");
                DrawText("Index", $"{Node.Index}");

                ImguiPropertyColumn.End();
            }

            if (Node.MeshData != null)
            {
                foreach (var env in Node.MeshData.Envelopes)
                {
                    if (ImGui.CollapsingHeader("Envelope", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImguiPropertyColumn.Begin("obj_prop");

                        void DrawText(string label, string text)
                        {
                            ImGui.Text(label); ImGui.NextColumn();
                            ImGui.Text(text); ImGui.NextColumn();
                        }

                        DrawText("SingleBinds", $"{env.SingleBinds.Count}");
                        DrawText("DoubleBinds", $"{env.DoubleBinds.Count}");
                        DrawText("MultiBinds", $"{env.MultiBinds.Count}");

                        DrawText("VertexCount", $"{env.VertexCount}");
                        DrawText("CopyCount", $"{env.CopyCount}");

                        ImguiPropertyColumn.End();
                    }
                }
            }

            if (updateTransform)
            {
                ObjectData.BaseTransform.Rotate = new Vector3XYZ(rot.X, rot.Y, rot.Z);
                ObjectData.BaseTransform.Scale = new Vector3XYZ(sca.X, sca.Y, sca.Z);
                ObjectData.BaseTransform.Translate = new Vector3XYZ(pos.X, pos.Y, pos.Z);

                ObjectData.CullBoxMax = new Vector3XYZ(cull_max.X, cull_max.Y, cull_max.Z);
                ObjectData.CullBoxMin = new Vector3XYZ(cull_min.X, cull_min.Y, cull_min.Z);

                Wrapper.UpdateRender();
            }
        }
    }
}
