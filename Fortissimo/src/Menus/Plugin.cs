using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SongDataIO;

namespace Fortissimo
{
    /// <summary>
    /// 
    /// </summary>
    public interface Plugin
    {
        String Name { get; }

        Player CreatePlayer(Game game);
        Menu GetPluginMenu(Game game, Menu oldMenu, Menu newMenu);
    }

    public class NoPlugin : Plugin
    {
        public String Name { get { return "Default"; } }

        public Player CreatePlayer(Game game)
        {
            return new Player(game);
        }

        public Menu GetPluginMenu(Game game, Menu oldMenu, Menu newMenu)
        {
            return newMenu;
        }
    }

}