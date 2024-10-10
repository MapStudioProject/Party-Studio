using ImGuiNET;
using IONET.Collada.Kinematics.Articulated_Systems;
using MapStudio.UI;
using MPLibrary.GCN;
using PartyStudioPlugin.src.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Animations;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using UIFramework;
using static MapStudio.UI.AnimationTree;

namespace PartyStudioPlugin
{
    public class PlayerViewer : FileEditor, IFileFormat
    {
        public GameList Games = new GameList();

        private GameConfig ActiveConfig = new GameConfig();

        public string[] Description => new string[0];
        public string[] Extension => new string[0];
        public bool CanSave { get; set; } = true;
        public File_Info FileInfo { get; set; }

        private string PlayerName;

        private MPBIN model_bin;

        private AnimationList AnimationWindow;

        public PlayerViewer()
        {
            Games.Import();
        }

        public void LoadGUI()
        {
            //determine what game to load the player list from
            if (ImGui.BeginChild("game_list", new System.Numerics.Vector2(ImGui.GetWindowWidth(), 23)))
            {
                ImGui.Columns(4);
                foreach (GameConfig game in Games.List)
                {
                    if (ImGui.RadioButton(game.Version.ToString(), game == ActiveConfig))
                        ActiveConfig = game;

                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();

            if (ActiveConfig != null)
            {
                string folder = ActiveConfig.GamePath;
                if (ImguiCustomWidgets.PathSelector("Data Folder", ref folder))
                {
                    ActiveConfig.GamePath = folder;
                    ActiveConfig.SaveConfig(); //save config
                }
            }

            DialogHandler.DrawCancelOk();
        }

        public bool Load()
        {
            AnimationWindow = new AnimationList(this.Workspace);
            AnimationWindow.DockDirection = ImGuiDir.Down;
            AnimationWindow.SplitRatio = 0.5f;
            AnimationWindow.ParentDock = this.Workspace.Outliner;

            this.FileInfo = new File_Info();

            this.Root = new NodeBase("Player Viewer");
            this.Root.ContextMenus.Add(new MenuItemModel("Save", () =>
            {
                int file_num = 1;
                string path = Path.Combine(ActiveConfig.GamePath, $"{PlayerName}mdl{file_num}.bin");

                var mem = new MemoryStream();
                model_bin.Save(mem);
                File.WriteAllBytes(path, mem.ToArray());
            }));
            this.Root.Tag = this;

            //No list present, skip loading
            if (ActiveConfig.PlayerFileList.Count == 0)
                return false;

            //Load the first file by default. A UI in the viewer will configure what player to show
            PlayerName = this.ActiveConfig.PlayerFileList.Keys.FirstOrDefault();
            ReloadFileList();

            return true;
        }

        private void ReloadFileList()
        {
            int file_num = 1;

            model_bin = MPBIN.LoadFile(Path.Combine(ActiveConfig.GamePath, $"{PlayerName}mdl{file_num}.bin"));
            var motion_bin = MPBIN.LoadFile(Path.Combine(ActiveConfig.GamePath, $"{PlayerName}mot.bin"));

            //
            Root.Children.Clear();
            for (int i = 0; i < model_bin.files.Count; i++)
            {
                var model_hsf = PartyStudio.GCN.HSF.LoadFile(model_bin.files[i].FileData);
                model_hsf.Root.Header = $"ModelLOD{i}";
                model_hsf.Root.Icon = IconManager.MODEL_ICON.ToString();
                model_hsf.Root.HasCheckBox = true;
                model_hsf.Root.Tag = model_hsf;
                model_hsf.FileInfo = new File_Info();
                model_hsf.Scene.Init();

                model_bin.files[i].FileFormat = model_hsf;

                model_hsf.Root.OnChecked += delegate
                {
                    model_hsf.Render.IsVisible = model_hsf.Root.IsChecked;
                };
                model_hsf.Root.IsChecked = i == 0;

                AddRender(model_hsf.Render);

                Root.AddChild(model_hsf.Root);
            }

            //animation nodes
            AnimationWindow.Clear();
            for (int i = 0; i < motion_bin.files.Count; i++)
            {
                var motion_hsf = PartyStudio.GCN.HSF.LoadFile(motion_bin.files[i].FileData);
                AnimationWindow.AddAnimation(motion_hsf, i);
            }
        }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return false;
        }

        public void Load(Stream stream)
        {
        }

        public void Save(Stream stream)
        {
        }

        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(AnimationWindow);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(Workspace.TimelineWindow);
            windows.Add(Workspace.GraphWindow);
            return windows;
        }

        public class AnimationList : DockWindow
        {
            public override string Name => "Animation List";

            public TreeView Tree = new TreeView();

            public AnimationList(DockSpaceWindow parent) : base(parent)
            {
                this.DockDirection = ImGuiDir.Left;
            }

            public override void Render()
            {
                base.Render();

                Tree.Render();
            }

            public void Clear()
            {
                Tree.Nodes.Clear();
            }

            public void AddAnimation(PartyStudio.GCN.HSF motion_hsf, int index)
            {
                var anim = motion_hsf.Animations.FirstOrDefault();

                TreeNode animNode = new TreeNode($"Animation{index}");
                animNode.Tag = anim;
                foreach (var menuItem in anim.GetMenuItems())
                {
                    animNode.ContextMenus.Add(new MenuItem(menuItem.Header, () =>
                    {
                      //  menuItem.Click();
                    })); 
                }
                animNode.Icon = IconManager.SKELEAL_ANIM_ICON.ToString();
                animNode.OnSelected += delegate
                {
                    var workspace = Workspace.ActiveWorkspace;
                    workspace.TimelineWindow.AddAnimation(anim);
                };
                Tree.Nodes.Add(animNode);
            }
        }
    }
}
