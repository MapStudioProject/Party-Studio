using GCNRenderLibrary.Rendering;
using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;
using MPLibrary.GCN;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartyStudio.GCN
{
    internal class MaterialTextureUI
    {
        private int selectedTextureIndex;
        private float uvWindowHeight = 150;

        private UVViewport UVViewport;

        private List<AttributeData> TextureAttributes = new List<AttributeData>();

        HSF.MaterialWrapper MaterialWrapper;
        Material Material;

        TextureSelectionDialog TextureDialog = new TextureSelectionDialog();

        bool open_texture_dialog;

        public void Init(HSF.MaterialWrapper materialWrapper, Material mat)
        {
            selectedTextureIndex = 0;

            MaterialWrapper = materialWrapper;
            Material = mat;
            TextureAttributes = Material.TextureAttributes.Select(x => x.AttributeData).Where(x => x != null).ToList();

            if (UVViewport == null)
                UVViewport = new UVViewport();

            ReloadUVDisplay();

            TextureDialog.HsfFile = mat.HsfFile;
        }

        public void Render()
        {
            var width = ImGui.GetWindowWidth();

            if (ImGui.Button($"   {IconManager.ADD_ICON}   "))
            {
                selectedTextureIndex = Material.TextureAttributes.Count;

                var texMap = new AttributeData();
                TextureAttributes.Add(texMap);
                Material.TextureAttributes.Add(new TextureAttribute()
                {
                    AttributeData = texMap,
                });
                UpdateTextureMaps();
            }

            ImGui.SameLine();

            if (ImGui.Button($"   {IconManager.DELETE_ICON}   "))
            {
                if (selectedTextureIndex >= 0 && selectedTextureIndex < TextureAttributes.Count)
                {
                    var texMap = TextureAttributes[selectedTextureIndex];
                    TextureAttributes.Remove(texMap);
                    Material.TextureAttributes.RemoveAt(selectedTextureIndex);
                    if (TextureAttributes.Count == 0)
                        selectedTextureIndex = -1;
                    else
                        selectedTextureIndex = 0;
                    UpdateTextureMaps();
                }
            }

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
            if (ImGui.BeginChild("textureList", new Vector2(width, 62)))
            {
                for (int i = 0; i < TextureAttributes.Count; i++)
                {
                    var tex = Material.GetTexture(i);
                    DrawTextureItemUI(i, TextureAttributes[i].TextureIndex);
                }
            }
            ImGui.EndChild();

            ImGui.PopStyleColor();

            if (TextureAttributes.Count > selectedTextureIndex && selectedTextureIndex >= 0)
            {
                DrawTextureInfo();
            }
        }

        void DrawTextureItemUI(int i,  int textureIndex)
        {
            int ID = IconManager.GetTextureIcon("TEXTURE");

            string label = "";
            var tex = Material.TextureAttributes[i].Texture;
            if (tex != null && tex.RenderTexture != null)
            {
                ID = tex.RenderTexture.ID;
                label = tex.Name;
            }

            ImGui.Image((IntPtr)ID, new Vector2(18, 18));
            ImGui.SameLine();


            bool select = selectedTextureIndex == i;
            if (ImGui.Selectable(string.IsNullOrEmpty(label) ? $"None##texmap{i}" : $"{label}##texmap{i}", ref select))
            {
                selectedTextureIndex = i;
                MaterialWrapper.DisplayTextureAnimationUI?.Invoke(selectedTextureIndex);
                ReloadUVDisplay();
            }
        }

        private void DrawTextureInfo()
        {
            var texMap = Material.TextureAttributes[selectedTextureIndex].AttributeData;
            bool enableNBT = texMap.NbtEnable != 0.0f;
            bool enableTexture = texMap.TextureEnable == 1.0f;

            var width = ImGui.GetWindowWidth();
            var size = ImGui.GetWindowSize();

            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button($"   {IconManager.IMAGE_ICON}   "))
                {
                    open_texture_dialog = true;
                }
                ImGui.SameLine();

                string name = "None";
                if (Material.TextureAttributes[selectedTextureIndex].Texture != null)
                    name = Material.TextureAttributes[selectedTextureIndex].Texture.Name;

                string texName = name == null ? "" : name;
                ImGui.InputText("Name", ref texName, 0x200, ImGuiInputTextFlags.ReadOnly);
            }

            if (open_texture_dialog)
            {
                if (TextureDialog.Render(ref open_texture_dialog))
                {
                    Material.TextureAttributes[selectedTextureIndex].Texture = TextureDialog.Output;
                    UpdateTextureMaps();
                }
            }

            if (ImGui.CollapsingHeader("Preview", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginChild("uvWindow", new Vector2(width, uvWindowHeight), false))
                {
                    var pos = ImGui.GetCursorScreenPos();

                    this.UVViewport.Render((int)width, (int)ImGui.GetWindowHeight());

                    ImGui.SetCursorScreenPos(pos);
                    ImGui.Checkbox("Show UVs", ref this.UVViewport.DisplayUVs);
                }
                ImGui.EndChild();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Separator]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.SeparatorHovered]);
                ImGui.Button("##hsplitter", new Vector2(-1, 2));
                if (ImGui.IsItemActive())
                {
                    var deltaY = -ImGui.GetIO().MouseDelta.Y;
                    if (uvWindowHeight - deltaY < size.Y - 22 && uvWindowHeight - deltaY > 22)
                        uvWindowHeight -= deltaY;
                }
            }

            ImGui.BeginChild("propertiesWindow");

            if (ImGui.CollapsingHeader("Material Flags", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Checkbox("Texture Enable", ref enableTexture))
                {
                    texMap.TextureEnable = enableTexture ? 1.0f : 0.0f; //Just a toggle. 0 for not used, 1.0 for used
                    Material.ReloadTevStages(true);
                }

                if (ImGui.SliderFloat("Bump Map Blend", ref texMap.NbtEnable, 0, 20.0f)) //Use as a bump map if > 0
                    Material.ReloadTevStages(true);

                if (UIHelper.DrawEnum("Combiner Method", ref texMap.BlendingFlag)) //Blend by adding or blending by transparency between stages
                    Material.ReloadTevStages(true);

                ImGuiHelper.Tooltip("Additive: Combines current texture with last one using blend amount.\n" +
                                     "Transparent Mix: Combines current texture with last one using the current texture's alpha channel.");

                if (ImGui.SliderFloat("Combiner Blend", ref texMap.BlendTextureAlpha, 0, 1.0f)) //Blends previous and current texture with blend value
                    Material.ReloadTevStages(true);

                EditByte("Alpha Flags", ref texMap.AlphaFlag); //Unsure the purpose of these. Usually 1 for alpha textures
                ImGui.InputInt("Texture Flags", ref texMap.TextureFlags); //Configures mip map usage
            }

            if (ImGui.CollapsingHeader("Wrap", ImGuiTreeNodeFlags.DefaultOpen))
            {
                UIHelper.DrawEnum("Wrap S", ref texMap.WrapS, UpdateTextureMaps);
                UIHelper.DrawEnum("Wrap T", ref texMap.WrapT, UpdateTextureMaps);
            }

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var p = new Vector2(texMap.TexAnimStart.Position.X, texMap.TexAnimStart.Position.Y);
                var s = new Vector2(1f / texMap.TexAnimStart.Scale.X, 1f / texMap.TexAnimStart.Scale.Y);

                if (ImGui.DragFloat2("Position##PosStart", ref p))
                {
                    SetVector2(p, ref texMap.TexAnimStart.Position);
                    Material.ReloadTextures();
                    Material.ReloadTevStages(true);
                    ReloadUVDisplay();
                }
                if (ImGui.DragFloat2("Scale##ScaStart", ref s))
                {
                    s = new Vector2(1f / s.X, 1f / s.Y);

                    SetVector2(s, ref texMap.TexAnimStart.Scale);
                    Material.ReloadTextures();
                    Material.ReloadTevStages(true);
                    ReloadUVDisplay();
                }

                var r = new Vector3(texMap.Rotation.X, texMap.Rotation.Y, texMap.Rotation.Z);
                if (ImGui.DragFloat3("Unknown Angle", ref r))
                    texMap.Rotation = new Vector3XYZ(r.X, r.Y, r.Z);
            }

            if (ImGui.CollapsingHeader("Mip Map", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.InputInt("LOD", ref texMap.MipmapMaxLOD);
            }

            //As far as I can tell, these are mostly not used. No noticable reference in code (maybe used for particles?)
            if (ImGui.CollapsingHeader("Extras", ImGuiTreeNodeFlags.None))
            {
                EditUshort("Unknown", ref texMap.Unknown1);

                ImGui.DragFloat("Unknown 3", ref texMap.Unknown3);
                ImGui.InputInt("Unknown 4", ref texMap.Unknown2);

                ImGui.DragFloat("Unknown 5", ref texMap.Unknown5);
                ImGui.DragFloat("Unknown 6", ref texMap.Unknown6);
                ImGui.DragFloat("Unknown 7", ref texMap.Unknown7);

                ImGui.InputInt("Unknown 8", ref texMap.Unknown8);
                ImGui.InputInt("Unknown 9", ref texMap.Unknown9);
                ImGui.InputInt("Unknown 10", ref texMap.Unknown10);

                ImGui.DragFloat("Unknown 12", ref texMap.Unknown4);

                ImGui.DragFloat("Unknown 13", ref texMap.Unknown13);
            }

            ImGui.EndChild();
        }

        private void UpdateTextureMaps()
        {
            Material.ReloadTextures();
            Material.ReloadTevStages();

            MaterialWrapper.Render.ReloadTextures(Material.RenderScene);
            Material.RenderScene?.ReloadShader();
            ReloadUVDisplay();

            GLContext.ActiveContext.UpdateViewport = true;
        }

        private void SetVector2(Vector2 value, ref Vector2XYZ vec)
        {
            vec = new Vector2XYZ()
            {
                X = value.X,
                Y = value.Y,
            };
        }

        private void EditByte(string label, ref byte value)
        {
            int v = value;
            if (ImGui.InputInt(label, ref v))
                value = (byte)v;
        }

        private void EditUshort(string label, ref ushort value)
        {
            int v = value;
            if (ImGui.InputInt(label, ref v))
                value = (ushort)v;
        }

        private void ReloadUVDisplay()
        {
            if (Material.TextureAttributes.Count > 0)
            {
                var texMap = Material.TextureAttributes[selectedTextureIndex].AttributeData;
                var tex = Material.TextureAttributes[selectedTextureIndex].Texture;

                int ID = IconManager.GetTextureIcon("BLANK");

                int width = 1;
                int height = 1;

                if (tex != null && tex.RenderTexture != null)
                {
                    width = (int)tex.TextureInfo.Width; height = (int)tex.TextureInfo.Height;
                    ID = tex.RenderTexture.ID;
                }

                UVViewport.Camera.Zoom = 31.5F;
                UVViewport.ActiveObjects.Clear();
                foreach (GXMesh mesh in MaterialWrapper.GetMeshes())
                {
                    //Add mesh to UV list
                    List<OpenTK.Vector2> texCoords = new List<OpenTK.Vector2>();
                    List<int> indices = new List<int>();

                    for (int i = 0; i < mesh.TexCoord0.Count; i++)
                    {
                        texCoords.Add(new OpenTK.Vector2(mesh.TexCoord0[i].X, mesh.TexCoord0[i].Y));
                    }

                    //Use all sub mesh indices
                    foreach (var ind in mesh.Indices)
                        indices.Add(ind);

                    //Add to UV viewer and update it
                    UVViewport.ActiveObjects.Add(new UVMeshObject(texCoords, indices.ToArray()));
                    UVViewport.UpdateVertexBuffer = true;
                }

                var matrix = Material.TextureMatrices[selectedTextureIndex].GetSRTMatrix();
                UVViewport.ActiveMatrix = matrix;

                UVViewport.ActiveTextureMap = new TextureSamplerMap()
                {
                    ID = ID,
                    WrapU = GetWrap(texMap.WrapS),
                    WrapV = GetWrap(texMap.WrapT),
                    MagFilter = TextureMagFilter.Linear,
                    MinFilter = TextureMinFilter.Linear,
                    Width = width,
                    Height = height,
                };
            }
        }

        private static TextureWrapMode GetWrap(WrapMode Wrap)
        {
            switch (Wrap)
            {
                case WrapMode.Clamp: return TextureWrapMode.ClampToEdge;
                case WrapMode.Repeat: return TextureWrapMode.Repeat;
                case WrapMode.Mirror: return TextureWrapMode.MirroredRepeat;

                default: throw new ArgumentException("Invalid wrap mode!");
            }
        }
    }
}
