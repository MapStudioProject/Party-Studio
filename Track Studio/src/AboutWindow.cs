using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Numerics;
using UIFramework;
using ImGuiNET;
using MapStudio.UI;
using Toolbox.Core;

namespace PartyStudio
{
    public class AboutWindow : Window
    {
        public override string Name => TranslationSource.GetText("ABOUT");

        public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoDocking;

        string AppVersion;

        public AboutWindow()
        {
            Size = new Vector2(500, 600);
            var asssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = asssemblyVersion.ToString();
            Opened = false;
        }

        public override void Render()
        {
            if (!IconManager.HasIcon("TOOL_ICON"))
                IconManager.AddIcon("TOOL_ICON", Properties.Resources.Icon, false);

            base.Render();

            ImGui.Image((IntPtr)IconManager.GetTextureIcon("TOOL_ICON"), new Vector2(50, 50));
            var bottom = ImGui.GetCursorPos();

            ImGui.SameLine();

            ImGui.SetWindowFontScale(1.5f);
            ImGui.AlignTextToFramePadding();

            var textPos = ImGui.GetCursorPos();
            ImGui.Text($"Party Studio v{AppVersion}");
            ImGui.SetWindowFontScale(1);

            ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 30));
            MapStudio.UI.ImGuiHelper.HyperLinkText("Copyright @ KillzXGaming 2022");

            ImGui.SetCursorPos(bottom);

            if (ImGui.CollapsingHeader("Credits"))
            {
                ImGui.BulletText("KillzXGaming - main developer");
                ImGui.BulletText("Abood XD / MasterVermilli0n - for wii u and switch texture swizzling. Also for the awesome effect decomp library.");
                ImGui.BulletText("Syroot - for wii u bfres library and binary IO");
                ImGui.BulletText("Ryujinx - for shader libraries used to decompile and translate switch binaries into glsl code.");
                ImGui.BulletText("JuPaHe64 - created animation timeline and helped me fix gizmo tools");
                ImGui.BulletText("OpenTK Team - for opengl c# bindings.");
                ImGui.BulletText("mellinoe and IMGUI Team - for c# port and creating the IMGUI library");
                ImGui.BulletText("MelonSpeedruns for program icon");
                
                MapStudio.UI.ImGuiHelper.BoldText("Beta Testers:");
                ImGui.BulletText("voidsource");
                ImGui.BulletText("Stuffy360");
                ImGui.BulletText("Divengerss");
                ImGui.BulletText("BluGoku");
            }
            ImGui.EndChild();
        }
    }
}
