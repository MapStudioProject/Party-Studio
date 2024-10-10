using MPLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace PartyStudioPlugin.src.UI
{
    public class GameList
    {
        public List<GameConfig> List = new List<GameConfig>();

        public static string ConfigDirectory => Path.Combine(Runtime.ExecutableDir, "PartyData");

        public void Import()
        {
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);

            List.Clear();
            foreach (var gameConfig in Directory.GetFiles(Path.Combine(Runtime.ExecutableDir, "PartyData")))
            {
                GameConfig game = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(gameConfig));
                List.Add(game);
            }
        }

        public void Export()
        {
            GameConfig game = new GameConfig(GameVersion.MP4);
            game.BoardFileList.Add("w01", "Toad's Midway Madness");
            game.BoardFileList.Add("w02", "Goomba's Greedy Gala");
            game.BoardFileList.Add("w03", "Shy Guy's Jungle Jam");
            game.BoardFileList.Add("w04", "Boo's Haunted Bash");
            game.BoardFileList.Add("w05", "Koopa's Seaside Soiree");
            game.BoardFileList.Add("w06", "Bowser's Gnarly Party");
            game.BoardFileList.Add("w20", "Mega Board Mayhem");
            game.BoardFileList.Add("w21", "Mini Board Mad-Dash");

            game.PlayerFileList.Add("mario", "Mario");
            game.PlayerFileList.Add("luigi", "Luigi");
            game.PlayerFileList.Add("peach", "Peach");
            game.PlayerFileList.Add("daisy", "Daisy");
            game.PlayerFileList.Add("wario", "Wario");
            game.PlayerFileList.Add("waluigi", "Waluigi");
            game.PlayerFileList.Add("dk", "DK");
            game.PlayerFileList.Add("yoshi", "Yoshi");

            game.PlayerMotionList.Add(0, "Idle");

            List.Add(game);

            game.SaveConfig();
        }
    }

    public class GameConfig
    {
        //Files of boards to load, then board name
        public Dictionary<string, string> BoardFileList { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PlayerFileList { get; set; } = new Dictionary<string, string>();
        public Dictionary<int, string> PlayerMotionList { get; set; } = new Dictionary<int, string>();

        //The path of the game
        public string GamePath { get; set; } = "";

        //Game identifier to load in the tool
        public GameVersion Version { get; set; }

        public GameConfig() { }

        public GameConfig(GameVersion ver)
        {
            Version = ver;
        }

        public void SaveConfig()
        {
            if (!Directory.Exists(GameList.ConfigDirectory)) Directory.CreateDirectory(GameList.ConfigDirectory);

            string path = Path.Combine(GameList.ConfigDirectory, $"{Version}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public class PlayerInfo //for player viewer selection
    {
        public string Name { get; set; }

        public string FileName { get; set; }

        public PlayerInfo() { }

        public PlayerInfo(string file, string name)
        {
            Name = name;
            FileName = file;
        }
    }

    public class PlayerMotInfo //for player viewer selection
    {
        public int Index;
        public string Name { get; set; }
    }

    public class BoardInfo
    {
        public string Name { get; set; }

        public string FileName { get; set; }

        public BoardInfo() { }

        public BoardInfo(string file, string name)
        {
            Name = name;
            FileName = file;
        }
    }
}
