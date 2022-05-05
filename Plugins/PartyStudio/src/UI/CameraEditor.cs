using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapStudio.UI;
using Toolbox.Core.Animations;
using CafeLibrary.Rendering;
using ImGuiNET;
using GLFrameworkEngine;

namespace PartyStudioPlugin
{
    public class CameraEditor : UIFramework.Window
    {
        public override string Name => TranslationSource.GetText("CAMERA_EDITOR");

        public List<BfresCameraAnim> Anims = new List<BfresCameraAnim>();

        AnimationPlayer Player;

        public CameraEditor()
        {
            Player = new AnimationPlayer();
        }

        private BfresCameraAnim selectedAnim;

        public override void Render()
        {
            DrawAnimPlayer();
            foreach (var anim in Anims)
            {
                bool isSelected = selectedAnim == anim;
                if (ImGui.Selectable(anim.Name, isSelected)) {
                    selectedAnim = anim;
                    PreparePlayer();
                }
            }
        }

        private void DrawAnimPlayer()
        {
            if (Player.IsPlaying) {
                if (ImGui.Button("Stop"))
                {
                    Player.Stop();

                    var viewCamera = GLContext.ActiveContext.Camera;
                    viewCamera.ResetAnimations();
                }
            }
            else
            {
                if (ImGui.Button("Play"))
                {
                    Player.Play();
                }
            }
        }

        private void PreparePlayer()
        {
            if (selectedAnim == null)
                return;

            Player.AddAnimation(selectedAnim, "");
        }
    }
}
