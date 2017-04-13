// Description  : This provides an abstract class that is intended to 
//                overloaded by different sources creating music
//                notes (such as midi devices).

#region Using Statements
using System;
using System.IO;
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
using SongDataIO;
#endregion

namespace Fortissimo
{
    public class NoteX : SongData.NoteSet
    {
        public bool isBonus = false;

        public NoteX()
            : base()
        {
        }

        public NoteX(uint time, ulong type)
            : base()
        {
            this.time = time;
            this.type = type;
        }

        public void AddTo(ulong type)
        {
            type |= type;
        }

        public static void InitializeSet(SongData.NoteSet[] noteSet, Player player)
        {
            for (int i = 0; i < noteSet.Length; i++)
            {
                noteSet[i].visible = new NoteX.VIS_STATE[player.Instr.NumTracks];
                for (int r = 0; r < noteSet[i].visible.Length; r++)
                    noteSet[i].visible[r] = NoteX.VIS_STATE.VISIBLE;
            }
        }
    }
    #region Note Manager Region
    public abstract class NoteManager : Microsoft.Xna.Framework.GameComponent
    {
        const uint Pillow = 200; // How close user can be to note for it to count
        const uint POST_SONG_BUFFER = 3; // 4.0change; 3 seconds after song; allows for last note to be hit

        protected SongData.NoteSet[] _noteSet = new SongData.NoteSet[0];
        protected uint[] _starLevels = new uint[0];
        protected int _noteIndex = 0;
        protected int _endIndex = 0;
        protected int _keyRange = 5;
        protected SongData.Barline[] _barlines = new SongData.Barline[0];

        TimeSpan _timeStarted;
        TimeSpan _songTime;
        TimeSpan _trueSongPosition;
        TimeSpan _tolerance = TimeSpan.FromMilliseconds(50); // Audio/visual drift tolerance

        bool _playing = false;
        bool _pastZero = false;

        public bool PastZero { get { return _pastZero; } }
        public TimeSpan TimeStarted { get { return _timeStarted; } }
        public TimeSpan SongTime { get { return _songTime; } }

        public int NoteIndex { get { return _noteIndex; } }

        public SongData.NoteSet[] NoteSet { get { return _noteSet; } }

        public SongData.NoteSet CurrentNoteSet
        { 
            get
            {
                if (_noteIndex < _noteSet.Length)
                {
                    return _noteSet[_noteIndex];
                }
                else
                {
                    return _noteSet[_noteSet.Length - 1];
                }
                
            }
        }

        public int CurrentBeginIndex { get { return _noteIndex; } }

        public int CurrentEndIndex { get { return _endIndex; } }

        public int NextIndex { get { return _endIndex + 1; } }

        public virtual SongData.Barline[] barlines { get { return _barlines; } }

        public bool SongDone { get { return (_noteIndex >= _noteSet.Length); } }

        public uint[] StarLevels { get { return _starLevels; } }

        public NoteManager(Game game)
            : base(game)
        {
        }

        public virtual void StartSong(GameTime gameTime)
        {
            for (int i = 0; i < _noteSet.Length - 1; i++)
            {
                uint dist = _noteSet[i + 1].time - _noteSet[i].time;
                if (dist > Pillow)
                    dist = Pillow;
                _noteSet[i].late = _noteSet[i].time + dist;
            }

            _timeStarted = gameTime.TotalGameTime;// +TimeSpan.FromSeconds(0); // TO DO: Magic number here??
            _playing = true;
            _pastZero = false;
            CalculateEndIndex();
        }

        public virtual int CalculateEndIndex()
        {
            _endIndex = _noteIndex;
            if (_noteIndex < _noteSet.Length)
            {
                while (_noteSet[_noteIndex].time - _noteSet[_endIndex].time < 10)
                {
                    _endIndex++;
                    if (_endIndex >= _noteSet.Length)
                        break;
                }
                _endIndex -= 1;
            }
            return _endIndex;
        }

        public virtual void IncrementNote()
        {
            _noteIndex++;
        }

        public virtual void IncrementPastEnd()
        {
            _noteSet[CurrentBeginIndex].burning = 0L;
            _noteIndex = CurrentEndIndex + 1;
            CalculateEndIndex();
        }

        public bool ValidRange(int index)
        {
            return (index < _noteSet.Length && _noteSet[index].time - Pillow < _songTime.TotalMilliseconds);
        }

        public bool ValidRange()
        {
            return ValidRange(_noteIndex);
        }

        public bool LateRange(int index)
        {
            return (index < _noteSet.Length && _noteSet[index].late <= _songTime.TotalMilliseconds);
        }

        public bool LateRange()
        {
            return LateRange(_noteIndex);
        }

        public bool BurningRange(SongData.NoteSet note)
        {
            return note.end > _songTime.TotalMilliseconds + POST_SONG_BUFFER;
        }

        public bool IsInNotePadding(SongData.NoteSet note)
        {
            // was 100
            return Math.Abs(_songTime.TotalMilliseconds - note.time) < Pillow; // 4.0change - Pillow was 190
        }

        public bool BurningRange(int index)
        {
            return index < _noteSet.Length && BurningRange(_noteSet[index]);
        }

        public bool BurningRange()
        {
            return BurningRange(_noteIndex);
        }

        public override void Update(GameTime gameTime)
        {
            if (!_playing)
                return;

            _songTime = gameTime.TotalGameTime - _timeStarted;
            if (_pastZero && _trueSongPosition != TimeSpan.Zero)
            {
                TimeSpan difference = _songTime - _trueSongPosition;
                if (difference > _tolerance || difference < -_tolerance)
                {
                    _songTime = _trueSongPosition;
                    _timeStarted = gameTime.TotalGameTime - _songTime;
                }
            }
            if (!_pastZero && _songTime > -_tolerance)
            {
                ((RhythmGame)Game).PlayQueuedSong();
                _pastZero = true;
            }
            base.Update(gameTime);
        }

        public void Reset()
        {
            _pastZero = false;
        }

        public void VerifySongTime(TimeSpan playPosition)
        {
            _trueSongPosition = playPosition;
        }

        public void SyncStartTime(TimeSpan timeAdded)
        {
            _timeStarted += timeAdded;
        }

        public int Length { get { return _noteSet.Length; } }
    }
    #endregion

    /// <summary>
    /// This is more or less a random note generator for testing purposes.
    /// It will only support 5 notes.  It needs to generate them on the
    /// fly so it can go on forever
    /// </summary>
    public class TestNoteManager : NoteManager
    {
        TimeSpan noteSpan;
        TimeSpan timeLeft;
        Random r;

        public TestNoteManager(Game game)
            : base(game)
        {
            noteSpan = new TimeSpan(0, 0, 1);
            timeLeft = noteSpan;

            r = new Random();
            GenerateNotes(300);
        }

        #region Note Generation
        public void GenerateNotes(int numNotes)
        {
            _noteSet = new NoteX[numNotes];
            // Randomly pick 1 of 3 notes
            for (int i = 0; i < numNotes; i++)
            {
                NoteX note = new NoteX();
                ulong rand = (ulong)r.Next(0, 5);
                note.time = (uint)(i * 1000);
                note.type = rand;
                _noteSet[i] = note;
            }
        }
        #endregion

    }


    /// <summary>
    /// The open source manager for songs from Unsigned
    /// </summary>
    public class UnsignedManager : NoteManager
    {
        SongData _songData;

        public UnsignedManager(Game game, Player player, byte difficulty, SongData songData)
            : base(game)
        {
            this._songData = songData;

            for (int i = 0; i < _songData.instruments.Length; i++)
            {
                if (_songData.instruments[i].instrumentType.Equals(player.Instr.CodeName))
                {
                    _noteSet = _songData.instruments[i].diffSets[difficulty].phrases[0].notes;
                    _starLevels = _songData.instruments[i].diffSets[difficulty].starScoreLevels;
                    // TODO: Grab star scores from here...
                    break;
                }
            }

            // Reset all the notes.  Not doing this makes the game auto play
            // its kind of cool actually.
            for (int i = 0; i < _noteSet.Length; i++)
                _noteSet[i].Reset();

            _barlines = _songData.info.barlines;

            NoteX.InitializeSet(_noteSet, player);
        }
    }

    public class GuitarHeroManager : NoteManager
    {
        private const int HOPO_Threshold = 200;

        public List<long> allBeats;
        private GuitarHeroMusicFile curMusicFile;
        private Player player;

        public GuitarHeroManager(Game game, Player player, int instrument, int difficulty, SongDataPlus dataPlus)
            : base(game)
        {
            this.player = player;

            int trackID = instrument == Player.GUITAR ? 0 : instrument == Player.RHYTHM ? 1 : instrument == Player.DRUMS ? 2 : instrument == Player.VOCALS ? 3 : 99;
            if (trackID == 99)
                throw new System.NotSupportedException();

            curMusicFile = new GuitarHeroMusicFile();
            if (!curMusicFile.GenerateNotesFromFile(dataPlus.fullPath))
                throw new System.NotSupportedException();

            // Check for tap-on/hammer-off notes... <200 ms since start of last note && this is the only note at this time slot
            if (curMusicFile.AllNotes[trackID][difficulty].Count > 1)
            {
                NoteX last = curMusicFile.AllNotes[trackID][difficulty][0];
                for (int i = 1; i < curMusicFile.AllNotes[trackID][difficulty].Count; i++)
                {
                    NoteX curr = curMusicFile.AllNotes[trackID][difficulty][i];
                    if (curr.type != last.type && curr.time != last.time)
                    {
                        // Make sure next note isn't at the same time slot
                        if (i == curMusicFile.AllNotes[trackID][difficulty].Count - 1 || curr.time != curMusicFile.AllNotes[trackID][difficulty][i + 1].time)
                        {
                            if (curr.time - last.time < HOPO_Threshold)
                                curMusicFile.AllNotes[trackID][difficulty][i].type |= (1<<5);
                        }
                    }
                    last = curr;
                }
            }
            foreach (NoteX note in curMusicFile.AllNotes[trackID][difficulty])
            {
                note.burning = 0;
                note.endtype = 0;
                if (note.length < 150)
                    note.length = 0;
            }

            allBeats = curMusicFile.markers;
            _noteSet = curMusicFile.AllNotes[trackID][difficulty].ToArray();
            NoteX.InitializeSet(NoteSet, player);
        }
    }
    
    public class MidiManager : NoteManager
    {
        private const int HOPO_Threshold = 200;

        public SongData _songData;
        private List<long> allBeats;
        private MidiFile curMusicFile;
        private Player player;


        public MidiManager(Game game, Player player, int instrument, float difficulty, String filename)
            : base(game)
        {
            this.player = player;
            MidiFile.UserMods mods = new MidiFile.UserMods(filename);
            List<int> tracks = instrument == Player.GUITAR ? mods.GuitarTracks : instrument == Player.RHYTHM ? mods.RhythmTracks : instrument == Player.DRUMS ? mods.DrumTracks : mods.VocalTracks;
            curMusicFile = new MidiFile();

            if (!curMusicFile.GenerateNotesFromFile(filename, tracks, difficulty))
                throw new System.NotSupportedException();

            // Apply user defined individual note changes
            List<int[]> changes = difficulty >= 0.95 ? null : mods.GetChangedNotes(instrument, difficulty < 0.25 ? 0 : difficulty < 0.5 ? 1 : difficulty < 0.75 ? 2 : 3);
            if (changes != null)
                foreach (int[] i_a in changes)
                    curMusicFile.AllNotes[i_a[0]].type = (ulong)i_a[1];

            // Set HOPO/tappable notes
            if (curMusicFile.AllNotes.Count > 1)
            {
                NoteX last = curMusicFile.AllNotes[0];
                for (int i = 1; i < curMusicFile.AllNotes.Count; i++)
                {
                    NoteX curr = curMusicFile.AllNotes[i];
                    if (curr.type != last.type && curr.time != last.time)
                    {
                        // Make sure next note isn't at the same time slot
                        if (i == curMusicFile.AllNotes.Count - 1 || curr.time != curMusicFile.AllNotes[i + 1].time)
                        {
                            if (curr.time - last.time < HOPO_Threshold)
                                curMusicFile.AllNotes[i].type |= (1<<5);
                        }
                    }
                    last = curr;
                }
            }
            /*
            // Add quarter-note ticks (may change by a factor of 2 for fast and slow songs)
            int msToNextQuarterNote = 0;
            curMusicFile.ticksPerBeat;
            curMusicFile.ticksPerFrame;
            curMusicFile.timeCode;
            
            int[] indexs = new int[curMusicFile.totalTracks];
            for(int ii=0; ii<curMusicFile.totalTracks; ii++)
                indexs[ii] = 0;

            bool loop;
            do
            {
                loop = false;
                for (int ii = 0; ii < curMusicFile.totalTracks; ii++)
                {
                    if(curMusicFile.allTracks[ii].allEvents[indexs[ii]].eventType == 0x51
                }

            } while (loop);

*/
            allBeats = curMusicFile.markers;
            _noteSet = curMusicFile.AllNotes.ToArray();
            NoteX.InitializeSet(NoteSet, player);

            // Build in Unsigned format
            _songData = new SongData();
            _songData.info = new SongData.FullBandChunk();
            SongData.Barline[] myBarlines = new SongData.Barline[curMusicFile.markers.Count];
            for (int i = 0; i < curMusicFile.markers.Count; i++)
                myBarlines[i] = new SongData.Barline((uint)curMusicFile.markers[i], 1);
            _songData.info.barlines = myBarlines;
        }
    }
}