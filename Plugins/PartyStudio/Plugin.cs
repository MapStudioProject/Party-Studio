using System;
using Toolbox.Core;
using MapStudio.UI;

namespace PartyStudio
{
    public class Plugin : IPlugin
    {
        public string Name => "Party Studio";

        public static string GamePath = @"";

        public Plugin()
        {
        }

        public void DrawUI()
        {

        }
    }
}