using ImGuiNET;
using PartyStudioPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIFramework;

namespace PartyStudio.GCN
{
    internal class SpaceTableWindow : Window
    {
        public override string Name => "Space Data";

        BoardLoader Board;

        public SpaceTableWindow(BoardLoader board) {
            Board = board;
        }

        public override void Render()
        {
            ImGui.Columns(3); //text + num params
            foreach (var space in Board.Spaces)
            {
                space.DrawTableUI();
            }
            ImGui.Columns(1);
        }
    }
}
