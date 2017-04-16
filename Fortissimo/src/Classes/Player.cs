// Description  : This is the main player of the game.  Each player
//                has its own notes source and input source and should
//                be able to determine the score... ect...

#region Using Statements
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
using SongDataIO;
using System.IO;
#endregion

namespace Fortissimo
{
    public interface IPlayerService
    {
        InputManager Input { get; }
        NoteManager Notes { get; }
    }

    public class Player : Microsoft.Xna.Framework.DrawableGameComponent, IPlayerService
    {
        public const int GUITAR = 0, RHYTHM = 1, DRUMS = 2, VOCALS = 3;

        public event Action<RhythmGame.GameStateType> ChangeState;
        public event Action<SongData.NoteSet> NoteWasMissed;
        public event Action<SongData.NoteSet> NoteWasHit;

        public InputManager Input { set; get; }
        public NoteManager Notes { set; get; }

        Texture2D healthBack;
        Texture2D lowHealthBack;
        Texture2D starTexture;

        Texture2D healthBox;
        protected SpriteFont spriteFont;
        protected SpriteFont titleFont;
        protected SpriteFont largeMenuFont;

        uint score = 0;
        public uint Score { get { return score; } }

        uint stars = 0;
        public uint Stars { get { return stars; } }
      
        float health_max = 35.0f;
        public float MaxHealth 
        { 
            get { return health_max; } 
            set { health_max = value; } 
        }
        float health = 35.0f;
        public float Health 
        { 
            get { return health; } 
            set { health = value; } 
        }

        uint noteWorth = 100;
        public uint NoteWorth 
        { 
            get { return noteWorth; } 
            set { noteWorth = value; } 
        }

        int noteStreak = 0;
        public int NoteStreak
        {
            get { return noteStreak; }
        }
        int missedStreak = 0;
        uint multiplier = 1;
        int antiMultiplier = 1;

        int notesHit = 0;
        public int NotesHit 
        { 
            get { return notesHit; } 
        }
        int notesMissed = 0;
        public int NotesMissed 
        { 
            get { return notesMissed; } 
        }

        ulong lastMissType = 0;
        public ulong LastMissType
        {
            get { return lastMissType; }
        }
        ulong goodHits;
        public ulong GoodHits
        {
            get { return goodHits; }
        }


        Instrument _instrument;

        SmokePlumeParticleSystem smoke;

        TimeSpan waitTimer;

        public Instrument Instr { get { return _instrument; } }

        public Player(Game game) : base(game)
        {
            Game.Services.RemoveService(typeof(IPlayerService));
            Game.Services.AddService(typeof(IPlayerService), this);

            _instrument = InstrumentMaster.GetSingleton().GetInstrument("LGT");

            smoke = new SmokePlumeParticleSystem(game, 50);
            game.Components.Add(smoke);
        }
        protected override void LoadContent()
        {
            base.LoadContent();
            
            spriteFont = Game.Content.Load<SpriteFont>("defaultSpriteFont");
            titleFont = Game.Content.Load<SpriteFont>("Menu/titleFont");
            largeMenuFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");

            healthBox = Game.Content.Load<Texture2D>("health_box");
            healthBack = Game.Content.Load<Texture2D>("health_back");
            lowHealthBack = Game.Content.Load<Texture2D>("low_health_back");

            missedSounds = new SoundEffect[2];
            missedSounds[0]= Game.Content.Load<SoundEffect>("Sound/Boing_1");
            missedSounds[1] = Game.Content.Load<SoundEffect>("Sound/Boing_2");

            starTexture = Game.Content.Load<Texture2D>("Skins/Guitar/RedStar");
            
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (((RhythmGame)Game).State == RhythmGame.GameStateType.Paused)
            {
                if (Input.OtherKeyPressed(OtherKeyType.Select))
                {
                    ChangeState(RhythmGame.GameStateType.Running);
                    return;
                }
            }

            if (((RhythmGame)Game).State != RhythmGame.GameStateType.Running)
                return;

            bool onLastNote = false;
            if (Notes.SongDone)
            {
                if (waitTimer > TimeSpan.Zero)
                {
                    waitTimer -= gameTime.ElapsedGameTime;
                    onLastNote = true;
                }
                else
                {
                    ChangeState(RhythmGame.GameStateType.SongSuccess);
                    return;
                }
            }

            if (Input.OtherKeyPressed(OtherKeyType.Pause))
            {
                ChangeState(RhythmGame.GameStateType.Paused);
                return;
            }

            SongData.NoteSet[] noteSet = Notes.NoteSet;
            int beginIndex = Notes.CurrentBeginIndex;
            int endIndex = Notes.CurrentEndIndex;

            ulong pressed = Input.FullPressed;
            ulong held = Input.FullHeld;

            if (onLastNote)
            {
                beginIndex--;
                endIndex--;
            }

            int noteRange = (endIndex - beginIndex);
            bool splitNotes = noteRange > 1;
            bool shouldIncrement = false;


            bool nextIsValid = Notes.ValidRange(Notes.NextIndex);
            if (Input.Strummed && nextIsValid)
            {
                bool wasBurning = false;
                for (int noteIdx = beginIndex; noteIdx <= endIndex; noteIdx++)
                {
                    SongData.NoteSet note = noteSet[noteIdx];
                    if (note.burning != 0L)
                        wasBurning = true;
                    note.burning = 0L;
                }
                if (wasBurning)
                    Notes.IncrementPastEnd();
            }

            for (int noteIdx = beginIndex; noteIdx <= endIndex; noteIdx++)
            {
                SongData.NoteSet note = noteSet[noteIdx];
                if (Input.Strummed && (note.burning != 0L) && !Notes.IsInNotePadding(note))
                {
                    note.burning = 0L;
                    shouldIncrement = true;
                    continue;
                }

                if (Notes.ValidRange() || (Notes.BurningRange() && (note.burning != 0L)))
                {
                    if (!Notes.BurningRange() && note.burning != 0L)
                    {
                        shouldIncrement = true;
                        continue;
                    }
                    if (Input.Strummed)
                        note.Strum();

                    note.AddPressedGuitar(_instrument, held);
                    goodHits = note.IsGood(_instrument, note.IsHOPO(_instrument)); //Notes.NoteIndex > 0 && noteSet[Notes.NoteIndex - 1].visible[0] == SongData.NoteSet.VIS_STATE.INVISIBLE);
                    bool burningBetter = (note.burning != 0L) && (goodHits > note.burning);
                    bool trueHit = goodHits > 0 && note.burning == 0L;
                    if (trueHit || burningBetter)
                    {
                        HitNote(note);
                        // Another hack... don't deal damage for burning hopos.
                        bool burningHOPO = (note.length <= 0) && note.IsHOPO(_instrument);
                        if (!burningHOPO && note.NumMissed > 0)
                            DealDamage(note);

                        if (note.length > 0)
                        {
                            note.burning = goodHits;
                        }
                        else
                        {
                            shouldIncrement = true;
                        }
                        note.exploding = goodHits;
                    }
                    else if (note.burning != 0L)
                    {
                        if (Notes.BurningRange() && (goodHits & note.type) != 0L)
                        {
                            note.burning &= goodHits;
                            BurnNote(note, gameTime);
                            //note.exploding = true;
                        }
                        else
                        {
                            note.burning = 0L;
                            shouldIncrement = true;

                        }
                    }
                    else if (Notes.LateRange())
                    {
                        DealDamage(note);
                        //missedNote = note; // 4.0change
                        for (int i = 0; i < note.visible.Length; i++)
                        {
                            if ( note.visible[i] == SongData.NoteSet.VIS_STATE.VISIBLE )
                                note.visible[i] = SongData.NoteSet.VIS_STATE.GREYED_OUT;
                        }
                        shouldIncrement = true;
                    }
                    held = (held ^ goodHits);
                }
                else
                {
                    if (Input.Strummed && !note.IsHOPO(_instrument))
                    {
                        if(noteIdx == 0 || !noteSet[noteIdx-1].IsHOPO(_instrument))
                        {
                            // 4.0change - Additionally, don't penalize when strumming immediately after a HOPO note
                            if(noteIdx == 0 || !(noteSet[noteIdx-1].IsHOPO(_instrument) && Notes.LateRange(noteIdx-1)))
                                DealDamage(note);
                        }
                        //missedNote = note; // 4.0change
                    }
                }
            }

            if (shouldIncrement)
                Notes.IncrementPastEnd();
        }
        /// <summary>
        /// Consitutes a missed note.
        /// </summary>
        public virtual void DealDamage(SongData.NoteSet note)
        {
            // Hack for the rpg stuff
            bool someExist = false;
            for (int i = 0; i < note.visible.Length; i++)
                someExist |= !(note.visible[i] == SongData.NoteSet.VIS_STATE.INVISIBLE);
            if (!someExist)
                return;

            noteStreak = 0;
            missedStreak++;

            antiMultiplier = Math.Min(missedStreak / 10 + 1, 4);
            multiplier = 1;

            notesMissed += note.NumMissed;

            lastMissType = (note.type ^ goodHits);

            if (notesMissed == 0)
                notesMissed = 1;

            health -= Math.Max(1, note.NumMissed) * antiMultiplier * 1.0F;

            // Realistically the sounds should be in the InputSkin class
            // or a new InputAudio class, allowing different instruments
            // to sound differently.  Due to time constraints we're going
            // to hack it here for now.
            if (Input.Strummed)
            {
                //health -= 0.5f;
                //MissedNoteSound();
            }

            // If dead, trigger fail event
            if ( NoteWasMissed != null )
                NoteWasMissed(note);
            if (health <= 0)
                ChangeState(RhythmGame.GameStateType.SongFail);
        }
        /// <summary>
        /// Consitutes a hit note.
        /// </summary>
        public virtual void HitNote(SongData.NoteSet note)
        {
            // Hack for the rpg stuff
            bool someExist = false;
            for (int i = 0; i < note.visible.Length; i++)
                someExist |= !(note.visible[i] == SongData.NoteSet.VIS_STATE.INVISIBLE);
            if (!someExist)
                return;

            note.Kill();

            noteStreak += note.NumHit;
            missedStreak = 0;

            notesHit += note.NumHit;

            // Modify a multiplier depending on streak.
            multiplier = (uint)Math.Min(noteStreak / 10 + 1, 4);
            antiMultiplier = 1;

            // Modify score according to modifier.
            score += (uint)(noteWorth * multiplier * note.NumHit);

            // Gain some health back
            health += 0.5F * multiplier;

            if (health > health_max)
                health = health_max;

            if (NoteWasHit != null)
                NoteWasHit(note);
        }

        SoundEffect[] missedSounds = null;
        Random random = new Random();
        public void MissedNoteSound()
        {
            if (missedSounds != null)
            {
                int numSounds = missedSounds.Length;
                int soundIdx = random.Next(numSounds);
                missedSounds[soundIdx].Play(0.25f, 1f, 0); // 4.0change - Turn it down, this thing is obnoxious
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (((RhythmGame)Game).State != RhythmGame.GameStateType.Running
                && ((RhythmGame)Game).State != RhythmGame.GameStateType.Paused)
                return;

            ISpriteBatchService spriteBatchService = 
                (ISpriteBatchService)Game.Services.GetService( typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            spriteBatch.Begin(0, BlendState.AlphaBlend); // 4.0change

            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;

            spriteBatch.DrawString(titleFont, "X" + multiplier, new Vector2((float)(width*0.8), 400), Color.Wheat);
            int barHeight = (int)(healthBack.Height * (health / health_max));
            if (health > (health_max/3.0))
                spriteBatch.Draw(healthBack, new Rectangle((int)(width*0.1), 500 - barHeight, healthBack.Width, barHeight), Color.Lime);
            else
                spriteBatch.Draw(lowHealthBack, new Rectangle((int)(width * 0.1), 500 - barHeight, healthBack.Width, barHeight), Color.Lime);
            spriteBatch.Draw(healthBox, new Rectangle((int)(width*0.1), 500 - healthBox.Height, healthBox.Width, healthBox.Height), Color.White);
            //spriteBatch.DrawString(spriteFont, "" + health, new Vector2((float)(width*0.1), 550), Color.Wheat);
            spriteBatch.DrawString(largeMenuFont, "" + score, new Vector2((float)(width*0.7), 300), Color.Wheat);

            // player is doing really good, smoke will rise!
            if (multiplier > 3)
            {
                Vector2 pos = new Vector2((int)(width * 0.1), 500-healthBox.Height);
                smoke.AddParticles(pos, Color.White);
            }

            // Check star levels...
            if (Notes != null)
            {
                uint[] starLevels = Notes.StarLevels;
                int stars = starLevels.Length;
                uint lastLevel = 0;
                uint starIdx = 0;
                for (int i = 0; i < stars; i++)
                {
                    // Are we good enough...?
                    Vector2 starPos = new Vector2(50 + starIdx * 150, 30);
                    Color slightlyClear = Color.White;
                    float scoreMax = (float)(starLevels[i] - lastLevel);
                    float scorePart = (float)(score - lastLevel);

                    if (starLevels[i] == 0)
                        continue;
                    starIdx++;

                    if (score > starLevels[i])
                    {
                        spriteBatch.Draw(starTexture, starPos, null, Color.White, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 1.0F);
                        this.stars = starIdx;
                    }
                    else
                    {
                        slightlyClear.A = (byte)((scorePart / scoreMax) * 255);
                        spriteBatch.Draw(starTexture, starPos, null, slightlyClear, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 1.0F);
                        break;
                    }
                    lastLevel = starLevels[i];
                }
            }

            // Our replacement for scoring different stars on a song...
            // if we're 
            //spriteBatch.DrawString(titleFont, "Wicked", new Vector2((float)(width * 0.2), 50), Color.Wheat);
            //Oh noes!
            //Beat-up
            //Bite
            //Bogus
            //Heinous
            //Nasty
            //Weak
            //Sad
            //Tired
            //Chump
            //Yuk
            //Jacked up
            //Fake
            //Crummy
            //Faulty
            //Crap
            //Aight
            //All Good
            //Freakin' Awesome
            //Perfect!
            //Butter
            //Cherry
            //Chill
            //Choice
            //Cool
            //Fly
            //Golden
            //A-1
            //Gravy
            //Hot
            //Killer
            //Nice
            //Pimp
            //Primo
            //Sick
            //Spiffy
            //Stellar
            //Styling
            //Sweet
            //Tight
            //Wicked

            spriteBatch.End();
        }

        public void BurnNote(SongData.NoteSet note, GameTime gameTime)
        {
            score += (uint)(note.NumHit * multiplier * gameTime.ElapsedGameTime.Milliseconds / 10);
        }

        public void ResetTimer()
        {
            waitTimer = TimeSpan.FromSeconds(3); // magic number: 3 sec after end of last note to keep scrolling
        }

        public virtual void Reset()
        {
            ResetTimer();
            score = 0;
            stars = 0;
            health = health_max;

            noteStreak = 0;
            missedStreak = 0;
            multiplier = 1;
            antiMultiplier = 1;

            notesHit = 0;
            notesMissed = 0;
            lastMissType = 0;

            ChangeState = null;
            //NoteWasMissed = null;
            //NoteWasHit = null;
        }

        public virtual void StartSong(RhythmGame game, GameTime gameTime, SongDataPlus dataPlus, int instrument, float difficulty)
        {
            // Get the input skin for the given instrument
            InputSkin inputSkin = new GuitarASDFG(game.ActiveInput, game);
            game.ActiveInput.UpdateOrder = 0;
            DrawOrder = 3;
            game.Components.Add(inputSkin);
            inputSkin.ReplaceBackground(Path.Combine(dataPlus.dirPath, "Default.png"));

            // Get the notes for the given song.
            NoteManager notes = null;
            switch (dataPlus.type)
            {
                case SongDataPlus.NoteType.MID:
                    notes = new GuitarHeroManager(game, this, instrument, difficulty < 0.25 ? 0 : difficulty < 0.5 ? 1 : difficulty < 0.75 ? 2 : difficulty < 0.95 ? 3 : 4, dataPlus);
                    break;
                case SongDataPlus.NoteType.GBA:
                    notes = new UnsignedManager(game, this, (byte)(difficulty < 0.25 ? 0 : difficulty < 0.5 ? 1 : difficulty < 0.75 ? 2 : 3), dataPlus.songData);
                    break;
                case SongDataPlus.NoteType.GenMID:
                    notes = new MidiManager(game, this, instrument, difficulty, dataPlus.fullPath);
                    break;
                default:
                    throw new NotSupportedException();
            }
            notes.UpdateOrder = 1;
            game.Components.Add(notes);
            Notes = notes;
            notes.StartSong(gameTime);
            Input = game.ActiveInput;

            game.Components.Add(this);
        }
    }

    public class PlayerFactory
    {
        public static Player TestPlayer(Player player, RhythmGame game)
        {
            Player test = player;
            if (test == null)
            {
                test = RhythmGame.ActivePlugin.CreatePlayer(game);
                test.ResetTimer();
                test.UpdateOrder = 3;
                test.DrawOrder = 2;
                game.Components.Add(test);
            }
            test.Input = game.ActiveInput;

            
            game.CurrentBand.BandMembers.Clear();
            game.CurrentBand.BandMembers.Add(test);
            return test;
        }
    }
}
