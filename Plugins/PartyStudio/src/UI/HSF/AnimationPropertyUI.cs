using ImGuiNET;
using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartyStudioPlugin
{
    public class AnimationPropertyUI
    {
        public void Render(HSFMotionAnimation anim, string json)
        {
            ImGui.Columns(6);
            foreach (var track in anim.GetAllTracks().OrderBy(x => x.TrackMode))
            {
                ImGui.Text($"{track.TrackMode}");
                ImGui.NextColumn();
                ImGui.Text($"{track.TrackEffect}");
                ImGui.NextColumn();
                ImGui.Text($"{track.ValueIdx}");
                ImGui.NextColumn();
                ImGui.Text($"{track.Name}");
                ImGui.NextColumn();
                ImGui.Text($"{track.InterpolationType}");
                ImGui.NextColumn();

                if (track.KeyFrames.Count > 0)
                {
                    ImGui.Text($"KeyFrames {track.KeyFrames.Count}");
                    ImGui.NextColumn();
                }
                else
                {
                    ImGui.Text($"{track.Constant}");
                    ImGui.NextColumn();
                }
            }
            ImGui.Columns(1);
        }
    }
}
