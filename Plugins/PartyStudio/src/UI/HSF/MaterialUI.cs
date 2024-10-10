using ImGuiNET;
using MapStudio.UI;
using MPLibrary.GCN;
using System;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartyStudio.GCN
{
    class MaterialUI
    {
        TabControl TabControl = new TabControl("material_menu1");

        MaterialTextureUI MaterialTextureUI = new MaterialTextureUI();

        Material Material;
        HSF.MaterialWrapper MaterialWrapper;

        static bool fragment = true;

        public void Init(HSF.MaterialWrapper materialWrapper, Material mat)
        {
            MaterialWrapper = materialWrapper;
            Material = mat;
            MaterialTextureUI.Init(materialWrapper, mat);

            PrepareTabs();
            /*
                        if (TabControl.SelectedTab == 1)
                            MaterialWrapper.DisplayTextureAnimationUI?.Invoke(0);
                        else*/
            MaterialWrapper.DisplayMaterialUI?.Invoke();
        }

        private void PrepareTabs()
        {
            TabControl.TabMode = TabControl.Mode.Horizontal_Tabs;
            TabControl.Pages.Clear();
            TabControl.Pages.Add(new TabPage($"   {'\uf0ad'}    Info", DrawMaterialInfo));
            TabControl.Pages.Add(new TabPage($"   {'\uf302'}    Textures", DrawTextureMaps));
            TabControl.Pages.Add(new TabPage($"   {'\uf61f'}    Shaders", DrawShaderTab));

         /*   TabControl.SelectedTabChanged = null;
            TabControl.SelectedTabChanged += delegate
            {
                if (TabControl.SelectedTab == 1)
                    MaterialWrapper.DisplayTextureAnimationUI?.Invoke(0);
                else
                    MaterialWrapper.DisplayMaterialUI?.Invoke();

            };*/
        }

        public void Render()
        {
            TabControl.Render();
        }

        private void DrawMaterialInfo()
        {
            var mat = Material.MaterialData;
            var hasLightmap = MaterialWrapper.HasLightmap;
            var altFlags = (int)mat.AltFlags;
            var vertexMode = (int)mat.VertexMode;
            var ambientColor = new Vector3(mat.MaterialColor.R, mat.MaterialColor.G, mat.MaterialColor.B) / 255.0f;
            var litAmbientColor = new Vector3(mat.AmbientColor.R, mat.AmbientColor.G, mat.AmbientColor.B) / 255.0f;
            var shadowColor = new Vector3(mat.ShadowColor.R, mat.ShadowColor.G, mat.ShadowColor.B) / 255.0f;
            var transparency = 1.0f - mat.TransparencyInverted;
            var renderFlags = MaterialWrapper.ObjectData.RenderFlags;
            bool depthWrite = !Material.NoDepthWrite;
            bool hasAlphaTest = Material.HasAlphaTest;
            var blendMode = Material.BlendMode;
            bool cullNode = Material.ShowBothFaces;
            bool hasBillboard = Material.HasBillboard;

            bool reloadColors = false;

            if (ImGui.CollapsingHeader("Material Flags", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (UIHelper.DrawEnum("Lighting Channel", ref mat.VertexMode))
                    Material.ReloadLightingChannels(true);

                ImGuiHelper.Tooltip("NoLighting: ");

                if (UIHelper.DrawEnum("Blend Mode", ref blendMode))
                {
                    Material.BlendMode = blendMode;
                    Material.ReloadPolygonState();
                    MaterialWrapper.ReloadDrawList();
                }

                if (ImGui.Checkbox("Use Lightmap", ref hasLightmap))
                {
                    MaterialWrapper.HasLightmap = hasLightmap;
                    //Force specular lighting during a toggle to ensure it can be used.
                    if (hasLightmap && 
                       (mat.VertexMode != LightingChannelFlags.LightingSpecular ||
                        mat.VertexMode != LightingChannelFlags.LightingSpecular2))
                    {
                        mat.VertexMode = LightingChannelFlags.LightingSpecular;
                        Material.ReloadLightingChannels(true);
                    }

                    Material.ReloadTextures();
                    Material.ReloadTevStages(true);
                }
                if (ImGui.Checkbox("Depth Write", ref depthWrite))
                {
                    Material.NoDepthWrite = !depthWrite;
                    Material.ReloadPolygonState();
                    MaterialWrapper.ReloadDrawList();
                }
                if (ImGui.Checkbox("Use Billboard", ref hasBillboard))
                {
                    Material.HasBillboard = hasBillboard;
                    GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
                }
                if (ImGui.Checkbox("Show Both Faces", ref cullNode))
                {
                    Material.ShowBothFaces = cullNode;
                    Material.ReloadPolygonState();
                }
                if (ImGui.Checkbox("Alpha Test", ref hasAlphaTest))
                {
                    Material.HasAlphaTest = hasAlphaTest;
                    Material.ReloadPolygonState();
                    Material.RenderScene?.ReloadShader();
                    MaterialWrapper.ReloadDrawList();

                }
            }
            if (ImGui.CollapsingHeader("Material Colors", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.ColorEdit3("Ambient", ref litAmbientColor, ImGuiColorEditFlags.NoInputs))
                    SetColor(litAmbientColor, ref mat.AmbientColor);

                if (ImGui.ColorEdit3("Material", ref ambientColor, ImGuiColorEditFlags.NoInputs))
                    SetColor(ambientColor, ref mat.MaterialColor);

                if (ImGui.ColorEdit3("Shadow", ref shadowColor, ImGuiColorEditFlags.NoInputs))
                    SetColor(shadowColor, ref mat.ShadowColor);
            }
            if (ImGui.CollapsingHeader("Blending", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.SliderFloat("Transparency", ref transparency, 0, 1))
                {
                    mat.TransparencyInverted = 1 - transparency;
                    Material.ReloadColors();
                    Material.ReloadTevStages(true);

                    MaterialWrapper.ReloadDrawList();
                }
            }

            if (ImGui.CollapsingHeader("Lighting", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.SliderFloat("Lightmap Scale", ref mat.HiliteScale, 0, 50))
                    Material.UpdateLightmapMatrix();

                if (ImGui.SliderFloat("Reflection Intensity", ref mat.ReflectionIntensity, 0, 1))
                {
                    Material.ReloadColors();
                    Material.ReloadTevStages(true);
                }

                ImGui.DragFloat("Unknown 2", ref mat.Unknown2);
                ImGui.DragFloat("Unknown 3", ref mat.Unknown3);
                ImGui.DragFloat("Unknown 4", ref mat.Unknown4);
            }
            if (ImGui.CollapsingHeader("Raw Flags", ImGuiTreeNodeFlags.None))
            {
                ImGui.InputInt("Unknown", ref mat.Unknown);
                ImGui.InputInt("Material Flags", ref mat.MaterialFlags);

                if (ImGui.InputInt("Alt Flags", ref altFlags))
                    mat.AltFlags = (ushort)altFlags;

                if (ImGui.InputInt("Render Flags", ref renderFlags))
                    MaterialWrapper.ObjectData.RenderFlags = renderFlags;
            }
        }

        private void SetColor(Vector3 color, ref ColorRGB_8 value)
        {
            value = new ColorRGB_8()
            {
                R = (byte)(color.X * 255),
                G = (byte)(color.Y * 255),
                B = (byte)(color.Z * 255)
            };
            Material.ReloadColors();
        }

        private void DrawTextureMaps()
        {
            MaterialTextureUI.Render();
        }

        private void DrawShaderTab()
        {
            ImGui.Checkbox("Fragment", ref fragment);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);

            ImGui.BeginChild("shaderWindow");

            if (fragment)
                ImGui.TextUnformatted(Material.Frag);
            else
                ImGui.TextUnformatted(Material.Vert);

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
