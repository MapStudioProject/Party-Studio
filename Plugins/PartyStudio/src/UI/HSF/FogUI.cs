using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapStudio.UI;
using ImGuiNET;

namespace PartyStudio.GCN
{
    internal class FogUI
    {
        public static void Render(HsfFile hsf, HsfRender render)
        {
            bool update = false;

            bool useFog = hsf.FogData.Count > 0;
            ImguiPropertyColumn.Begin("fogProperties");

            if (ImguiPropertyColumn.Bool("Enable", ref useFog))
            {
                hsf.FogData.Count = (uint)(useFog ? 1 : 0);
                update = true;
            }

            update |= ImguiPropertyColumn.SliderFloat("Start", ref hsf.FogData.Start, 0, 10000);
            update |= ImguiPropertyColumn.SliderFloat("End", ref hsf.FogData.End, 0, 1000);

            //Note: Color start unused
            update |= ImguiPropertyColumn.ColorEdit4("Color", ref hsf.FogData.Color, ImGuiColorEditFlags.NoInputs);

            ImguiPropertyColumn.End();

            if (update)
            {
                render.SetFog(hsf.FogData);
                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            }
        }
    }
}
