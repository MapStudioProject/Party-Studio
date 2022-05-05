using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MapStudio.UI;
using ImGuiNET;
using UIFramework;

namespace PartyStudioPlugin
{
    public class SpaceWindow : DockWindow
    {
        public override string Name => "SPACES";

        BoardLoader Board;

        public SpaceWindow(DockSpaceWindow dockSpace, BoardLoader board): base(dockSpace) {
            Board = board;
            this.DockDirection = ImGuiDir.Right;
            this.ParentDock = Workspace.ActiveWorkspace.ViewportWindow;
            this.SplitRatio = 0.12f;
            this.Opened = true;
        }

        public override void Render()
        {
            var width = ImGui.GetWindowWidth();
            var spaces = Board.SpaceTypeList;
            var itemSize = 54;

            var columnCount = (int)(width / (itemSize));
            var rowCount = columnCount == 0 ? 0 : Math.Max(spaces.Count / columnCount, 1);

            ImGui.Columns(columnCount, "spaceList1", false);

            int index = 0;
            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    var space = spaces[index];
                    if (IconManager.HasIcon(space))
                    {
                        var selColor = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
                        selColor = new Vector4(selColor.X, selColor.Y, selColor.Z, 0.2f);
                        ImGui.PushStyleColor(ImGuiCol.Header, selColor);
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, selColor);

                        var pos = ImGui.GetCursorPos();
                        IconManager.DrawIcon(space, 48);
                        ImGui.SetCursorPos(pos);

                        Vector2 size = new Vector2(ImGui.GetColumnWidth(), itemSize);

                        bool isSelected = space == BoardLoader.ActiveSpaceType;
                        bool select = ImGui.Selectable($"##{space}", isSelected, ImGuiSelectableFlags.AllowItemOverlap, size);

                        if (select)
                        {
                            BoardLoader.ActiveSpaceType = space;
                            Board.PathRender.ResetCreateAction();
                        }

                        ImGui.PopStyleColor(2);
                        ImGui.NextColumn();
                    }
                    index++;
                }
            }
            ImGui.Columns(1);
        }
    }
}
