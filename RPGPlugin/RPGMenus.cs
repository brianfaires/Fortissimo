using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Fortissimo;

namespace RPGPlugin
{
       

    public class SkillsMenu : Menu
    {
        SpriteFont defaultFont;
        SpriteFont infoFont;
        SpriteFont mainFont;
        Dictionary<int, SkillInformation> skillInformation;

        public struct SkillInformation
        {
            public SkillInformation(String title) { Level = 0; CostToNext = 1; Explanation = ""; this.Title = title; Image = null; Available = false; }
            public int Level;
            public int CostToNext;
            public String Explanation;
            public String Title;
            public Texture2D Image;
            public bool Available;
        }

        public SkillsMenu(Game game, Menu menu)
            : base(game, menu)
        {
            ((RPGPlugin)RhythmGame.ActivePlugin).RPGChangeState(RPGPlugin.RPGState.None);
            skillInformation = new Dictionary<int, SkillInformation>();
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState, int childId, SkillInformation info)
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            bool owns = (info.Level <= 0);
            if (owns)
                newItem.FontColor = Color.Gray;
            newItem.ChildId = childId;
            newItem.ItemText = text;
            if (owns)
                newItem.NextState = nextState;
            else
                newItem.NextState = RhythmGame.GameStateType.None;
            //newItem.DriftRateX = -2.0f;
            //newItem.DriftRateY = -0.25f;
            //newItem.ScaleRate = 0.9f;
            newItem.DefaultX = 120;
            newItem.StartY = 175;
            MenuItem.ScreenInfo screenInfo = newItem.CurrentInfo;
            //info.pos.Y -= 220;
            //info.pos.X += 30;
            newItem.CurrentInfo = screenInfo;
            newItem.LargeFont = true;
            MenuItem inside = (MenuItem)newItem;
            if (parent != null)
                if (!parent.AddLink(ref inside, key))
                    parent = null;

            if (parent == null)
            {
                newItem.ItemSetup();
                Selected = newItem;
            }

            skillInformation.Add(childId, info);

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            newItem.PreSelected += new Action<RhythmGame.GameStateType>(BuySkill);
            //newItem.PreSelected += new Action<RhythmGame.GameStateType>(LeavingMenu);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            List<Player> players = ((RhythmGame)Game).CurrentBand.BandMembers;
            foreach (RPGPlayer player in players)
            {
                Dictionary<string, Skill> skills = player.SkillManager.Skills;
                Dictionary<string, Skill> allSkills = player.SkillManager.AllSkills;
                int i = 0;
                MenuItem parent = null;
                foreach (KeyValuePair<string, Skill> pair in allSkills)
                {
                    bool owned = skills.ContainsKey(pair.Key);
                    SkillInformation info = new SkillInformation(pair.Key);
                    info.Explanation = "";
                    if ( player.SkillManager.SkillTextures.ContainsKey(pair.Key) )
                        info.Image = player.SkillManager.SkillTextures[pair.Key];
                    info.CostToNext = allSkills[pair.Key].Cost;
                    if (owned)
                        info.Level = skills[pair.Key].Level;
                    else
                        info.Level = 0;
                    parent = AddMenuItem(parent, OtherKeyType.Down, pair.Key, RhythmGame.GameStateType.None, i, info);
                    i++;
                }
            }
            MenuHelper.ConnectEdges(Selected);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/Background");
            defaultFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
            infoFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            mainFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont"); ;
        }

        void BuySkill(RhythmGame.GameStateType state)
        {
            List<Player> players = ((RhythmGame)Game).CurrentBand.BandMembers;
            foreach (RPGPlayer player in players)
            {
                SkillInformation info = skillInformation[Selected.ChildId];
                if (player.XpManager.SkillPoints >= info.CostToNext)
                {
                    // Consider it bought.
                    string name = skillInformation[Selected.ChildId].Title;

                    Dictionary<string, Skill> skills = player.SkillManager.Skills;
                    Dictionary<string, Skill> allSkills = player.SkillManager.AllSkills;
                    if (!skills.ContainsKey(name))
                        player.SkillManager.gainSkill(name);
                    if (allSkills[name].AdvanceSkillLevel())
                    {
                        info.Level = allSkills[name].Level;
                        skillInformation[Selected.ChildId] = info;
                        player.XpManager.SkillPoints -= info.CostToNext;
                    }
                }
                else
                {
                    // Play the failed sound.
                }
            }
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (defaultFont != null)
            {
                spriteBatch.DrawString(defaultFont, "Skills", new Vector2(250, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
            }

            Texture2D texture = skillInformation[Selected.ChildId].Image;
            if (texture != null)
            {
                Color color = Color.White;
                int level = skillInformation[Selected.ChildId].Level;
                if (level <= 0)
                    color = Color.Gray;

                color.A = 200;
                spriteBatch.Draw(texture, new Vector2(150, 50), color);

                if (mainFont != null)
                {
                    spriteBatch.DrawString(mainFont, "Skill Level:   " + level, new Vector2(220, 420), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(mainFont, "Skill Level:   " + level, new Vector2(227, 427), Color.LightSkyBlue, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                }
            }

            if (infoFont != null)
            {
                List<Player> players = ((RhythmGame)Game).CurrentBand.BandMembers;
                foreach (RPGPlayer player in players)
                {
                    spriteBatch.DrawString(infoFont, "Level:   " + player.XpManager.Level, new Vector2(625, 525), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(infoFont, "Level:   " + player.XpManager.Level, new Vector2(627, 527), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(infoFont, "Skill Points:   " + player.XpManager.SkillPoints, new Vector2(50, 525), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(infoFont, "Skill Points:   " + player.XpManager.SkillPoints, new Vector2(52, 527), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                }
            }
        }
    }

    public class TasksMenu : Menu
    {
        SpriteFont defaultFont;
        SpriteFont infoFont;
        SpriteFont mainFont;
        Dictionary<int, TaskInformation> taskInformation;
        public struct TaskInformation
        {
            public TaskInformation(String name) { this.Name = name; this.Description = ""; Image = null; IsComplete = false; Depends = ""; }
            public bool IsComplete;
            public String Name;
            public String Description;
            public string Depends;
            public Texture2D Image;
        }

        public TasksMenu(Game game, Menu menu) : base(game, menu)
        {
            ((RPGPlugin)RhythmGame.ActivePlugin).RPGChangeState(RPGPlugin.RPGState.None);
            taskInformation = new Dictionary<int, TaskInformation>();
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState, int childId, TaskInformation info)
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            newItem.ChildId = childId;
            newItem.ItemText = text;
            newItem.NextState = RhythmGame.GameStateType.None;
            //newItem.DriftRateX = -2.0f;
            //newItem.DriftRateY = -0.25f;
            //newItem.ScaleRate = 0.9f;
            newItem.DefaultX = 120;
            newItem.StartY = 175;
            MenuItem.ScreenInfo screenInfo = newItem.CurrentInfo;
            //info.pos.Y -= 220;
            //info.pos.X += 30;
            newItem.CurrentInfo = screenInfo;
            newItem.LargeFont = true;
            MenuItem inside = (MenuItem)newItem;
            if (parent != null)
                if (!parent.AddLink(ref inside, key))
                    parent = null;

            if (parent == null)
            {
                newItem.ItemSetup();
                Selected = newItem;
            }

            taskInformation.Add(childId, info);

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);

            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            List<Player> players = ((RhythmGame)Game).CurrentBand.BandMembers;
            
            foreach (RPGPlayer player in players)
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();
                int j = 0;
                while (player.TaskManager.NextTask() && j <= player.TaskManager.TaskCount())
                {
                    if(!tasks.ContainsKey(player.TaskManager.CurrentTaskName)){
                        tasks.Add(player.TaskManager.CurrentTaskName, player.TaskManager.CurrentTask);
                    }
                    j++;
                }

                int i = 0;
                MenuItem parent = null;
                foreach (KeyValuePair<string, Task> pair in tasks)
                {
                    TaskInformation info = new TaskInformation(pair.Key);
                    info.Description = pair.Value.GetDescription();
                    info.Depends = pair.Value.Depends;
                    if (pair.Value.IsComplete)
                        info.Image = player.TaskManager.CompletedTexture;
                    else
                        info.Image = player.TaskManager.IncompleteTexture;

                    parent = AddMenuItem(parent, OtherKeyType.Down, pair.Key, RhythmGame.GameStateType.None, i, info);
                    i++;
                }
            }
            MenuHelper.ConnectEdges(Selected);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/Background");
            defaultFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
            infoFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            mainFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont"); ;
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Console.WriteLine("Draw Foreground");
            base.DrawForeground(gameTime, spriteBatch);
            if (defaultFont != null)
            {
                spriteBatch.DrawString(defaultFont, "Tasks", new Vector2(250, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
            }

            Texture2D texture = taskInformation[Selected.ChildId].Image;
            if (texture != null)
            {
                string status = "Incomplete";
                Color color = Color.White;
                spriteBatch.Draw(texture, new Vector2(150, 50), color);

                if (mainFont != null)
                {
                    spriteBatch.DrawString(infoFont, "Description:   " + taskInformation[Selected.ChildId].Description, new Vector2(151, 276), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(infoFont, "Description:   " + taskInformation[Selected.ChildId].Description, new Vector2(150, 275), Color.LightSkyBlue, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    if (taskInformation[Selected.ChildId].IsComplete)
                    {
                        status = "Complete";
                    }

                    spriteBatch.DrawString(infoFont, "Prerequisite:   " + taskInformation[Selected.ChildId].Depends, new Vector2(176, 301), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(infoFont, "Status:   " + status, new Vector2(151, 326), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(infoFont, "Prerequisite:   " + taskInformation[Selected.ChildId].Depends, new Vector2(175, 300), Color.LightSkyBlue, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(infoFont, "Status:   " + status, new Vector2(150, 325), Color.LightSkyBlue, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                }
            }
        }
    }

    public class RPGSongSelect : SongSelect
    {
        SpriteFont textFont;
        public RPGSongSelect(Game game, Menu menu)
            : base(game, menu)
        {
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            textFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            InputManager[] inputs = ((RhythmGame)Game).InputDevices;

            if (((RPGPlugin)RhythmGame.ActivePlugin).RpgState == RPGPlugin.RPGState.SkillMenu || ((RPGPlugin)RhythmGame.ActivePlugin).RpgState == RPGPlugin.RPGState.TaskMenu)
                return;

            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].KeyPressed(2))
                {
                    ((RPGPlugin)RhythmGame.ActivePlugin).RPGChangeState(RPGPlugin.RPGState.SkillMenu);
                    // Once to save it out, its the same state so it will return without doing anything else
                    ((RhythmGame)Game).ChangeState(RhythmGame.GameStateType.SongSelect);
                    // Clear the state and actually do the change, what a hack!
                    ((RhythmGame)Game).State = RhythmGame.GameStateType.None;
                    ((RhythmGame)Game).ChangeState(RhythmGame.GameStateType.SongSelect);
                }
                else if (inputs[i].KeyPressed(3))
                {
                    ((RPGPlugin)RhythmGame.ActivePlugin).RPGChangeState(RPGPlugin.RPGState.TaskMenu);
                    // Once to save it out, its the same state so it will return without doing anything else
                    ((RhythmGame)Game).ChangeState(RhythmGame.GameStateType.SongSelect);
                    // Clear the state and actually do the change, what a hack!
                    ((RhythmGame)Game).State = RhythmGame.GameStateType.None;
                    ((RhythmGame)Game).ChangeState(RhythmGame.GameStateType.SongSelect);
                }


            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            spriteBatch.Begin();

            spriteBatch.DrawString(textFont, "Yellow - Skills", new Vector2(50, 550), Color.Black);
            spriteBatch.DrawString(textFont, "Yellow - Skills", new Vector2(52, 551), Color.Yellow);
            spriteBatch.DrawString(textFont, "Blue - Tasks", new Vector2(630, 550), Color.Black);
            spriteBatch.DrawString(textFont, "Blue - Tasks", new Vector2(632, 551), Color.Yellow); 

            spriteBatch.End();
        }

        public override void Initialize()
        {
            base.Initialize();
        } 
    }
                

    public class RGBCreditsScreen : Menu
    {
        SpriteFont titleFont;
        Texture2D[] coders;

        public RGBCreditsScreen(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(MenuItem parent, String name, String details, Texture2D logo, OtherKeyType key)
        {
            PhotoTextItem newItem = new PhotoTextItem(Game, this);

            MenuItem.ScreenInfo info = newItem.CurrentInfo;
            info.pos.Y -= 110;
            info.pos.X -= 275;
            newItem.CurrentInfo = info;
            newItem.SetStringAndLogo(name, logo);
            newItem.SetDetails(details);

            newItem.NextState = RhythmGame.GameStateType.None;
            MenuItem inside = (MenuItem)newItem;
            if (parent != null)
                if (!parent.AddLink(ref inside, key))
                    parent = null;

            if (parent == null)
            {
                newItem.ItemSetup();
                Selected = newItem;
            }

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!isInitialized)
            {
                MenuItem tmp3 = AddMenuItem(null, "Brian Faires", "Fortissimo :\nFileIO, Song Generation, XNA 4.0 Porting!", coders[1], OtherKeyType.Left);
                AddMenuItem(Selected, "Derrick Birkes", "Fortissimo :\n3D Development, Special Effects", coders[2], OtherKeyType.Right);

                MenuItem tmp  = AddMenuItem(Selected, "Nate Crandall", "RGB Plugin :\nGameplay", coders[4], OtherKeyType.Left);
                MenuItem tmp1 = AddMenuItem(tmp, "Seamus Connor", "RGB Plugin :\nGameplay", coders[6], OtherKeyType.Left);
                MenuItem tmp2 = AddMenuItem(tmp1, "Jonathon Muir", "RGB Plugin :\nGameplay", coders[5], OtherKeyType.Left);
                AddMenuItem(tmp2, "Bradley C. Grimm", "Fortissimo & RGB Plugin :\nGameplay, Menus, Graphics & Art, Audio", coders[0], OtherKeyType.EndType);
                AddMenuItem(tmp3, "Jaden He", "Fortissimo :\nIntroductory Movie, Data collection", coders[3], OtherKeyType.Left);

                MenuHelper.ConnectEdges(Selected);
            }
            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/VanBackground");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");

            coders = new Texture2D[7];
            coders[0] = Game.Content.Load<Texture2D>("Credits/Brad");
            coders[1] = Game.Content.Load<Texture2D>("Credits/Brian");
            coders[2] = Game.Content.Load<Texture2D>("Credits/Derrick");
            coders[3] = Game.Content.Load<Texture2D>("Credits/Jaden");
            coders[4] = Game.Content.Load<Texture2D>("Credits/nate");
            coders[5] = Game.Content.Load<Texture2D>("Credits/jon");
            coders[6] = Game.Content.Load<Texture2D>("Credits/seamus");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            spriteBatch.DrawString(titleFont, "Credits", new Vector2(200, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
        }

    }


    public class RGBSongSuccess : Menu
    {
        Texture2D incomplete;
        Texture2D complete;

        SpriteFont titleFont;
        SpriteFont menuFont;
        SpriteFont smallFont;

        bool tasks = false;
        bool dispImage = false;
        bool drawTaskComplete = false;

        public RGBSongSuccess(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(RhythmGame.GameStateType nextState)
        {
            InvisibleMenuItem newItem = new InvisibleMenuItem(Game, this);
            Items.Add(newItem);
            newItem.NextState = nextState;
            newItem.PreSelected += new Action<RhythmGame.GameStateType>(LeavingMenu);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            Selected = AddMenuItem(RhythmGame.GameStateType.None);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/Background");
            menuFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
            smallFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");

            incomplete = Game.Content.Load<Texture2D>("incomplete");
            complete = Game.Content.Load<Texture2D>("complete");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Song Passed!", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

            Band band = ((RhythmGame)Game).CurrentBand;

            if (menuFont != null)
            {
                if(!tasks)
                {
                    double score = band.CurrentScore;
                    int hit = band.HitNotes;
                    int missed = band.MissedNotes;
                    int total = hit + missed;
                    double percent = ((double)hit / (double)total) * 100.0;
                    String percentComplete;
                    if (percent >= 100.0)
                        percentComplete = "Percent hit: 100%";
                    else if (percent >= 10.0)
                        percentComplete = "Percent hit: " + percent.ToString("00") + "%";
                    else
                        percentComplete = "Percent hit: " + percent.ToString("0") + "%";
                    spriteBatch.DrawString(menuFont, "Score: " + score, new Vector2(118, 123), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, "Score: " + score, new Vector2(120, 125), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(menuFont, "Notes hit: " + hit + "/" + total, new Vector2(118, 223), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, "Notes hit: " + hit + "/" + total, new Vector2(120, 225), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(menuFont, percentComplete, new Vector2(118, 323), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, percentComplete, new Vector2(120, 325), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                }
                else
                {

                    foreach (Player p in band.BandMembers)
                    {
                        drawTaskComplete = ((RPGPlayer)p).TaskManager.DrawTaskDisplayInfo(smallFont, spriteBatch, 175, 123);
                    }

                    if(dispImage)
                    {
                        Rectangle imageRect = new Rectangle(0, 0, complete.Width, complete.Height);
                        if (drawTaskComplete)
                        {
                            spriteBatch.Draw(complete, imageRect, Color.White);
                        } else
                        {
                            spriteBatch.Draw(incomplete, imageRect, Color.White);
                        }
                    }
                    
                }
                
            }

        }

        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            if (tasks == false)
            {
                tasks = true;
            }
            else
            {
                if (!dispImage)
                {
                    dispImage = true;
                } 
                else
                {
                    ((RhythmGame)Game).CancelMenu();
                }
            }
        }
    }

    public class RGBSongFail : Menu
    {
        Texture2D incomplete;
        Texture2D complete;

        SpriteFont titleFont;
        SpriteFont menuFont;
        SpriteFont smallFont;
        bool tasks = false;
        bool dispImage = false;
        bool drawTaskComplete = false;

        public RGBSongFail(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(RhythmGame.GameStateType nextState)
        {
            InvisibleMenuItem newItem = new InvisibleMenuItem(Game, this);
            Items.Add(newItem);
            newItem.NextState = nextState;
            newItem.PreSelected += new Action<RhythmGame.GameStateType>(LeavingMenu);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            Selected = AddMenuItem(RhythmGame.GameStateType.None);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/Background");
            menuFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
            smallFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");

            incomplete = Game.Content.Load<Texture2D>("incomplete");
            complete = Game.Content.Load<Texture2D>("complete");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Song Failed!", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

            Band band = ((RhythmGame)Game).CurrentBand;

            if (menuFont != null)
            {
                if (!tasks)
                {
                    IPlayerService playerService =
                        (IPlayerService)Game.Services.GetService(typeof(IPlayerService));
                    NoteManager notes = playerService.Notes;

                    double score = band.CurrentScore;
                    int hit = notes.NoteIndex + 1;
                    int total = notes.Length;
                    double percent = ((double)hit / (double)total) * 100.0;
                    String percentComplete;
                    if (percent >= 100.0)
                        percentComplete = "Percent complete: 100%";
                    else if (percent >= 10.0)
                        percentComplete = "Percent complete: " + percent.ToString("00") + "%";
                    else
                        percentComplete = "Percent complete: " + percent.ToString("0") + "%";


                    spriteBatch.DrawString(menuFont, "Score: " + score, new Vector2(118, 123), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, "Score: " + score, new Vector2(120, 125), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(menuFont, "Notes hit: " + hit + "/" + total, new Vector2(118, 223), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, "Notes hit: " + hit + "/" + total, new Vector2(120, 225), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                    spriteBatch.DrawString(menuFont, percentComplete, new Vector2(118, 323), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                    spriteBatch.DrawString(menuFont, percentComplete, new Vector2(120, 325), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                }
                else
                {

                    foreach (Player p in band.BandMembers)
                    {
                        drawTaskComplete = ((RPGPlayer)p).TaskManager.DrawTaskDisplayInfo(smallFont, spriteBatch, 175, 123);
                    }

                    if (dispImage)
                    {
                        Rectangle imageRect = new Rectangle(0, 0, complete.Width, complete.Height);
                        if (drawTaskComplete)
                        {
                            spriteBatch.Draw(complete, imageRect, Color.White);
                        } else
                        {
                            spriteBatch.Draw(incomplete, imageRect, Color.White);
                        }
                    }
                }
            }

        }

        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            if (tasks == false)
            {
                tasks = true;
            } 
            else
            {
                if (!dispImage)
                {
                    dispImage = true;
                } else
                {
                    ((RhythmGame)Game).CancelMenu();
                }
            }
        }
    }


    public class RPGTitleScreen : Menu, ISetlistProvider
    {
        public RPGTitleScreen(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState)
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            newItem.LargeFont = true;
            newItem.ItemText = text;
            newItem.NextState = nextState;
            MenuItem inside = (MenuItem)newItem;
            if (parent != null)
                if (!parent.AddLink(ref inside, key))
                    parent = null;

            if (parent == null)
            {
                newItem.ItemSetup();
                Selected = newItem;
            }

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!isInitialized)
            {
                AddMenuItem(null, OtherKeyType.EndType, "Career Mode", RhythmGame.GameStateType.BandSelect);
                MenuItem Career = AddMenuItem(Selected, OtherKeyType.Down, "Free Play", RhythmGame.GameStateType.SongSelect);
                AddMenuItem(Career, OtherKeyType.Down, "Credits", RhythmGame.GameStateType.Credits);
                MenuHelper.ConnectEdges(Selected);
            }

            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/RPGTitleScreen");
        }

        public Setlist GetSetlist()
        {
            return SongSearcher.SearchAllLocations();
        }
    }
}