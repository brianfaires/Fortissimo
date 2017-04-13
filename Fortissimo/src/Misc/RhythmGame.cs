//#define DEMO_MODE

// Bugs/Enhancements
//      - Last note can't be hit?! How frustrating!
//      - HOPOs don't require an active streak
//      - Back to back identical hold chords don't fire the second one
//      - Silence guitar track after a hold is released
//      - Silence ONLY the guitar track (currently muting some bass/drum lines)


#region Using Statements
using System;
using System.IO;
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
using SongDataIO;
//using Scurvy.Media;
//using Scurvy.Media.Pipeline;
using SeeSharp.Xna.Video;
#endregion

namespace Fortissimo
{
    public interface ISpriteBatchService
    {
        SpriteBatch SpriteBatch { get; set; }
    }

    public class RhythmGame : Microsoft.Xna.Framework.Game, ISpriteBatchService
    {
        const bool DemoMode = true;
        //This needs to be defined in a menu, 0-4 (4==Super Expert, removes all filters)
        public int _difficulty = 3;
        public int Difficulty { get { return _difficulty; } set { _difficulty = value; } }

        GraphicsDeviceManager graphics;
        public static RhythmGame GameInstance;

        public SpriteBatch SpriteBatch { set; get; }
        public enum GameStateType { TitleScreen, BandSelect, MembersSelect, CareerSelect, SongSelect, DifficultySelect,
        LoadSong, Running, Paused, SongRestart, SongFail, SongSuccess, SongCancel, BuySong, Credits, IntroMovie, None, ExitGame
        };

        public InputManager[] InputDevices;
        public InputManager ActiveInput;

        private Band _currentBand = null;
        public Band CurrentBand 
        {
            get
            {
                if (_currentBand == null)
                    _currentBand = Band.GenerateRandomBand();
                return _currentBand;
            }
            set { _currentBand = value; }
        }

        public GameStateType State { get; set; }
        GameStateType lastState = GameStateType.None;
        bool stateChanged = false;

        List<MusicPlayer> availablePlayers;
        MusicPlayer musicPlayer;
        Menu menu;

        GameTime gameTime;

        Queue<SongDataPlus> songQueue;
        SongDataPlus _currentSong;
        SongDataPlus _backgroundSong;
        SongDataPlus _defaultSong;
        public SongDataPlus CurrentSong { get { return _currentSong; } }

        Stack<StateBackup> menuStack;
        public static Plugin ActivePlugin = null;
        
        TimeSpan pausedTime;
        TimeSpan _backgroundTimer;
        TimeSpan AUDIO_VIDEO_OFFSET = TimeSpan.FromMilliseconds(0);
        //VideoContentManager _videoContentManager;
        Microsoft.Xna.Framework.Media.VideoPlayer introVideo = null; // 4.0change
        
        public RhythmGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800; // btftest

            Content.RootDirectory = "Content";

            Services.AddService(typeof(ISpriteBatchService), this);

            InstrumentMaster.CreateSingleton();
            GameInstance = this;
            //this.IsFixedTimeStep = false;
            State = GameStateType.None;
            ActivePlugin = new NoPlugin();
            _backgroundTimer = TimeSpan.Zero;
            //_videoContentManager = new VideoContentManager(Services);

#if DEMO_MODE
            this.graphics.IsFullScreen = true;
#else
            this.graphics.IsFullScreen = false;
#endif

        }

        public void AddPlugin(Plugin plugin)
        {
            ActivePlugin = plugin;
            if (plugin is DrawableGameComponent)
            {
                DrawableGameComponent pluginComp = (DrawableGameComponent)plugin;
                pluginComp.UpdateOrder = 10;
                pluginComp.DrawOrder = 10;
                Components.Add(pluginComp);
            }
        }

        public void ResetComponents()
        {
            Components.Clear();
            for (int i = 0; i < InputDevices.Length; i++)
                Components.Add(InputDevices[i]);
            if (ActivePlugin is GameComponent)
                Components.Add((GameComponent)ActivePlugin);
        }

        protected override void Initialize()
        {
            songQueue = new Queue<SongDataPlus>();
            availablePlayers = new List<MusicPlayer>();
#if WINDOWS
            availablePlayers.Add(new OGGPlayer(this));
#endif
            availablePlayers.Add(new MP3Player(this));

#if !DEMO_MODE
            availablePlayers.Add(new MediaLibraryPlayer(this));
#endif
            menuStack = new Stack<StateBackup>();

            InputDevices = new InputManager[2];
            InputDevices[0] = new ASDFGInput(this);
            InputDevices[1] = new GuitarInput(this);
            InputDevices[0].UpdateOrder = 0;
            InputDevices[1].UpdateOrder = 0;
            Components.Add(InputDevices[0]);
            Components.Add(InputDevices[1]);

            ChangeState(GameStateType.TitleScreen);

            base.Initialize();

        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            base.LoadContent();

            _defaultSong = new SongDataPlus();
            _defaultSong.type = SongDataPlus.NoteType.GBA;
            _defaultSong.dirPath = "";// TITLE_LOCATION;
            _defaultSong.songData = new SongData();
            _defaultSong.songData.info.filename = "Default";

            String aviPath = Path.Combine(/*TITLE_LOCATION,*/ "Intro.avi");
            try
            {/*
                introVideo = new Microsoft.Xna.Framework.Media.VideoPlayer(aviPath, GraphicsDevice);
                introVideo.OnVideoComplete += new EventHandler(MovieFinished);
                introVideo.Play();
                ChangeState(GameStateType.IntroMovie);*/
                introVideo = null; // 4.0change
            }
            catch (Exception)
            {
                introVideo = null;
            }
        }

        protected override void UnloadContent()
        {
            if (introVideo != null)
                introVideo.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            this.gameTime = gameTime;

            switch (State)
            {
                case GameStateType.Paused:
                    if (stateChanged)
                    {
                        pausedTime = gameTime.TotalGameTime;
                    }
                    break;
                case GameStateType.Running:
                    if (stateChanged && lastState == GameStateType.Paused)
                    {
                        TimeSpan totalPausedTime = gameTime.TotalGameTime - pausedTime;
                        
                        foreach (GameComponent component in Components)
                        {
                            if (component is Player)
                                ((Player)component).Notes.SyncStartTime(totalPausedTime);
                        }
                    }
                    break;
                    
            }
            stateChanged = false;

            if (State == GameStateType.Running)
            {
                TimeSpan playPosition = musicPlayer.PlayPosition() + AUDIO_VIDEO_OFFSET;
                List<Player> members = CurrentBand.BandMembers;
                foreach (Player p in members)
                    p.Notes.VerifySongTime(playPosition);
            }

            if (IsBackgroundMusicState(State))
            {
                // Give the song a minute, then fade out and start a new one.
                if (_backgroundTimer < TimeSpan.FromSeconds(10))
                {
                    if (musicPlayer != null)
                        musicPlayer.FadeVolume(-(gameTime.ElapsedGameTime.Milliseconds / 10000.0));
                }

                if (_backgroundTimer < TimeSpan.Zero)
                {
                    _backgroundSong = SongSearcher.PickRandomSong();
                    StopAllSongs();
                    if (QueueSong(_backgroundSong))
                    {
                        PlayQueuedSong();
                    }
                    else
                    {
                        // Try default song.
                        if (QueueSong(_defaultSong))
                        {
                            PlayQueuedSong();
                        }
                    }
                    _backgroundTimer = TimeSpan.FromMinutes(1);
                }
                _backgroundTimer -= gameTime.ElapsedGameTime;
            }
            else
            {
                // Start it up as soon as we get to a valid state.
                _backgroundTimer = TimeSpan.Zero;
            }

            base.Update(gameTime);

            // Update after the game update to prevent the user from skipping the title screen.
            if (State == GameStateType.IntroMovie)
            {
                // Funky work around for video player.
                GraphicsDevice.Textures[0] = null;
                //introVideo.Update(); // 4.0change

                for (int i = 0; i < InputDevices.Length; i++)
                {
                    if (InputDevices[i].OtherKeyPressed(OtherKeyType.Select)
                            || InputDevices[i].OtherKeyPressed(OtherKeyType.Cancel)
                            || InputDevices[i].OtherKeyPressed(OtherKeyType.Select))
                    {
                        //introVideo.Stop(); // 4.0change
                        MovieFinished(null, null);
                    }
                }
            }
        }

        public void EnqueueSong(SongDataPlus song)
        {
            // 4.0change
            songQueue.Enqueue(song);
        }

        public void MovieFinished(object sender, EventArgs e)
        {
            ChangeState(GameStateType.TitleScreen);
            introVideo.Dispose();
            introVideo = null;
        }

        /// <summary>
        /// Searches all known song formats and determines if the music
        /// exists in any of the following formats.
        /// </summary>
        public bool QueueSong(SongDataPlus songToQueue)
        {
            if ( musicPlayer != null )
                musicPlayer.StopAll();
            _songPlaying = false;

            if (songToQueue.type == SongDataPlus.NoteType.None)
                return false;

            bool foundValidPlayer = false;
            for (int i = 0; i < availablePlayers.Count; i++)
            {
                musicPlayer = availablePlayers[i];

                foundValidPlayer = musicPlayer.LoadSong(songToQueue.dirPath, songToQueue.songData.info.filename);
                if (foundValidPlayer)
                    break;
            }

            if (!foundValidPlayer)
                return false;

            // Can possibly have 5 extra tracks playing...?
            // For now they must use the same player they used for the main song.
            for (int t = 1; t <= 5; t++)
            {
                musicPlayer.LoadTrack(songToQueue.dirPath, songToQueue.songData.info.filename + "_tr" + t);
            }

            return true;
        }

        bool _songPlaying = false;
        public void PlayQueuedSong()
        {
            if (musicPlayer != null)
            {
                musicPlayer.PlayAll();
                _songPlaying = true;
            }
        }

        /// <summary>
        /// Stops all songs.
        /// </summary>
        public void StopAllSongs()
        {
            for (int i = 0; i < availablePlayers.Count; i++)
            {
                if ( availablePlayers[i] != null )
                    availablePlayers[i].StopAll();
            }
        }

        public void CancelMenu()
        {
            if (menuStack.Count > 0)
            {
                ResetComponents();
                StateBackup backup = menuStack.Pop();
                menu = backup.menu;
                State = backup.state;
                for (int i = 0; i < backup.components.Count; i++)
                    Components.Add(backup.components[i]);
            }
        }

        public void StoreState()
        {
            StateBackup backup = new StateBackup();
            backup.menu = menu;
            backup.components = new GameComponentCollection();
            backup.state = State;
            foreach (GameComponent gc in Components)
            {
                if (!(gc is InputManager) && !(gc is Plugin))
                    backup.components.Add(gc);
            }
            menuStack.Push(backup);
        }

        public bool IsStorableState(GameStateType state, GameStateType nextState)
        {
            if (state == GameStateType.None || state == GameStateType.LoadSong
                || state == GameStateType.Paused || state == GameStateType.BuySong
                || state == GameStateType.DifficultySelect || state == GameStateType.IntroMovie)
                return false;
            if (state == GameStateType.Running)
            {
                if (nextState != GameStateType.Paused)
                    return false;
            }
            return true;
        }

        public bool IsBackgroundMusicState(GameStateType state)
        {
            if (state == GameStateType.None || state == GameStateType.LoadSong
                || state == GameStateType.Paused || state == GameStateType.BuySong
                || state == GameStateType.DifficultySelect || state == GameStateType.Running
                || state == GameStateType.IntroMovie || state == GameStateType.SongSelect)
                return false;
            return true;
        }

        public bool ShouldSaveBand(GameStateType state)
        {
            if (state == GameStateType.SongFail || state == GameStateType.SongSuccess || state == GameStateType.SongCancel)
                return true;
            return false;
        }

        public void PauseGame(bool pause)
        {
            foreach (GameComponent component in Components)
            {
                if (!(component is Player) && !(component is InputManager))
                    component.Enabled = !pause;
            }

            if ( _songPlaying )
            {
                musicPlayer.PauseAll(pause);
            }

            if (pause)
            {
                PauseOverlay pauseOverlay = new PauseOverlay(this, null);
                pauseOverlay.DrawOrder = 100;
                Components.Add(pauseOverlay);
            }
        }

        /// <summary>
        /// All state changes go through this function.
        /// </summary>
        public void ChangeState(GameStateType state)
        {
            // Transition to a NoneState?!  No way!
            if (state == GameStateType.None)
                return;

            // Store off old state.
            if (IsStorableState(State, state))
                StoreState();

            lastState = State;
            State = state;

            if (lastState != state)
                stateChanged = true;

            if (!stateChanged)
                return;

            if (!IsBackgroundMusicState(state) && IsBackgroundMusicState(lastState))
                StopAllSongs();

            // Pause is a unique state.
            if (state == GameStateType.Paused && lastState != GameStateType.Paused)
            {
                PauseGame(true);
                return;
            }

            if (lastState == GameStateType.Paused)
            {
                CancelMenu();
                if (state == GameStateType.Running)
                {
                    PauseGame(false);
                    return;
                }
            }

            if (state == GameStateType.SongRestart)
            {
                musicPlayer.StopAll();
                QueueSong(_currentSong);
                List<Player> members = CurrentBand.BandMembers;
                foreach (Player p in members)
                    p.Notes.Reset();
                ChangeState(GameStateType.Running);
                return;
            }

            // Clear out residue from last state.
            ResetComponents();


            // Temporarily skip band members
            if (State == GameStateType.MembersSelect)
                state = State = GameStateType.CareerSelect;

            if (ShouldSaveBand(state))
            {
                if (state == GameStateType.SongSuccess)
                {
                    Band tmp = CurrentBand;
                    _currentBand.ScoreSong(_currentSong.songData.info.name, _currentBand.CurrentScore, _currentBand.Stars);
                }
                Band.SaveBandToFile(_currentBand);
            }

            bool changeToMenu = false;
            Menu newMenu = null;
            // Initialize our next state.
            switch (state)
            {
                case GameStateType.TitleScreen:
                    newMenu = new TitleScreen(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.CareerSelect:
                    newMenu = new CareerSelect(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.MembersSelect:
                    changeToMenu = true;
                    break;
                case GameStateType.BandSelect:
                    newMenu = new BandSelect(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.SongSelect:
                    // Stupid hack to get a player readied for rpg stuffs
                    if (CurrentBand.BandMembers.Count == 0)
                    {
                        PlayerFactory.TestPlayer(null, this);
                    }

                    newMenu = new SongSelect(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.BuySong:
                    newMenu = new BuySongWarning(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.Credits:
                    newMenu = new CreditsScreen(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.DifficultySelect:
                    newMenu = new DifficultySelect(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.Running:
                    menu = null;
                    float realDifficulty = (Difficulty == 0 ? 0.2f : Difficulty == 1 ? 0.4f : Difficulty == 2 ? 0.6f : Difficulty == 3 ? 0.8f : 0.96f);
                    
                    foreach (Player p in CurrentBand.BandMembers)
                    {
                        p.Reset();
                        p.StartSong(this, gameTime, _currentSong, Player.GUITAR, realDifficulty);
                        p.ChangeState += new Action<RhythmGame.GameStateType>(ChangeState);

                        if (musicPlayer != null)
                        {
                            p.NoteWasMissed += new Action<SongData.NoteSet>(musicPlayer.MissedNote);
                            p.NoteWasHit += new Action<SongData.NoteSet>(musicPlayer.HitNote);
                        }
                    }
                    break;
                case GameStateType.LoadSong:
                    _currentSong = songQueue.Dequeue();

                    if (QueueSong(_currentSong))
                        ChangeState(GameStateType.DifficultySelect);
                    else
                        ChangeState(GameStateType.BuySong);
                    break;
                case GameStateType.SongFail:
                    newMenu = new SongFail(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.SongSuccess:
                    newMenu = new SongSuccess(this, menu);
                    changeToMenu = true;
                    break;
                case GameStateType.SongCancel:
                    StopAllSongs();
                    CancelMenu();
                    break;
            }

            if (changeToMenu)
            {
                // Give the plugin a chance to override the menu.
                newMenu = ActivePlugin.GetPluginMenu(this, menu, newMenu);

                menu = newMenu;
                menu.DrawOrder = 1;
                menu.Enabled = true;
                menu.Visible = true;
                Components.Add(menu);
            }
        }

        /// <summary>
        /// Clear background, draw self.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            if (State == GameStateType.ExitGame)
            {
                Exit();
            }
            else if (State == GameStateType.IntroMovie)
            {
                SpriteBatch.Begin();
                Texture2D videoFrame = null;// introVideo.OutputFrame; // 4.0change
                // Causing a bunch of flicker and tearing.
                //Viewport viewport = GraphicsDevice.Viewport;
                //Rectangle dispRect = new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                SpriteBatch.Draw(videoFrame, new Vector2(40, 60), Color.White);
                SpriteBatch.End();
                //if (vid.IsPlaying)
                //{
                //    this.SpriteBatch.Begin();
                //    this.SpriteBatch.Draw(introVideo.CurrentTexture, new Vector2(10, 10), Color.White);
                //    this.SpriteBatch.End();
                //}
            }

            base.Draw(gameTime);
        }

        public String ExplainMusicPlayers()
        {
            String explanation = "";
            for (int i = 0; i < availablePlayers.Count; i++)
            {
                explanation += availablePlayers[i].SetupExplanation() + "\n";
            }
            return explanation;
        }
    }
}
