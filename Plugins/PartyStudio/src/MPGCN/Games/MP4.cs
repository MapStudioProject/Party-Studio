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
    internal class MP4
    {
        public static List<string> GetSupportedSpaces()
        {
            List<string> spaces = new List<string>();
            foreach (var space in Enum.GetValues(typeof(MP4_BoardSpaceType)))
                spaces.Add(space.ToString());
            return spaces;
        }

        public class MP4Space : SpaceNode
        {
            public PlaceType Place
            {
                get { return (MP4.PlaceType)MPSpace.Param1; }
                set { 
                    MPSpace.Param1 = (ushort)value;

                    foreach (var selected in PathPoint.ParentPath.GetSelectedPoints())
                    {
                        var space = selected.UINode.Tag as MP4Space;
                        space.MPSpace.Param1 = (ushort)value;
                        space.ReloadType();
                    }
                    ReloadType();
                }
            }

            public MP4_BoardSpaceType TypeList
            {
                get { return (MP4_BoardSpaceType)TypeID; }
                set
                {
                    TypeID = (ushort)value;

                    foreach (var selected in PathPoint.ParentPath.GetSelectedPoints())
                    {
                        var space = selected.UINode.Tag as MP4Space;
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

                bool useHex = true;

                if (ImGui.CollapsingHeader("Parameters", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);

                    DrawTypeParam();

                    switch (this.TypeList)
                    {
                        case MP4_BoardSpaceType.Star:
                            DrawPlaceParam("Star Number");  
                            DrawParamLabel("Param 2", useHex, "Param2", MPSpace.Param2);
                            break;
                        default:
                            DrawPlaceParam("Place");
                            DrawParamLabel("Param 1", useHex, "Param1", MPSpace.Param1);
                            DrawParamLabel("Param 2", useHex, "Param2", MPSpace.Param2);
                            break;
                    }
                    ImGui.Columns(1);
                }
            }

            public override void DrawTableUI()
            {
                bool useHex = true;

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<MP4_BoardSpaceType>("##Type", this, "TypeList", ImGuiComboFlags.NoArrowButton);
                ImGui.PopItemWidth();

                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<PlaceType>("##Place", this, "Place", ImGuiComboFlags.NoArrowButton);
                ImGui.PopItemWidth();

                ImGui.NextColumn();

                DrawParam(useHex, "Param2", MPSpace.Param2);

                ImGui.NextColumn();
            }

            private void DrawTypeParam()
            {
                var param1 = (int)MPSpace.Param1;

                ImGui.Text("Type");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<MP4_BoardSpaceType>("##Type", this, "TypeList");
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawSpawnParam()
            {
                var param1 = (int)MPSpace.Param1;

                ImGui.Text("Place");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<PlaceType>("##Place", this, "Place");
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawPlaceParam(string text)
            {
                var param1 = (int)MPSpace.Param1;

                ImGui.Text(text);
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
                MapStudio.UI.ImGuiHelper.ComboFromEnum<PlaceType>("##Place", this, "Place");
                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            private void DrawParamLabel(string label, bool useHex, string property, ushort param)
            {
                ImGui.Text(label);
                ImGui.NextColumn();

                DrawParam(useHex, property, param);

                ImGui.NextColumn();
            }

            private void DrawParam(bool useHex, string property, ushort param)
            {
                int value = param;

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);

                if (useHex)
                {
                    string text = value.ToString("X");
                    if (ImGui.InputText($"##{property}", ref text, 0x20))
                    {
                        var ob = MPSpace.GetType().GetProperty(property);
                        ob.SetValue(MPSpace, ushort.Parse(text, System.Globalization.NumberStyles.HexNumber));
                    }
                }
                else
                {
                    if (ImGui.InputInt($"##{property}", ref value))
                    {
                        var ob = MPSpace.GetType().GetProperty(property);
                        ob.SetValue(MPSpace, param);
                    }
                }

                ImGui.PopItemWidth();
            }

            private void ReloadType()
            {
                Name = TypeList.ToString();
                if (Place != PlaceType.None && TypeList == MP4_BoardSpaceType.Invisible)
                    Name = $"{Place}";

                this.Icon = Name;
                if (PathPoint != null)
                    PathPoint.UINode.Icon = Name;
                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            }

            public override Space Copy()
            {
                return new MP4Space()
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

            public MP4Space() { }

            public MP4Space(BoardLoader loader) : base(loader)
            {
                MPSpace = new MPSpace();
                MPSpace.TypeID = (ushort)MP4_BoardSpaceType.Blue;

                if (!string.IsNullOrEmpty(BoardLoader.ActiveSpaceType))
                    MPSpace.TypeID = (ushort)Enum.Parse(typeof(MP4_BoardSpaceType), BoardLoader.ActiveSpaceType);


                ReloadType();
            }

            public MP4Space(MPSpace space) : base(space)
            {
                MPSpace = space;
                TypeID = space.TypeID;

                ReloadType();
            }
        }

        public enum MP4_BoardSpaceType : ushort
        {
            Invisible,
            Blue,
            Red,
            Bowser,
            Item,
            VS,
            Event,
            Miracle = 7,
            Star,
            Spring,
        }

        //Attribute 1
        public enum PlaceType
        {
            None   = 0,
            Star_1 = 1, //Stars. Number determines the cycle number which is randomized 1 - 7.
            Star_2 = 2,
            Star_3 = 3,
            Star_4 = 4,
            Star_5 = 5,
            Star_6 = 6,
            Star_7 = 7,
            Shop1  = 0x8, //Different shops sell different items
            Shop2  = 0x9,
            Shop3  = 0x10,
            Event1 = 0x20, //Special mini event
            Event2 = 0x30, //Special mini event
            Event3 = 0x40, //Special mini event
            PlayerSpot = 0x200, //Used in places to move player into area
            ToadStar = 0x400, //Host placement with the star floor
            Boo = 0x800, //Boo house. Limit of 1 per board
            Lottery = 0x1000, //Lottery. Limit of 1 per board
            MiniMushroomPipe =  0x2000, //Mini mushroom required to pass
            Start = 0x8000, //Starting point. Must be present for board to work.
        }
    }
}
