using ImGuiNET;
using MapStudio.UI;
using MPLibrary;
using Newtonsoft.Json;
using PartyStudioPlugin.src.UI;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;

namespace PartyStudioPlugin
{
    public class BoardLoaderUI
    {
        public GameList Games = new GameList();

        public BoardLoaderUI()
        {
            Games.Export();
            Games.Import();
        }

        public GameConfig ActiveGame;

        public string SelectedFile;

        public string FilePath => Path.Combine(ActiveGame.GamePath, SelectedFile);


        public void Render()
        {
            if (ImGui.BeginChild("game_list", new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23)))
            {
                ImGui.Columns(4);
                foreach (GameConfig game in Games.List)
                {
                    if (ImGui.RadioButton(game.Version.ToString(), game == ActiveGame))
                        ActiveGame = game;

                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("file_list", new System.Numerics.Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() - 100)))
            {
                if (ActiveGame != null)
                {
                    string folder = ActiveGame.GamePath;
                    if (ImguiCustomWidgets.PathSelector("Data Folder", ref folder))
                    {
                        ActiveGame.GamePath = folder;
                        ActiveGame.SaveConfig(); //save config
                    }

                    ImGui.Columns(2);

                    ImGuiHelper.BoldText("File Name"); ImGui.NextColumn();
                    ImGuiHelper.BoldText("Name"); ImGui.NextColumn();

                    foreach (var file in ActiveGame.BoardFileList)
                    {
                        bool selected = SelectedFile == file.Value;
                        if (ImGui.Selectable(file.Value, selected, ImGuiSelectableFlags.SpanAllColumns))
                            SelectedFile = file.Value;
                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            DialogHandler.ClosePopup(true);

                        ImGui.NextColumn();
                        ImGui.Text(file.Key);
                        ImGui.NextColumn();
                    }
                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();

            DialogHandler.DrawCancelOk();
        }

        public void LoadBoard()
        {
            if (!string.IsNullOrEmpty(SelectedFile))
                return;

            //Game path should be the folder of where the map files are in
            string path = Path.Combine(ActiveGame.GamePath, $"{SelectedFile}.bin");
            if (!File.Exists(path))
                return;


        }
    }
}
