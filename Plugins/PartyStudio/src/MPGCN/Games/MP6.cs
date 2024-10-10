using ImGuiNET;
using MPLibrary.GCN;
using PartyStudioPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PartyStudio.GCN.MPGCN;

namespace PartyStudio.GCN
{
    internal class MP6
    {
        public static List<string> GetSupportedSpaces()
        {
            List<string> spaces = new List<string>();
            foreach (var space in Enum.GetValues(typeof(MP6_BoardSpaceType)))
                spaces.Add(space.ToString());
            return spaces;
        }

        public class MP6Space : SpaceNode
        {
            public MP6_BoardSpaceType TypeList
            {
                get { return (MP6_BoardSpaceType)TypeID; }
                set
                {
                    TypeID = (ushort)value;

                    foreach (var selected in PathPoint.ParentPath.GetSelectedPoints())
                    {
                        var space = selected.UINode.Tag as MP6Space;
                        space.TypeID = (ushort)value;
                        space.ReloadType();
                    }
                    ReloadType();
                }
            }

            public ushort TypeID
            {
                get { return MPSpace.TypeID; }
                set
                {
                    MPSpace.TypeID = value;
                    ReloadType();
                }
            }

            public override void DrawUI()
            {
                base.DrawUI();

                var param1 = (int)MPSpace.Param1;
                var param2 = (int)MPSpace.Param2;

                if (ImGui.CollapsingHeader("Parameters", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);

                    DrawTypeParam();

                    switch (this.TypeList)
                    {
                        default:
                            DrawParam1();
                            DrawParam2();
                            DrawParam3();
                            break;
                    }
                    ImGui.Columns(1);
                }
            }

            private void DrawTypeParam()
            {
                var param1 = (int)MPSpace.Param1;

                ImGui.Text("Type");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<MP6_BoardSpaceType>("##Type", this, "TypeList");
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawParam1()
            {
                var param1 = (int)MPSpace.Param1;

                ImGui.Text("Param 1");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                if (ImGui.InputInt("##Param1", ref param1))
                    MPSpace.Param1 = (ushort)param1;
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawParam2()
            {
                var param2 = (int)MPSpace.Param2;

                ImGui.Text("Param 2");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                if (ImGui.InputInt("##Param2", ref param2))
                    MPSpace.Param2 = (ushort)param2;
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawParam3()
            {
                var param3 = (int)MPSpace.Param3;

                ImGui.Text("Param 3");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                if (ImGui.InputInt("##Param3", ref param3))
                    MPSpace.Param3 = (ushort)param3;
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void ReloadType()
            {
                Name = TypeList.ToString();

                this.Icon = Name;
                if (PathPoint != null)
                    PathPoint.UINode.Icon = Name;
                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            }

            public override Space Copy()
            {
                return new MP6Space()
                {
                    MPSpace = new MPSpace()
                    {
                        Param1 = MPSpace.Param1,
                        Param2 = MPSpace.Param2,
                        Param3 = MPSpace.Param3,
                        TypeID = MPSpace.TypeID,
                    },
                    Rotation = Rotation,
                    Position = Position,
                    Scale = Scale,
                    Name = Name,
                    Icon = Icon,
                };
            }

            public MP6Space() {
                MPSpace = new MPSpace();
                MPSpace.TypeID = (ushort)MP6_BoardSpaceType.Blue;

                if (!string.IsNullOrEmpty(BoardLoader.ActiveSpaceType))
                    MPSpace.TypeID = (ushort)Enum.Parse(typeof(MP6_BoardSpaceType), BoardLoader.ActiveSpaceType);

                ReloadType();
            }

            public MP6Space(BoardLoader loader) : base(loader)
            {
                MPSpace = new MPSpace();
                MPSpace.TypeID = (ushort)MP6_BoardSpaceType.Blue;

                if (!string.IsNullOrEmpty(BoardLoader.ActiveSpaceType))
                    MPSpace.TypeID = (ushort)Enum.Parse(typeof(MP6_BoardSpaceType), BoardLoader.ActiveSpaceType);

                ReloadType();
            }

            public MP6Space(MPSpace space) : base(space)
            {
                MPSpace = space;
                TypeID = space.TypeID;

                ReloadType();
            }
        }

        public enum MP6_BoardSpaceType
        {
            Invisible,
            Blue,
            Red,
            Event,
            Miracle,
            VS,
            DK_Bowser,
            Star,
            Orb,
            Shop,
        }
    }
}
