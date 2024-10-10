using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FirstPlugin.BoardEditor
{
    public class BoardHelper
    {
        //Data from https://github.com/shibbo/SMPe/blob/master/SMPe/Helper.cs
        /*
        © 2018 - shibboleet
        SMPe is free software: you can redistribute it and/or modify it under
        the terms of the GNU General Public License as published by the Free
        Software Foundation, either version 3 of the License, or (at your option)
        any later version.
        SMPe is distributed in the hope that it will be useful, but WITHOUT ANY 
        WARRANTY; See the GNU General Public License for more details.
        You should have received a copy of the GNU General Public License along 
        with SMPe. If not, see http://www.gnu.org/licenses/.
        */

        public static Dictionary<string, string> mSimpleNodeNames = new Dictionary<string, string>()
        {
            { "PLUS", "Blue" },
            { "MINUS", "Red" },
            { "LUCKY", "Lucky" },
            { "LUCKY_2", "Lucky2" },
            { "DONT_ENTRY", "Non-Enterable" },
            { "HATENA_1", "Event1" },
            { "HATENA_2", "Event2" },
            { "HATENA_3", "Event3" },
            { "HATENA_4", "Event4" },
            { "START", "StartingPoint" },
            { "MARK_PC", "CharacterStartPoint" },
            { "MARK_STAR", "Star" },
            { "MARK_STAROBJ", "Toadette" },
            { "SUPPORT", "Ally" },
            { "HAPPENING", "Unlucky" },
            { "ITEM", "Item" },
            { "BATTAN", "Whomp" },
            { "DOSSUN", "Thwomp" },
            { "JUGEMU_OBJ", "LakituCloud" },
            { "JUGEMU", "Lakitu" },
            { "JOYCON", "VS" },
            { "TREASURE_OBJ", "Treasure" },
            { "SHOP_A", "Shop 1 (Normal)" },
            { "SHOP_B", "Shop 2 (Special)" },
            { "SHOP_C", "Shop 3" },
            { "HARD_SHOP", "Shop (Force Buy)" },
            { "ICECREAM_SHOP", "Ice Cream Shop" },
            { "CLAYPIPE", "Pipe" },
            { "CLAYPIPE_RED", "Red Pipe" },
            { "PRESENT_BOX", "Present Space" },
            { "PRESENT_BOX_OBJ", "Present Object" },
            { "TURNOUT_SWITCH", "Conveyer Belt Space" },
            { "NPC_BOMBHEI", "Bob-omb Space" },
            { "", "Branch" }
        };

        public static Dictionary<string, string> massModels = new Dictionary<string, string>()
        {
            { "ITEM",      "bds001_mass00" },
            { "LUCKY",     "bds001_mass02" },
            { "LUCKY_2",    "bds001_mass02" },
            { "JOYCON",    "bds001_mass03" },
            { "HATENA_1",  "bds001_mass04" },
            { "HATENA_2",  "bds001_mass04" },
            { "HATENA_3",  "bds001_mass04" },
            { "HATENA_4",  "bds001_mass04" },
            { "HAPPENING", "bds001_mass05" },
            { "HAPPENING_2","bds001_mass06" }, //todo confirm if space is called this
            { "PLUS",      "bds001s_mass50" },
            { "MINUS",     "bds001s_mass51" },
            { "SUPPORT",   "bds001s_mass52" },
            { "START",     "bds001s_mass53" },
            { "SIGNBOARD_SHOP",         "bds001_sign00" },
            { "SIGNBOARD_JUGEMU",       "bds001_sign01" },
            { "SIGNBOARD_PATAPATA",     "bds001_sign02" },
        };

        public static Dictionary<string, string> shopModels = new Dictionary<string, string>()
        {
            { "JUGEMU_OBJ", "npc017_m1" },
            { "PATAPATA_OBJ", "npc053_m1" },
        };

        public static Dictionary<string, Color> mNodeTypeToColor = new Dictionary<string, Color>()
        {
            { "PLUS", Color.Blue },
            { "MINUS", Color.Red },
            { "LUCKY", Color.LightGreen },
            { "HATENA_1", Color.DarkOliveGreen },
            { "HATENA_2", Color.DarkOliveGreen },
            { "HATENA_3", Color.DarkOliveGreen },
            { "HATENA_4", Color.DarkOliveGreen },
            { "ITEM", Color.Green },
            { "ITEM_2", Color.Green },
            { "SUPPORT", Color.SlateBlue },
            { "START", Color.DarkGreen },
            { "MARK_PC", Color.DarkViolet },
            { "JOYCON", Color.Orange },
            { "TREASURE_OBJ", Color.Brown },
            { "BATTAN", Color.DarkGray },
            { "DOSSUN", Color.Gray },
            { "MARK_STAR", Color.Yellow },
            { "MARK_STAROBJ", Color.DeepPink },
            { "HAPPENING", Color.DarkRed },
            { "SHOP_A", Color.Silver },
            { "SHOP_B", Color.Silver },
            { "SHOP_C", Color.Silver },
            { "", Color.White }
        };
    }
}
