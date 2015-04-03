using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using AiM.Plugins;

namespace AiM
{
    public class PluginLoader
    {
        private static bool loaded;
        public PluginLoader()
        {
            if (!loaded)
            {
                switch(ObjectHandler.Player.ChampionName)
                {
                    default:
                        new Default();
                        break;
                }
            }
        }
    }
}
