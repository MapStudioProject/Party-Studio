using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using Toolbox.Core;
using MapStudio.UI;
using MPLibrary.GCN;

namespace PartyStudio.GCN
{
    public class TextureSelectionDialog
    {
        public HsfFile HsfFile;

        public HSFTexture Output;
        public string Previous = "";

        string _searchText = "";

        bool popupOpened = false;
        bool scrolled = false;

        public void Init() { Output = null; }

        public bool Render(ref bool dialogOpened)
        {
            var pos = ImGui.GetCursorScreenPos();

            if (!popupOpened)
            {
                ImGui.OpenPopup("textureSelector1");
                popupOpened = true;
                scrolled = false;
            }

            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(color.X, color.Y, color.Z, 1.0f));

            bool hasInput = false;
            if (ImGui.BeginPopup("textureSelector1", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (ImGui.IsKeyDown((int)ImGuiKey.Enter))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text($"   {IconManager.SEARCH_ICON}  ");

                ImGui.SameLine();

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                if (ImGui.InputText("Search", ref _searchText, 200))
                {
                }
                ImGui.PopStyleVar();

                ImGui.TextColored(ThemeHandler.Theme.Error, $"   {IconManager.DELETE_ICON}   ");
                ImGui.SameLine();

                if (ImGui.Selectable("None"))
                {
                    Output = null;
                    hasInput = true;
                }

                if (HsfFile != null)
                {
                    var width = ImGui.GetWindowWidth();

                    float size = ImGui.GetFrameHeight();
                    ImGui.BeginChild("textureList", new System.Numerics.Vector2(320, 300));
                    bool isSearch = !string.IsNullOrEmpty(_searchText);

                    int index = 0;
                    foreach (var tex in HsfFile.Textures)
                    {
                        bool HasText = tex.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isSearch && !HasText)
                            continue;

                        bool isSelected = Output == tex;
                        int ID = IconManager.GetTextureIcon("TEXTURE");

                        if (tex.RenderTexture != null)
                            ID = tex.RenderTexture.ID;

                        ImGui.Image((IntPtr)ID, new System.Numerics.Vector2(22, 22));
                        ImGui.SameLine();

                        if (!scrolled && isSelected)
                        {
                            ImGui.SetScrollHereY();
                            scrolled = true;
                        }

                        if (ImGui.Selectable($"{tex.Name}##tex{index++}", isSelected))
                        {
                            Output = tex;
                            hasInput = true;
                        }
                        if (ImGui.IsItemFocused() && !isSelected)
                        {
                            Output = tex;
                            hasInput = true;
                        }
                        if (ImGui.IsMouseDoubleClicked(0) && ImGui.IsItemHovered())
                        {
                            ImGui.CloseCurrentPopup();
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();

                        index++;
                    }
                    ImGui.EndChild();
                }
                ImGui.EndPopup();
            }
            else if (popupOpened)
            {
                dialogOpened = false;
                popupOpened = false;
            }
            ImGui.PopStyleColor();

            return hasInput;
        }
    }
}
