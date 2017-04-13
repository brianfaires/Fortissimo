#region Using Statements
using System;
using System.Collections;
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
using FortissimoMath;
using SongDataIO;
#endregion


namespace Fortissimo
{
    public enum MenuState { Waiting, Transitioning };

    public struct StateBackup 
    {
        public Menu menu;
        public GameComponentCollection components;
        public RhythmGame.GameStateType state;
    }

    #region Interfaces
    public interface ISetlistProvider
    {
        Setlist GetSetlist();
    }

    public interface IBandProvider
    {
        List<Band> GetBandList();
    }

    public interface IOneLineProvider
    {
        String GetLine();
    }
    #endregion

    #region Menu Template
    public class Menu : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public Action<int, int> Shift;

        protected Texture2D background;
        protected Texture2D foreground;
        protected MenuItem Selected;
        protected bool isInitialized = false;

        protected ArrayList Items;

        Random rand;
        protected ColorRange foregroundRange;
        bool InterpUp;

        SoundEffect selectSound = null;
        SoundEffect cancelSound = null;
        SoundEffect navigateSound = null;

        public Menu(Game game, Menu menu)
            : base(game)
        {
            Items = new ArrayList();
            rand = new Random();
            foregroundRange = new ColorRange(Color.Yellow, Color.White);
            foregroundRange.CurrentInterp = 1.0;
            InterpUp = false;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            foreground = null;
            background = null;

            selectSound = Game.Content.Load<SoundEffect>("Sound/CLICK10A");
            cancelSound = Game.Content.Load<SoundEffect>("Sound/button26");
            navigateSound = Game.Content.Load<SoundEffect>("Sound/button8");

            SetupMenuItems();
        }

        public virtual void SetupMenuItems()
        {
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            KeyboardState keyboardState = Keyboard.GetState();
            Keys[] newKeys = (Keys[])keyboardState.GetPressedKeys();

            InputManager[] inputs = ((RhythmGame)Game).InputDevices;
            
            for (int i = 0; i < inputs.Length; i++)
            {
                for (OtherKeyType okt = OtherKeyType.Up; okt < OtherKeyType.EndType; okt++)
                {
                    if (inputs[i].OtherKeyPressed(okt))
                    {
                        if (okt == OtherKeyType.Cancel)
                        {
                            if (cancelSound != null)
                                cancelSound.Play();
                            ((RhythmGame)Game).CancelMenu();
                            break;
                        }

                        if (Selected == null)
                            continue;
                        if (Selected.FollowLink(okt, Items.Count))
                        {
                            //if (Selected.NextState != RhythmGame.GameStateType.None)
                            {
                                if (navigateSound != null)
                                    navigateSound.Play();
                                Selected = Selected.LinkToFollow;
                                break;
                            }
                        }
                        else
                        {
                            if (selectSound != null)
                                selectSound.Play();
                        }
                    }
                }
            }
            
            // Interpolate color, to give a shimmer/glow
            if (rand.NextDouble() < 0.03)
                InterpUp = !InterpUp;
            if (InterpUp)
                foregroundRange.CurrentInterp += gameTime.ElapsedGameTime.TotalMilliseconds / 4000.0; 
            else
                foregroundRange.CurrentInterp -= gameTime.ElapsedGameTime.TotalMilliseconds / 4000.0;

            foregroundRange.CurrentInterp = Math.Min(
                Math.Max(foregroundRange.CurrentInterp, 0.0), 1.0);
        }

        /// <summary>
        /// Draw background, then children, then foreground.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // 4.0change

            if ( background != null )
                spriteBatch.Draw(background, Vector2.Zero, Color.White);
            base.Draw(gameTime);
            if ( foreground != null )
                spriteBatch.Draw(foreground, Vector2.Zero, foregroundRange.InterpColor());
            DrawForeground(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public virtual void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch) { }
    }

    public class MenuHelper
    {
        public static void ConnectEdges(MenuItem Selected)
        {
            if (Selected is HorizontalMenuItem)
            {
                HorizontalMenuItem sel = (HorizontalMenuItem)Selected;
                HorizontalMenuItem leftmost = sel, rightmost = sel;
                while (leftmost.Left != null)
                {
                    if (!(leftmost.Left is HorizontalMenuItem))
                        return;
                    leftmost = (HorizontalMenuItem)leftmost.Left;
                }

                while (rightmost.Right != null)
                {
                    if (!(rightmost.Right is HorizontalMenuItem))
                        return;
                    rightmost = (HorizontalMenuItem)rightmost.Right;
                }

                leftmost.Left = rightmost;
                rightmost.Right = leftmost;
            }
            if (Selected is VerticalMenuItem)
            {
                VerticalMenuItem sel = (VerticalMenuItem)Selected;
                VerticalMenuItem top = sel, bottom = sel;
                while (top.Up != null)
                {
                    if (!(top.Up is VerticalMenuItem))
                        return;
                    top = (VerticalMenuItem)top.Up;
                }

                while (bottom.Down != null)
                {
                    if (!(bottom.Down is VerticalMenuItem))
                        return;
                    bottom = (VerticalMenuItem)bottom.Down;
                }

                top.Up = bottom;
                bottom.Down = top;
            }
        }
    }
    #endregion

    #region Menu Instances
    public class TitleScreen : Menu, ISetlistProvider
    {
        public TitleScreen(Game game, Menu menu)
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
                MenuItem Credits = AddMenuItem(Career, OtherKeyType.Down, "Credits", RhythmGame.GameStateType.Credits);
                AddMenuItem(Credits, OtherKeyType.Down, "Quit Game", RhythmGame.GameStateType.ExitGame);
                MenuHelper.ConnectEdges(Selected);
            }

            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if ( background == null )
                background = Game.Content.Load<Texture2D>("Menu/TitleScreen");
        }

        public Setlist GetSetlist()
        {
            return SongSearcher.SearchAllLocations();
        }
    }

    public class BandSelect : Menu, IBandProvider
    {
        SpriteFont titleFont;

        public BandSelect(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public List<Band> GetBandList()
        {
            if (Selected is IBandProvider)
                return ((IBandProvider)Selected).GetBandList();

            return null;
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, RhythmGame.GameStateType nextState)
        {
            BandlistItem newItem = new BandlistItem(Game, this);

            MenuItem.ScreenInfo info = newItem.CurrentInfo;
            info.pos.Y -= 150;
            info.pos.X -= 275;
            newItem.CurrentInfo = info;

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
                List<Band> bandList = BandSearcher.SearchDefaultLocation();
                MenuItem leftEnd = null, rightEnd = null;
                OtherKeyType okt = OtherKeyType.EndType;

                for (int k = 0; k < bandList.Count; k++)
                {
                    MenuItem potentialParent;
                    if (okt == OtherKeyType.Right || okt == OtherKeyType.EndType)
                    {
                        potentialParent = AddMenuItem(rightEnd, okt, RhythmGame.GameStateType.MembersSelect);
                        rightEnd = potentialParent;
                        if (okt == OtherKeyType.EndType)
                            leftEnd = potentialParent;
                        okt = OtherKeyType.Left;
                    }
                    else
                    {
                        potentialParent = AddMenuItem(leftEnd, okt, RhythmGame.GameStateType.MembersSelect);
                        leftEnd = potentialParent;
                        okt = OtherKeyType.Right;
                    }

                    ((BandlistItem)potentialParent).AddBandData(bandList[k]);
                }

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
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            base.DrawForeground(gameTime, spriteBatch);
            spriteBatch.DrawString(titleFont, "Band Select", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
        }

    }

    public class CareerSelect : Menu, ISetlistProvider
    {
        SpriteFont titleFont;

        public CareerSelect(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public Setlist GetSetlist()
        {
            if (Selected is ISetlistProvider)
                return ((ISetlistProvider)Selected).GetSetlist();

            return null;
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, RhythmGame.GameStateType nextState)
        {
            SetlistItem newItem = new SetlistItem(Game, this);
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
                List<Setlist> listOfLists = SongSearcher.SearchDefaultLocation();
                MenuItem leftEnd = null, rightEnd = null;
                OtherKeyType okt = OtherKeyType.EndType;

                for (int i = 0; i < listOfLists.Count; i++)
                {
                    Setlist setlist = listOfLists[i];

                    MenuItem potentialParent;
                    if (okt == OtherKeyType.Right || okt == OtherKeyType.EndType)
                    {
                        potentialParent = AddMenuItem(rightEnd, okt, RhythmGame.GameStateType.SongSelect);
                        rightEnd = potentialParent;
                        if (okt == OtherKeyType.EndType)
                            leftEnd = potentialParent;
                        okt = OtherKeyType.Left;
                    }
                    else
                    {
                        potentialParent = AddMenuItem(leftEnd, okt, RhythmGame.GameStateType.SongSelect);
                        leftEnd = potentialParent;
                        okt = OtherKeyType.Right;
                    }

                    ((SetlistItem)potentialParent).Name = setlist.Name; ;
                    List<SongDataPlus> songList = setlist.Songs;
                    for (int k = 0; k < songList.Count; k++)
                    {
                        ((SetlistItem)potentialParent).AddSongData(songList[k]);
                    }
                }

                MenuHelper.ConnectEdges(Selected);
            }
            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if ( background == null )
                background = Game.Content.Load<Texture2D>("Menu/Background");
            //foreground = Game.Content.Load<Texture2D>("Menu/Foreground");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            spriteBatch.DrawString(titleFont, "Career Select", new Vector2(120, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
        }
    }

    public class SongSelect : Menu
    {
        protected SpriteFont titleFont;
        Setlist _setlist;
        List<uint> _stars;

        Texture2D starTexture;

        public SongSelect(Game game, Menu menu)
            : base(game, menu)
        {
            if (menu is ISetlistProvider)
                _setlist = ((ISetlistProvider)menu).GetSetlist();
            else
                _setlist = new Setlist(new List<SongDataPlus>(), "");
            _stars = new List<uint>();
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState, int childId)
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            newItem.InvertControls = true;
            newItem.ItemText = text;
            newItem.NextState = nextState;
            newItem.DriftRateX = 0.0f;
            newItem.DriftRateY = -0.25f;
            newItem.ScaleRate = 0.9f;
            newItem.DefaultX = 75;
            newItem.StartY = 75;
            MenuItem.ScreenInfo info = newItem.CurrentInfo;
            info.pos.Y -= 220;
            info.pos.X += 30;
            newItem.CurrentInfo = info;
            MenuItem inside = (MenuItem)newItem;
            if (parent != null)
                if (!parent.AddLink(ref inside, key))
                    parent = null;

            if (parent == null)
            {
                newItem.ItemSetup();
                Selected = newItem;
            }

            newItem.ChildId = Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            newItem.PreSelected += new Action<RhythmGame.GameStateType>(LeavingMenu);
            return newItem;
        }

        String GetDetailsAsString(SongDataPlus dataPlus)
        {
            Band band = ((RhythmGame)Game).CurrentBand;
            double score = band.GetSongScore(dataPlus.songData.info.name);
            return dataPlus.songData.info.name + "        " + dataPlus.songData.info.artist + "              Score: " + score;
        }

        public override void Initialize()
        {
            base.Initialize();
            
            Band band = ((RhythmGame)Game).CurrentBand;

            List<SongDataPlus> songList = _setlist.Songs;
            if (isInitialized)
            {
                ((VerticalTextItem)Selected).ItemText = GetDetailsAsString(songList[Selected.ChildId]);
                _stars[Selected.ChildId] = band.GetSongStars(songList[Selected.ChildId].songData.info.name);
            }

            if (!isInitialized)
            {
                MenuItem parent = null;
                for (int i = 0; i < songList.Count; i++)
                {
                    _stars.Add(band.GetSongStars(songList[i].songData.info.name));
                    parent = AddMenuItem(parent, OtherKeyType.Down, GetDetailsAsString(songList[i]), RhythmGame.GameStateType.LoadSong, i);
                }

                MenuHelper.ConnectEdges(Selected);
                Shift += new Action<int, int>(ChangeSong);
            }

            if (Selected != null)
            {
                int index = Selected.ChildId;
                ((RhythmGame)Game).StopAllSongs();
                if (((RhythmGame)Game).QueueSong(_setlist.Songs[index]))
                {
                    ((RhythmGame)Game).PlayQueuedSong();
                }
            }

            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if ( background == null )
                background = Game.Content.Load<Texture2D>("Menu/Background");
            //foreground = Game.Content.Load<Texture2D>("Menu/Foreground");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
            starTexture = Game.Content.Load<Texture2D>("Skins/Guitar/RedStar");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            spriteBatch.DrawString(titleFont, "Pick a song", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);


            // Check star levels...
            if (_stars.Count > 0)
            {
                for (int i = 0; i < _stars[Selected.ChildId]; i++)
                {
                    // Are we good enough...?
                    Vector2 starPos = new Vector2(50 + i * 150, 430);
                    Color slightlyClear = Color.White;

                    spriteBatch.Draw(starTexture, starPos, null, Color.White, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 1.0F);
                }
            }
        }

        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            int index = Selected.ChildId;
            ((RhythmGame)Game).EnqueueSong(_setlist.Songs[index]);
        }

        SongDataPlus currentSong;
        bool loadMusic = false;
        TimeSpan waitTime;
        public void ChangeSong(int dir, int circular)
        {
            int index = Selected.LinkToFollow.ChildId;
            loadMusic = true;
            waitTime = TimeSpan.FromSeconds(1);
            currentSong = _setlist.Songs[index];
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (loadMusic)
            {
                if (waitTime > TimeSpan.Zero)
                    waitTime -= gameTime.ElapsedGameTime;
                else
                {
                    ((RhythmGame)Game).StopAllSongs();
                    if (((RhythmGame)Game).QueueSong(currentSong))
                    {
                        ((RhythmGame)Game).PlayQueuedSong();
                    }
                    loadMusic = false;
                }
            }
        }
    }

    public class BuySongWarning : Menu
    {
        SpriteFont titleFont;
        SpriteFont menuFont;

        public BuySongWarning(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(RhythmGame.GameStateType nextState)
        {
            InvisibleMenuItem newItem = new InvisibleMenuItem(Game, this);
            Items.Add(newItem);
            newItem.NextState = nextState;
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();
            Selected = AddMenuItem(RhythmGame.GameStateType.DifficultySelect);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/Background");
            menuFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Music Missing", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
            if (menuFont != null)
            {
                spriteBatch.DrawString(menuFont, "We couldn't find the music for song.\n    If you don't own the song we encourage you to go purchase it.\n    If you do own the song read below on how to insert it in the game.\n\nYou may continue to play without music.", new Vector2(50, 150), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                spriteBatch.DrawString(menuFont, ((RhythmGame)Game).ExplainMusicPlayers(), new Vector2(50, 300), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
            }
        }
    }

    public class PauseOverlay : Menu
    {
        SpriteFont titleFont;
        SpriteFont menuFont;

        public PauseOverlay(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState)
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            newItem.ItemText = text;
            newItem.NextState = nextState;
            //newItem.DriftRateX = -2.0f;
            //newItem.DriftRateY = -0.25f;
            //newItem.ScaleRate = 0.9f;
            newItem.DefaultX = 250;
            newItem.StartY = 275;
            MenuItem.ScreenInfo info = newItem.CurrentInfo;
            //info.pos.Y -= 220;
            //info.pos.X += 30;
            newItem.CurrentInfo = info;
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

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();

            Selected = AddMenuItem(null, OtherKeyType.EndType, "Continue song", RhythmGame.GameStateType.Running);
            MenuItem restartSong = AddMenuItem(Selected, OtherKeyType.Down, "Restart song", RhythmGame.GameStateType.SongRestart);
            MenuItem quitSong = AddMenuItem(restartSong, OtherKeyType.Down, "Quit song", RhythmGame.GameStateType.SongCancel);
            MenuHelper.ConnectEdges(Selected);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/PauseScreen");
            menuFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Paused", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
        }
    }

    public class DifficultySelect : Menu
    {
        SpriteFont titleFont;
        SpriteFont menuFont;

        public DifficultySelect(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public MenuItem AddMenuItem(MenuItem parent, OtherKeyType key, String text, RhythmGame.GameStateType nextState, bool validDifficulty, int childId )
        {
            VerticalTextItem newItem = new VerticalTextItem(Game, this);
            if (!validDifficulty)
                newItem.FontColor = Color.Gray;
            newItem.ChildId = childId;
            newItem.ItemText = text;
            if (validDifficulty)
                newItem.NextState = nextState;
            else
                newItem.NextState = RhythmGame.GameStateType.None;
            //newItem.DriftRateX = -2.0f;
            //newItem.DriftRateY = -0.25f;
            //newItem.ScaleRate = 0.9f;
            newItem.DefaultX = 250;
            newItem.StartY = 275;
            MenuItem.ScreenInfo info = newItem.CurrentInfo;
            //info.pos.Y -= 220;
            //info.pos.X += 30;
            newItem.CurrentInfo = info;
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

            Items.Add(newItem);
            Shift += new Action<int, int>(newItem.Shift);
            newItem.PreSelected += new Action<RhythmGame.GameStateType>(LeavingMenu);
            return newItem;
        }

        public override void Initialize()
        {
            base.Initialize();

            SongDataPlus songDataPlus = ((RhythmGame)Game).CurrentSong;

            bool[] validDifficulty = new bool[4];
            // TODO :Hacky way of getting around it.  Won't work for multiplayer.
            if (songDataPlus.songData.instruments != null)
            {
                for (int i = 0; i < songDataPlus.songData.instruments.Length; i++)
                {
                    if (songDataPlus.songData.instruments[i].instrumentType.Equals("LGT"))
                    {
                        SongDataIO.SongData.DifficultySet[] diffSet = songDataPlus.songData.instruments[i].diffSets;
                        validDifficulty = new bool[diffSet.Length];
                        for (int difficulty = 0; difficulty < diffSet.Length; difficulty++)
                        {
                            if (diffSet[difficulty].phrases[0].notes.Length > 0)
                                validDifficulty[difficulty] = true;
                        }
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < validDifficulty.Length; i++)
                    validDifficulty[i] = true;
            }

            if ( validDifficulty[0] )
                Selected = AddMenuItem(null, OtherKeyType.EndType, "Easy", RhythmGame.GameStateType.Running, validDifficulty[0], 0);
            MenuItem medium = Selected;
            if ( validDifficulty[1] )
                medium = AddMenuItem(Selected, OtherKeyType.Down, "Medium", RhythmGame.GameStateType.Running, validDifficulty[1], 1);
            MenuItem hard = medium;
            if ( validDifficulty[2] )
                hard = AddMenuItem(medium, OtherKeyType.Down, "Hard", RhythmGame.GameStateType.Running, validDifficulty[2], 2);
            MenuItem expert;
            if ( validDifficulty[3] )
                expert = AddMenuItem(hard, OtherKeyType.Down, "Expert", RhythmGame.GameStateType.Running, validDifficulty[3], 3);
            MenuHelper.ConnectEdges(Selected);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/DifficultySelect");
            menuFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Difficulty", new Vector2(200, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
        }


        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            int difficulty = Selected.ChildId;
            ((RhythmGame)Game).Difficulty = difficulty;
        }
    }

    public class SongSuccess : Menu
    {
        SpriteFont titleFont;
        SpriteFont menuFont;

        public SongSuccess(Game game, Menu menu)
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
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Song Passed!", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

            if (menuFont != null)
            {
                Band band = ((RhythmGame)Game).CurrentBand;
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

        }

        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            ((RhythmGame)Game).CancelMenu();
        }
    }

    public class SongFail : Menu
    {
        SpriteFont titleFont;
        SpriteFont menuFont;

        public SongFail(Game game, Menu menu)
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
        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            if (titleFont != null)
                spriteBatch.DrawString(titleFont, "Song Failed!", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

            if (menuFont != null)
            {
                Band band = ((RhythmGame)Game).CurrentBand;
                
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

        }

        public void LeavingMenu(RhythmGame.GameStateType type)
        {
            ((RhythmGame)Game).CancelMenu();
        }
    }

    public class CreditsScreen : Menu
    {
        SpriteFont titleFont;
        Texture2D[] coders;

        public CreditsScreen(Game game, Menu menu)
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
                // Yes, I selfishly moved myself to the front of this menu here. :P
                AddMenuItem(null, "Bradley C. Grimm", "Gameplay, Menus, Graphics & Art, Audio", coders[0], OtherKeyType.EndType);
                MenuItem tmp = AddMenuItem(Selected, "Derrick Birkes", "3D Development, Special Effects, Documentation", coders[2], OtherKeyType.Right);
                AddMenuItem(tmp, "Jaden He", "Introductory Movie, Data collection", coders[3], OtherKeyType.Right);
                tmp = AddMenuItem(Selected, "Brian Faires", "XNA 4.0 Porting!!, File IO, Note generation, Gameplay", coders[1], OtherKeyType.Left);

                base.Shift(1, 4);
                MenuHelper.ConnectEdges(tmp);
            }
            isInitialized = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (background == null)
                background = Game.Content.Load<Texture2D>("Menu/VanBackground");
            titleFont = Game.Content.Load<SpriteFont>("Menu/TitleFont");

            coders = new Texture2D[4];
            coders[0] = Game.Content.Load<Texture2D>("Credits/Brad");
            coders[1] = Game.Content.Load<Texture2D>("Credits/Brian");
            coders[2] = Game.Content.Load<Texture2D>("Credits/Derrick");
            coders[3] = Game.Content.Load<Texture2D>("Credits/Jaden");

        }

        public override void DrawForeground(GameTime gameTime, SpriteBatch spriteBatch)
        {
            base.DrawForeground(gameTime, spriteBatch);
            spriteBatch.DrawString(titleFont, "Credits", new Vector2(150, 25), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F); // 4.0change; 200->150 text position
        }

    }
    #endregion

    #region Menu Item Template
    public class MenuItem : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public enum ShiftType { Circular, StartAtZero }
        public struct ScreenInfo
        {
            public Vector2 pos;
            public float scale;
        }

        #region Textures & Graphics Information
        protected ScreenInfo oldInfo;
        protected ScreenInfo currentInfo;
        protected ScreenInfo goalInfo;
        #endregion

        public int Index { get; set; }

        protected TimeSpan CurTransTime;
        protected TimeSpan TransitionTime;
        protected TimeSpan TimeUntilSelect;

        protected ShiftType _shiftType;

        ColorRange FadeOut = new ColorRange(new Color(255,255,255,0), Color.White);

        protected Action<RhythmGame.GameStateType> Selected;
        protected Action<RhythmGame.GameStateType> _preSelected;
        protected Menu menu;
        protected MenuItem _linkToFollow;
        protected int _childId;

        protected RhythmGame.GameStateType _nextState = RhythmGame.GameStateType.None;

        #region Properties
        public MenuItem LinkToFollow
        {
            get { return _linkToFollow; }
            set { _linkToFollow = value; }
        }
        public RhythmGame.GameStateType NextState 
        {
            get { return _nextState; }
            set { _nextState = value; }
        }
        public Action<RhythmGame.GameStateType> PreSelected
        {
            get { return _preSelected; }
            set { _preSelected = value; }
        }
        public int ChildId
        {
            get { return _childId; }
            set { _childId = value; }
        }

        public ScreenInfo CurrentInfo
        {
            get { return currentInfo; }
            set { currentInfo = value; }
        }
        #endregion

        public MenuItem(Game game, Menu menu)
            : base(game)
        {
            oldInfo = new ScreenInfo();
            currentInfo = new ScreenInfo();
            goalInfo = new ScreenInfo();

            currentInfo.pos = new Vector2(300, 100);

            TransitionTime = TimeSpan.FromMilliseconds(500);
            CurTransTime = TransitionTime;

            Selected += new Action<RhythmGame.GameStateType>(((RhythmGame)Game).ChangeState);
            Game.Components.Add(this);
            DrawOrder = 3;

            _childId = 0;

            this.menu = menu;
        }

        public virtual void ItemSetup()
        {
            DrawOrder = 2;
            ScreenPosition(Index);
        }

        public virtual bool AddLink(ref MenuItem item, OtherKeyType key)
        {
            return false;
        }

        public virtual bool FollowLink(OtherKeyType key, int circular)
        {
            if (key == OtherKeyType.Select)
            {
                if ( PreSelected != null )
                    PreSelected(_nextState);
                Selected(_nextState);
            }
            return false;
        }

        /// <summary>
        /// All menus are just a list of times.  This goes through and 
        /// shifts this index's position to where it should be on screen.
        /// </summary>
        public virtual void Shift(int dir, int circular)
        {
            int nextIdx = Index + dir;
            if (circular != -1)
            {
                int low = 0, high = circular - 1;
                if (_shiftType == ShiftType.Circular)
                {
                    low = 0 - circular / 2;
                    high = circular + low - 1;
                }
                if (nextIdx < low)
                    nextIdx = high + (nextIdx - low + 1);
                if (nextIdx > high)
                    nextIdx = low + (nextIdx - high - 1);
            }
            ScreenPosition(nextIdx);
        }

        /// <summary>
        /// Overridden with position and scale information for menu items.
        /// </summary>
        public virtual void ScreenPosition(int idx)
        {
            //goalInfo.scale = Overridden Scale;
            //oldInfo = currentInfo;
            //CurTransTime = TransitionTime;
        }

        /// <summary>
        /// Automatically interpolates scale and position.  Can be overridden
        /// if a different type of interpolation is desired.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            CurTransTime -= gameTime.ElapsedGameTime;
            if (CurTransTime < TimeSpan.Zero)
                CurTransTime = TimeSpan.Zero;

            float percent = 1.0F - (float)(CurTransTime.TotalMilliseconds / TransitionTime.TotalMilliseconds);
            //percent = percent * percent;

            currentInfo.scale = oldInfo.scale + (goalInfo.scale - oldInfo.scale) * percent;
            currentInfo.pos = oldInfo.pos + (goalInfo.pos - oldInfo.pos) * percent;
        }
    }

    public class HorizontalMenuItem : MenuItem
    {
        MenuItem left, right;
        bool _invertControls = true;
        public bool InvertControls { set { _invertControls = value; } }

        #region Properties
        public MenuItem Left
        {
            get { return left; }
            set { left = value; }
        }

        public MenuItem Right
        {
            get { return right; }
            set { right = value; }
        }
        #endregion


        public HorizontalMenuItem(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public override bool AddLink(ref MenuItem item, OtherKeyType key)
        {
            HorizontalMenuItem parent = this;
            HorizontalMenuItem child;
            if (item is HorizontalMenuItem)
                child = (HorizontalMenuItem)item;
            else
                child = null;

            switch (key)
            {
                case OtherKeyType.Left:
                    parent.Right = item;
                    if (child != null)
                        child.Left = parent;
                    item.Index = parent.Index - 1;
                    break;
                case OtherKeyType.Right:
                    parent.Left = item;
                    if (child != null)
                        child.Right = parent;
                    item.Index = parent.Index + 1;
                    break;
                default:
                    return false;
            }
            item.ItemSetup();
            return true;
        }

        public override bool FollowLink(OtherKeyType key, int circular)
        {
            if (base.FollowLink(key, circular))
                return true;

            if (_invertControls)
            {
                if (key == OtherKeyType.Left)
                    key = OtherKeyType.Right;
                else if ( key == OtherKeyType.Right )
                    key = OtherKeyType.Left;
            }
            switch (key)
            {
                case OtherKeyType.Left:
                    _linkToFollow = Left;
                    menu.Shift(-1, circular);
                    return (_linkToFollow != null);
                case OtherKeyType.Right:
                    _linkToFollow = Right;
                    menu.Shift(1, circular);
                    return (_linkToFollow != null);
            }
            return false;
        }
    }
    
    public class VerticalMenuItem : MenuItem
    {
        MenuItem up, down;

        bool _invertControls = false;
        public bool InvertControls { set { _invertControls = value; } }

        #region Properties
        public MenuItem Up
        {
            get { return up; }
            set { up = value; }
        }

        public MenuItem Down
        {
            get { return down; }
            set { down = value; }
        }
        #endregion

        public VerticalMenuItem(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public override bool AddLink(ref MenuItem item, OtherKeyType key)
        {
            VerticalMenuItem parent = this;
            VerticalMenuItem child;
            if (item is VerticalMenuItem)
                child = (VerticalMenuItem)item;
            else
                child = null;

            switch (key)
            {
                case OtherKeyType.Up:
                    parent.Up = item;
                    if (child != null)
                        child.Down = parent;
                    item.Index = parent.Index - 1;
                    break;
                case OtherKeyType.Down:
                    parent.Up = item;
                    if (child != null)
                        child.Down = parent;
                    item.Index = parent.Index + 1;
                    break;
                default:
                    return false;
            }
            item.ItemSetup();
            return true;
        }

        public override bool FollowLink(OtherKeyType key, int circular)
        {
            if (base.FollowLink(key, circular))
                return true;

            if (_invertControls)
            {
                if (key == OtherKeyType.Up)
                    key = OtherKeyType.Down;
                else if (key == OtherKeyType.Down)
                    key = OtherKeyType.Up;
            }
            switch (key)
            {
                case OtherKeyType.Up:
                    _linkToFollow = Up;
                    menu.Shift(-1, circular);
                    return (_linkToFollow != null);
                case OtherKeyType.Down:
                    _linkToFollow = Down;
                    menu.Shift(1, circular);
                    return (_linkToFollow != null);
            }
            return false;
        }
    }

    public class InvisibleMenuItem : MenuItem
    {
        public InvisibleMenuItem(Game game, Menu menu)
            : base(game, menu)
        {
        }
    }
    #endregion

    #region Item Instances
    public class VerticalTextItem : VerticalMenuItem
    {
        SpriteFont spriteFont = null;

        Color _fontColor = Color.LightYellow;
        public Color FontColor { set { _fontColor = value; } }
        String _itemText;
        bool _largeFont = false;
        int _defaultX = 350;
        int _startY = 470;
        int _drawRangePositive = 12;
        int _drawRangeNegative = -3;

        float _driftRateX = 1.0f, _driftRateY = 1.0f;
        float _scaleRate = 0.5f;

        #region Properties
        public bool LargeFont
        {
            get { return _largeFont; }
            set { _largeFont = value; if (spriteFont != null) LoadContent(); }
        }
        public String ItemText
        {
            get { return _itemText; }
            set { _itemText = value; }
        }
        public int DefaultX
        {
            get { return _defaultX; }
            set { _defaultX = value; }
        }
        public int StartY
        {
            get { return _startY; }
            set { _startY = value; }
        }
        public float DriftRateX
        {
            get { return _driftRateX; }
            set { _driftRateX = value; }
        }
        public float DriftRateY
        {
            get { return _driftRateY; }
            set { _driftRateY = value; }
        }
        public float ScaleRate
        {
            get { return _scaleRate; }
            set { _scaleRate = value; }
        }
        #endregion

        public VerticalTextItem(Game game, Menu menu)
            : base(game, menu)
        {
            currentInfo.pos = new Vector2(350, 470);
            _itemText = "Default Item text";

            this.menu = menu;

            _shiftType = MenuItem.ShiftType.StartAtZero;
        }

        #region Graphics
        public override void ScreenPosition(int idx)
        {
            Index = idx;

            float Dir = (idx == 0) ? 0.0F : ((idx > 0) ? 1.0F : -1.0F);
            float scale = 1.0F;
            goalInfo.pos = new Vector2(_defaultX, StartY);

            int absi = Math.Abs(idx);
            for (int i = 0; i < absi; i++)
            {
                float midPoint = 350 * scale * _scaleRate;
                //goalInfo.pos.X += midPoint;

                scale *= _scaleRate;

                goalInfo.pos.X -= Dir * scale * 70 * DriftRateX;
                goalInfo.pos.Y -= 600 * scale * 0.5F * DriftRateY;

                midPoint = 350 * scale * _scaleRate;
                //goalInfo.pos.X -= midPoint;
            }
            goalInfo.scale = scale;
            oldInfo = currentInfo;
            CurTransTime = TransitionTime;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            if (LargeFont)
                spriteFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
            else
                spriteFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (Index > _drawRangePositive || Index < _drawRangeNegative)
                return;

            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            Color textColor;
            if (Index == 0)
                textColor = Color.Yellow;
            else
                textColor = _fontColor;
            Color highlight = textColor;
            highlight.R += 150;
            highlight.G += 150;
            highlight.B += 150;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // 4.0change
            spriteBatch.DrawString(spriteFont, _itemText, currentInfo.pos, Color.Black, 0.0f, Vector2.Zero, currentInfo.scale, SpriteEffects.None, 1.0f);
            Vector2 shifted = currentInfo.pos;
            shifted.X += 10;
            shifted.Y += 10;
            spriteBatch.DrawString(spriteFont, _itemText, shifted, highlight, 0.0f, Vector2.Zero, currentInfo.scale, SpriteEffects.None, 1.0f);
            shifted.X -= 1;
            shifted.Y -= 1;
            spriteBatch.DrawString(spriteFont, _itemText, shifted, textColor, 0.0f, Vector2.Zero, currentInfo.scale, SpriteEffects.None, 1.0f);
            spriteBatch.End();
        }
        #endregion 
    }

    public class CrackableMenuItem : HorizontalMenuItem
    {
        protected enum MenuItemState { Waiting, Cracking, Breaking, Growing, Done }
        protected MenuItemState state;

        protected int _defaultX = 240;
        protected int StartY = 100;

        protected TimeSpan _breakTime = TimeSpan.FromMilliseconds(200);
        protected bool _earlyFade = false;

        #region Textures & Graphics Information
        protected Texture2D background = null;
        protected Texture2D foreground = null;
        protected Texture2D flame = null;

        Texture2D crackedFull;
        protected Texture2D[] cracked = null;
        protected Texture2D[] crackedFront = null;
        protected Texture2D[] shards = null;
        Vector2[] shardPos;
        Vector2[] crackedPos;
        Vector2[] shardDir;
        float[] crackedScale;

        protected SpriteFont font;
        #endregion

        Random r = new Random();
        ColorRange FadeOut = new ColorRange(new Color(255, 255, 255, 0), Color.White);

        bool _doCrack = true;

        public CrackableMenuItem(Game game, Menu menu)
            : base(game, menu)
        {
            currentInfo.pos = new Vector2(200, 100);
            this.menu = menu;
        }

        #region Graphics Functions
        public override void ScreenPosition(int idx)
        {
            Index = idx;

            float Dir = (idx == 0) ? 0.0F : ((idx > 0) ? 1.0F : -1.0F);
            float scale = 1.0F;
            goalInfo.pos = new Vector2();
            goalInfo.pos = new Vector2(_defaultX, StartY);

            int absi = Math.Abs(idx);
            for (int i = 0; i < absi; i++)
            {
                float midPoint = background.Width * scale / 2;
                goalInfo.pos.X += midPoint;

                scale /= 2;

                goalInfo.pos.X += Dir * scale * 400;
                goalInfo.pos.Y += background.Height * scale * 0.25F;

                midPoint = background.Width * scale / 2;
                goalInfo.pos.X -= midPoint;
            }
            goalInfo.scale = scale;
            oldInfo = currentInfo;
            CurTransTime = TransitionTime;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            background = Game.Content.Load<Texture2D>("Menu/MenuItem");
            foreground = Game.Content.Load<Texture2D>("Menu/MenuItemFront");
            font = Game.Content.Load<SpriteFont>("Menu/MenuFont");
            crackedFull = Game.Content.Load<Texture2D>("Menu/MenuItemCracked");

            cracked = new Texture2D[2];
            cracked[0] = Game.Content.Load<Texture2D>("Menu/CrackLeft");
            cracked[1] = Game.Content.Load<Texture2D>("Menu/CrackRight");
            crackedFront = new Texture2D[2];
            crackedFront[0] = Game.Content.Load<Texture2D>("Menu/CrackLeftFront");
            crackedFront[1] = Game.Content.Load<Texture2D>("Menu/CrackRightFront");

            shards = new Texture2D[5];
            for (int i = 0; i < 5; i++)
                shards[i] = Game.Content.Load<Texture2D>("Menu/Shard" + (i + 1));

            flame = Game.Content.Load<Texture2D>("Menu/flame");

            ScreenPosition(Index);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // 4.0change

            Vector2 scale = new Vector2( currentInfo.scale );
            Color color = FadeOut.InterpColor(scale.Y * scale.Y);
            float timeLeft = 1.0f;

            bool drawCrack = false, drawFlame = false, drawForeground = false;
            Vector2 pos = currentInfo.pos;
            switch (state)
            {
                case MenuItemState.Waiting:
                    if ( background != null )
                        spriteBatch.Draw(background, pos, null, color, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                    drawForeground = true;
                    break;
                case MenuItemState.Breaking:
                    if ( _earlyFade )
                        timeLeft = (float)TimeUntilSelect.TotalMilliseconds / 1500.0F;
                    if ( crackedFull != null )
                        spriteBatch.Draw(crackedFull, pos, null, color, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                    drawForeground = true;

                    TimeUntilSelect -= gameTime.ElapsedGameTime;
                    if (TimeUntilSelect < TimeSpan.Zero)
                    {
                        TimeUntilSelect = TimeSpan.FromMilliseconds(700);
                        state = MenuItemState.Cracking;
                        crackedPos = new Vector2[2];
                        crackedPos[0] = pos;
                        crackedPos[1] = pos;
                        crackedScale = new float[2];
                        crackedScale[0] = scale.Y;
                        crackedScale[1] = scale.Y;
                    }
                    break;
                case MenuItemState.Cracking:
                    timeLeft = (float)TimeUntilSelect.TotalMilliseconds / 1500.0F;

                    float balloon = 2.15F;
                    crackedPos[0].X -= (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 160.0F * balloon;
                    crackedPos[1].X += (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 155.0F * balloon;
                    crackedPos[0].Y -= (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 65.0F * balloon;
                    crackedPos[1].Y -= (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 65.0F * balloon;
                    crackedScale[0] += (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 0.18F * balloon;
                    crackedScale[1] += (gameTime.ElapsedGameTime.Milliseconds / 1000.0F) * 0.18F * balloon;
                    drawCrack = true;
                    drawFlame = true;

                    Color c = color;
                    c.A = (byte)(255 * timeLeft);
                    for (int i = 0; i < 5; i++)
                    {
                        shardPos[i].X += (float)(shardDir[i].X * 600.0 * gameTime.ElapsedGameTime.Milliseconds / 1000.0);
                        shardPos[i].Y += (float)(shardDir[i].Y * 600.0 * gameTime.ElapsedGameTime.Milliseconds / 1000.0);
                        if (shards != null)
                            spriteBatch.Draw(shards[i], shardPos[i], null, c, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                    }

                    TimeUntilSelect -= gameTime.ElapsedGameTime;
                    if (TimeUntilSelect < TimeSpan.Zero)
                    {
                        state = MenuItemState.Growing;
                        TimeUntilSelect = TimeSpan.FromMilliseconds(250);
                    }
                    break;
                case MenuItemState.Growing:
                    drawCrack = true;
                    drawFlame = true;
                    timeLeft = 0.0f;

                    TimeUntilSelect -= gameTime.ElapsedGameTime;
                    if (TimeUntilSelect < TimeSpan.Zero)
                    {
                        state = MenuItemState.Done;
                    }
                    break;
                case MenuItemState.Done:
                    timeLeft = 0.0f;
                    drawCrack = true;
                    drawFlame = true;
                    break;
            }

            if (drawCrack && cracked != null)
            {
                spriteBatch.Draw(cracked[0], crackedPos[0], null, color, 0.0F, Vector2.Zero, crackedScale[0], SpriteEffects.None, 1);
                spriteBatch.Draw(cracked[1], crackedPos[1], null, color, 0.0F, Vector2.Zero, crackedScale[1], SpriteEffects.None, 1);
            }

            if (drawFlame && flame != null)
            {
                Rectangle flameDest = new Rectangle();
                flameDest.X += 140 + (int)crackedPos[0].X;
                flameDest.Y += 20 + (int)crackedPos[0].Y;
                flameDest.Width = (int)crackedPos[1].X - (int)crackedPos[0].X + 70;
                flameDest.Height = 10;

                Color c = Color.White;
                c.A = (byte)(120 - flameDest.Y);

                spriteBatch.Draw(flame, flameDest, null, c, 0.0F, Vector2.Zero, SpriteEffects.None, 1);

                flameDest.Y += (int)(420 * crackedScale[0]);
                spriteBatch.Draw(flame, flameDest, null, c, 0.0F, Vector2.Zero, SpriteEffects.None, 1);
            }

            DrawData(timeLeft, scale, pos, color);

            if (drawCrack && crackedFront != null)
            {
                spriteBatch.Draw(crackedFront[0], crackedPos[0], null, color, 0.0F, Vector2.Zero, crackedScale[0], SpriteEffects.None, 1);
                spriteBatch.Draw(crackedFront[1], crackedPos[1], null, color, 0.0F, Vector2.Zero, crackedScale[1], SpriteEffects.None, 1);
            }

            if (drawForeground && foreground != null)
                spriteBatch.Draw(foreground, pos, null, color, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);

            spriteBatch.End();
        }

        public virtual void DrawData(float timeLeft, Vector2 scale, Vector2 pos, Color color)
        {
        }
        #endregion

        #region Update and Navigation
        public override bool FollowLink(OtherKeyType key, int circular)
        {
            if (state != MenuItemState.Waiting)
                return false;

            if (key != OtherKeyType.Select)
                return base.FollowLink(key, circular);

            if (NextState == RhythmGame.GameStateType.None)
                return false;

            TimeUntilSelect = _breakTime;
            if (_doCrack)
            {
                state = MenuItemState.Breaking;
                shardPos = new Vector2[5];
                shardDir = new Vector2[5];
                for (int i = 0; i < 5; i++)
                {
                    shardPos[i] = currentInfo.pos;
                    shardDir[i].X = (float)r.NextDouble() - 0.5F;
                    shardDir[i].Y = (float)r.NextDouble() - 0.5F;
                }
            }
            else
                state = MenuItemState.Done;
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            CurTransTime -= gameTime.ElapsedGameTime;
            if (CurTransTime < TimeSpan.Zero)
                CurTransTime = TimeSpan.Zero;

            float percent = 1.0F - (float)(CurTransTime.TotalMilliseconds / TransitionTime.TotalMilliseconds);
            //percent = percent * percent;

            currentInfo.scale = oldInfo.scale + (goalInfo.scale - oldInfo.scale) * percent;
            currentInfo.pos = oldInfo.pos + (goalInfo.pos - oldInfo.pos) * percent;

            if (state == MenuItemState.Done)
            {
                if (PreSelected != null)
                    PreSelected(_nextState);
                Selected(_nextState);
                state = MenuItemState.Waiting;
            }
        }
        #endregion
    }

    public class SetlistItem : CrackableMenuItem, ISetlistProvider
    {
        Setlist _setList = new Setlist(new List<SongDataPlus>(), "");
        public String Name { set { _setList.Name = value; } }
        SpriteFont _largeFont = null;

        public SetlistItem(Game game, Menu menu)
            : base(game, menu)
        {
        }

        public Setlist GetSetlist()
        {
            return _setList;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            _largeFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
        }

        public void AddSongData(SongDataPlus songInfo)
        {
            _setList.Songs.Add(songInfo);
        }

        public override void DrawData(float timeLeft, Vector2 scale, Vector2 pos, Color color)
        {
            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            Color invColor = Color.Black;
            Color titleColor = Color.Maroon;
            Color titleBack = Color.Black;
            invColor.A = (byte)(color.A * timeLeft);
            titleColor.A = (byte)(color.A * timeLeft * 0.5F);
            titleBack.A = (byte)(color.A * timeLeft * 0.3F);

            List<SongDataPlus> songList = _setList.Songs;
            for (int i = 0; i < songList.Count && i < 16; i++)
            {
                Vector2 textPos = Vector2.Multiply(new Vector2(60, 40 + i * 24), scale) + pos;
                SongDataPlus dataPlus = (SongDataPlus)songList[i];
                String truncatedName = dataPlus.songData.info.name;
                if ( truncatedName.Length > 17 )
                    truncatedName = truncatedName.Substring(0, 17);
                spriteBatch.DrawString(font, truncatedName, textPos, invColor, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
            }

            if (_largeFont != null)
            {
                Vector2 titlePos = Vector2.Multiply(new Vector2(120, 60), scale) + pos;
                String truncatedTitle = _setList.Name;
                if (truncatedTitle.Length > 17)
                    truncatedTitle = truncatedTitle.Substring(0, 17);

                //spriteBatch.DrawString(_largeFont, truncatedTitle, titlePos, titleBack, (float)Math.PI / 2, Vector2.Zero, scale, SpriteEffects.None, 1);
                //titlePos.X += 2;
                spriteBatch.DrawString(_largeFont, truncatedTitle, titlePos, titleColor, (float)Math.PI / 2, Vector2.Zero, scale, SpriteEffects.None, 1);
            }
        }
    }

    public class PhotoTextItem : CrackableMenuItem
    {
        protected String _name;
        protected String _details = "";
        protected Texture2D _logo;
        protected SpriteFont _smFont;

        public PhotoTextItem(Game game, Menu menu)
            : base(game, menu)
        {
            _name = "";
            _logo = null;

            _defaultX = 105;
            StartY = 50;
        }


        protected override void LoadContent()
        {
            background = Game.Content.Load<Texture2D>("Menu/BandCard");
            font = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
            _smFont = Game.Content.Load<SpriteFont>("Menu/MenuFont");
        }

        public void SetStringAndLogo(String name, Texture2D logo)
        {
            _name = name;
            _logo = logo;
        }

        public void SetDetails(String details)
        {
            _details = details;
        }

        public override void DrawData(float timeLeft, Vector2 scale, Vector2 pos, Color color)
        {
            if (state == MenuItemState.Waiting || state == MenuItemState.Breaking)
            {
                ISpriteBatchService spriteBatchService =
                    (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
                SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

                Color invColor = Color.White;
                invColor.A = (byte)(color.A * scale.X * scale.X * timeLeft);

                Vector2 imagePos = Vector2.Multiply(new Vector2(170, 30), scale) + pos;
                Vector2 textPos = Vector2.Multiply(new Vector2(70, 300), scale) + pos;
                Vector2 smallTextPos = Vector2.Multiply(new Vector2(140, 400), scale) + pos;
                spriteBatch.DrawString(font, _name, textPos, invColor, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                if (_logo != null)
                    spriteBatch.Draw(_logo, imagePos, null, invColor, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                spriteBatch.DrawString(_smFont, _details, smallTextPos, invColor, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    }

    public class BandlistItem : CrackableMenuItem, IBandProvider
    {
        List<Band> _bandList = new List<Band>();
        ExplosionParticleSystemHuge explosion;
        ExplosionParticleSystemHuge explosionSmaller;
        Random random = new Random();

        public BandlistItem(Game game, Menu menu)
            : base(game, menu)
        {
            Selected += new Action<RhythmGame.GameStateType>(SetCurrentBand);
            _defaultX = 105;
            StartY = 50;
            explosion = new ExplosionParticleSystemHuge(game, 100, false);
            explosionSmaller = new ExplosionParticleSystemHuge(game, 100, false);
            game.Components.Add(explosion);
            game.Components.Add(explosionSmaller);
            _breakTime = TimeSpan.FromMilliseconds(500);
            _earlyFade = true;
        }

        public List<Band> GetBandList()
        {
            return _bandList;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            background = Game.Content.Load<Texture2D>("Menu/BandCard");
            font = Game.Content.Load<SpriteFont>("Menu/MenuFont");
        }

        public void AddBandData(Band bandInfo)
        {
            _bandList.Add(bandInfo);
        }

        public override void DrawData(float timeLeft, Vector2 scale, Vector2 pos, Color color)
        {
            if (state == MenuItemState.Waiting || state == MenuItemState.Breaking)
            {
                ISpriteBatchService spriteBatchService =
                    (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
                SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

                Color invColor = Color.White;
                invColor.A = (byte)(color.A * scale.X * scale.X * timeLeft);
                for (int i = 0; i < _bandList.Count; i++)
                {
                    Vector2 imagePos = Vector2.Multiply(new Vector2(170, 30 + i * 24), scale) + pos;
                    Vector2 textPos = Vector2.Multiply(new Vector2(70, 350 + i * 24), scale) + pos;
                    Band bandInfo = (Band)_bandList[i];
                    spriteBatch.DrawString(font, bandInfo.BandName, textPos, invColor, 0.0F, Vector2.Zero, scale * 2, SpriteEffects.None, 1);
                    spriteBatch.Draw(bandInfo.BandLogo, imagePos, null, invColor, 0.0F, Vector2.Zero, scale, SpriteEffects.None, 1);
                }
            }
        }

        bool explosionDone = false;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (state == MenuItemState.Waiting )
                explosionDone = false;
            if (state != MenuItemState.Waiting && explosionDone == false)
            {
                explosionDone = true;
                explosionSmaller.AddParticles(new Vector2(360, 125), Color.Yellow);
                explosion.AddParticles(new Vector2(360, 300), Color.Red);
                explosion.AddParticles(new Vector2(360, 300), Color.DarkGray);
                explosionSmaller.AddParticles(new Vector2(360, 325), Color.Yellow);
            }
            if (state == MenuItemState.Cracking)
            {
                state = MenuItemState.Done;
            }
        }

        public void SetCurrentBand(RhythmGame.GameStateType type)
        {
            ((RhythmGame)Game).CurrentBand = _bandList[0];
        }
    }
    #endregion
}
