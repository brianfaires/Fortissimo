using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fortissimo;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RPGPlugin
{
    public class RPGPlugin : Microsoft.Xna.Framework.DrawableGameComponent, Plugin
    {
        public String Name { get { return "RPG"; } }
        public enum RPGState { None, SkillMenu, TaskMenu, EndType }

        SpriteFont defaultFont;
        Game BaseGame;
        RPGState rpgState;
        public RPGState RpgState { get { return rpgState; } }
        RPGState oldRpgState;

        public RPGPlugin(Game game) : base(game)
        {
            BaseGame = game;
            rpgState = RPGState.None;
        }

        public Player CreatePlayer(Game game)
        {
            return new RPGPlayer(game);
        }

        protected override void LoadContent() 
        {
            base.LoadContent();
            defaultFont = BaseGame.Content.Load<SpriteFont>("DefaultFont");
        }
        
        public override void Update(GameTime gameTime) 
        {
            base.Update(gameTime);

            if (((RhythmGame)Game).InputDevices[0].OtherKeyPressed(OtherKeyType.Left) && ((RhythmGame)Game).State == RhythmGame.GameStateType.SongSelect)
            {
                List<Player> players = ((RhythmGame)Game).CurrentBand.BandMembers;
                foreach (RPGPlayer player in players)
                {
                    player.XpManager.addExperience(100000000);
                }
            }
        }

        public Menu GetPluginMenu(Game game, Menu oldMenu, Menu newMenu)
        {
            //if (newMenu is SongSelect)
            //    return new SkillsMenu(game, oldMenu);

            if (newMenu is CreditsScreen)
            {
                return new RGBCreditsScreen(game, oldMenu);
            }
            else if (newMenu is TitleScreen)
            {
                return new RPGTitleScreen(game, oldMenu);
            }
            else if (newMenu is SongSelect)
            {
                if (rpgState == RPGState.SkillMenu)
                {
                    return new SkillsMenu(game, oldMenu);
                }
                else if (rpgState == RPGState.TaskMenu)
                {
                    return new TasksMenu(game, oldMenu);
                }
                else
                {
                    return new RPGSongSelect(game, oldMenu);
                }
            }
            else if (newMenu is SongFail)
            {
                return new RGBSongFail(game, oldMenu);

                // I can never win them, so I'll just switch the menus for testing...
                //return new RGBSongSuccess(game, oldMenu);
            }
            else if (newMenu is SongSuccess)
            {
                return new RGBSongSuccess(game, oldMenu);
            }
            else if (newMenu is TitleScreen)
            {
                return new RPGTitleScreen(game, oldMenu);
            }


            else
            {
                return newMenu;
            }
           
        }

        public void RPGChangeState(RPGState state)
        {
            if (rpgState == state)
                return;

            RPGState oldState = oldRpgState;
            oldRpgState = rpgState;
            rpgState = state;
        }

        public override void Draw(GameTime gameTime) 
        {
            base.Draw(gameTime);
            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)BaseGame.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            spriteBatch.Begin();
            spriteBatch.DrawString(defaultFont, "Rpg Plugin Enabled", new Vector2(620, 570), Color.Black);
            spriteBatch.End();
        }
    }
}
