using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SongDataIO
{
    /// <summary>
    /// Contains all data in a songdata file(s)
    /// </summary>
    public class SongData
    {
        /// <summary>
        /// Defines a Color in non-XNA environments (such as VocalsEditor)
        /// </summary>
        public struct Color
        {
            /// <summary>
            /// Red, Green, Blue, Alpha... duh
            /// </summary>
            public byte R, G, B, A;

            /// <summary>
            /// Generates an opaque Color
            /// </summary>
            /// <param name="r">Red Channel</param>
            /// <param name="g">Green Channel</param>
            /// <param name="b">Blue Channel</param>
            public Color(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
                A = 255;
            }

            /// <summary>
            /// Generates a semi-transparent Color
            /// </summary>
            /// <param name="r">Red Channel</param>
            /// <param name="g">Green Channel</param>
            /// <param name="b">Blue Channel</param>
            /// <param name="a">Alpha Channel (255=opaque,0=invisible)</param>
            public Color(byte r, byte g, byte b, byte a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        /// <summary>
        /// Represents a single note or chord
        /// </summary>
        public class NoteSet : IComparable
        {
            public uint time, length;
            public ulong type, endtype;
            public String text;
            public uint end
            {
                get { return time + length; }
            }

            public VIS_STATE[] visible;//0=visible,1=greyedout,2=invisible,3=invisibleButAvailable(HOPO)
            public ulong burning;//for held Notes
            public ulong exploding;

            public uint late;

            public enum VIS_STATE { VISIBLE = 0, GREYED_OUT = 1, INVISIBLE = 2, HOPOED = 3, OVERDONE = 4/*drums*/, };

            protected bool strummed;
            protected ulong pressed;
            protected ulong good;

            protected int numMissed;
            public int NumMissed { get { return numMissed; } }
            protected int numHit;
            public int NumHit { get { return numHit; } }

            public static ulong IsValidFrettage(ulong note, ulong pressed, Instrument instr)
            {
                int numHit, numMissed;
                return IsValidFrettage(note, pressed, instr, out numHit, out numMissed);
            }

            private static uint GetNumFrets(ulong value)
            {
                uint retVal = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (((1 << i) & (int)value) != 0)
                        retVal++;
                }

                return retVal;
            }

            public static ulong IsValidFrettage(ulong note, ulong pressed, Instrument instr, out int numHit, out int numMissed)
            {
                ulong NOT_HOPO_VALUE = ~(((ulong)1) << instr.NumTracks);
                note &= NOT_HOPO_VALUE;

                numMissed = 1;
                numHit = 0;

                // There's no note to be hit, penalize!
                if (note == 0)
                    return 0L;

                // 4.0change: Check for chords and return false if too many frets are pressed (ok if single notes though)
                uint nFrets = GetNumFrets(note);
                if (nFrets > 1)
                    if(nFrets != GetNumFrets(pressed))
                        return 0L;

                numMissed = 0;

                // Starting from the lowest note we check if its safe.
                ulong intersection = 0L;
                bool extraNote = false;
                bool hitOne = false;
                for (int i = 4; i >= 0; i--)
                {
                    ulong fretMask = (ulong)1 << i;
                    if ((fretMask & pressed) != 0)
                    {
                        // Fret is pressed.
                        ulong singleFret = fretMask & note;
                        if ((singleFret & pressed) != 0)
                        {
                            // Only correct notes below this one...?
                            if (!extraNote)
                            {
                                hitOne = true;
                                intersection |= fretMask;
                                numHit++;
                            }
                        }
                        else
                        {
                            // They're pressing a fret not part of the note.
                            // This is only a problem if there's another note on a higher fret that it blocks.
                            extraNote = true;
                            //intersection |= fretMask;
                        }
                    }
                    else
                    {
                        // Fret is not being pressed.
                        ulong singleFret = fretMask & note;
                        if (singleFret == 0)
                        {
                            // Nothing to hit.
                            //intersection |= fretMask;
                        }
                        else
                        {
                            // They missed a note they should have hit.
                            numMissed++;
                        }
                    }
                }
                if (!hitOne)
                    intersection = 0L;
                return intersection;
            }

            public void Strum()
            {
                strummed = true;
            }

            public void AddPressedGuitar(Instrument instr, ulong pressed)
            {
                good = IsValidFrettage(type, pressed, instr, out numHit, out numMissed);
            }

            public ulong AddPressedDrums(Instrument instr, ulong pressed)
            {
                ulong ret = 0;
                if (pressed == 0)
                    return 0;
                for (int i = 0; i < instr.NumTracks; i++)
                {
                    if ((pressed & (((ulong)1) << i)) != 0 && (this.pressed & (((ulong)1) << i)) == 0)
                    { ret |= (byte)(1 << i); visible[i] = VIS_STATE.INVISIBLE; }
                }
                this.pressed |= pressed;
                ret &= this.type;
                return ret;
            }

            public ulong IsGood(Instrument instr, bool HOPOable)
            {
                if (!instr.NeedsStrum)
                    if (pressed != 0)
                        return (pressed) & (type);
                    else
                        return (pressed) & (type);
                if ((instr.CanHOPO && HOPOable && (type & (((ulong)1) << instr.NumTracks)) != 0) || strummed)
                    return good;
                return 0L;
            }

            public int CompareTo(object other)
            {
                return time.CompareTo(((NoteSet)other).time);
            }

            public void Kill()
            {
                for (int i = 0; i < visible.Length; i++)
                {
                    ulong fret = 1UL << i;
                    if ( (fret & good) != 0 )
                        visible[i] = VIS_STATE.INVISIBLE;
                    else
                        visible[i] = VIS_STATE.GREYED_OUT;
                }
            }

            public bool IsHOPO(Instrument instrument)
            {
                if (!instrument.CanHOPO)
                    return false;

                // 4.0change - Don't allow chords to be HOPO'd
                //if (this.length != 1)
                    //return false;

                return (type & (((ulong)1) << instrument.NumTracks)) != 0;
            }

            public bool HasStrummed()
            {
                return strummed;
            }

            public void Reset()
            {
                if (visible != null)
                {
                    for (int i = 0; i < visible.Length; i++)
                        visible[i] = VIS_STATE.VISIBLE;
                }
                burning = 0L;
                exploding = 0L;
                late = 0;
                strummed = false;
                pressed = 0;
                good = 0L;
                numMissed = 0;
                numHit = 0;
            }
        }

        /// <summary>
        /// Represents a section of notes
        /// </summary>
        public struct Phrase
        {
            public uint time;
            public SongData.TYPE type;
            public bool rockpower;
            public SongData.RTYPE rType;
            public NoteSet[] notes;
        }

        /// <summary>
        /// Represents a "fill" (think drum fills) that
        /// can be used for some instruments to activate
        /// rock power
        /// </summary>
        public struct Fill
        {
            public uint time, len;
            public bool hitGreen;
            public float amount;
            public uint end
            {
                get { return time + len; }
            }

            public Fill(uint time, uint len)
            {
                this.time = time;
                this.len = len;
                hitGreen = false;
                amount = 0f;
            }
        }

        /// <summary>
        /// Represents a section of time in which
        /// all encompassed notes are white and, if
        /// hit, will give the user 25% rock power
        /// </summary>
        public struct RockPowerPhrase
        {
            public uint time, len;
            public bool okay;//defaulted to true, falsed when note missed
            public uint end
            {
                get { return time + len; }
            }
            public RockPowerPhrase(uint time, uint len)
            {
                this.time = time;
                this.len = len;
                okay = true;
            }
        }

        /// <summary>
        /// Represents a "solo" section where the user
        /// can recieve a score bonus for high levels
        /// of hit percentage.
        /// </summary>
        public struct Solo
        {
            public uint time, len;
            public bool okay;//defaulted to true, falsed when note missed
            public uint end
            {
                get { return time + len; }
            }
            public Solo(uint time, uint len)
            {
                this.time = time;
                this.len = len;
                okay = true;
            }
        }

        /// <summary>
        /// Represents a special lighting effect.
        /// </summary>
        public abstract class SpecialEffect
        {
            public uint time;
            public String type;
            public uint length;

            public uint begin { get { return time; } }
            public uint end { get { return time+length; } }
        }

        /// <summary>
        /// Represents a return to normalcy in regards to lighting
        /// </summary>
        public class NormalLightingSpecialEffect : SpecialEffect
        {
            public Color color;
            public NormalLightingSpecialEffect(uint time, uint length, Color color)
            {
                this.time = time;
                this.length = length;
                this.color = color;
                this.type = "ln";
            }
        }

        /// <summary>
        /// Holds all lighting effects in the track
        /// </summary>
        public struct EffectsTrack
        {
            /// <summary>
            /// Array of time values for when the camera
            /// should switch to a new track.
            /// Stored in Milliseconds
            /// </summary>
            public uint[] cameraSwitches;
            /// <summary>
            /// An array of lighting effects sorted by time
            /// </summary>
            public SpecialEffect[] effects;
        }
        
        /// <summary>
        /// represents a bar line for visual purposes
        /// </summary>
        public struct Barline
        {
            public uint time;
            public uint numBeats;
            public Barline(uint time, uint numBeats)
            {
                this.time = time;
                this.numBeats = numBeats;
            }
        }

        /// <summary>
        /// represents a big rock ending section
        /// in which all players play freestyle for
        /// bonus points at the end of a song
        /// </summary>
        public struct BigRockEnding
        {
            public bool enabled;
            public uint start, end;

            public BigRockEnding(bool enabled, uint start, uint end)
            {
                this.enabled = enabled;
                this.start = start;
                this.end = end;
            }
        }

        /// <summary>
        /// Represents a section of the song where if all
        /// specified players hit all notes encompassed by
        /// the section, they get a bonus
        /// </summary>
        public struct Harmony
        {
            public uint start, end;
            public ulong instruments;
            public Harmony(uint start, uint end, ulong instruments)
            {
                this.start = start;
                this.end = end;
                this.instruments = instruments;
            }
        }

        /// <summary>
        /// Represents information not specific to a 
        /// single instrument
        /// </summary>
        public class FullBandChunk
        {
            /// <summary>
            /// SongData version, for compatibility purposes
            /// </summary>
            public byte version;
            /// <summary>
            /// Name of the file
            /// </summary>
            public String filename;
            /// <summary>
            /// Title of the Song
            /// </summary>
            public String name;
            /// <summary>
            /// Name of the Composer/Performer of the Song
            /// </summary>
            public String artist;
            /// <summary>
            /// Year song was released
            /// </summary>
            public uint year;
            /// <summary>
            /// Genre of the song
            /// Only used for sorting and pre-song header
            /// </summary>
            public String genre;
            /// <summary>
            /// When the song should end in success
            /// </summary>
            public TimeSpan length;
            /// <summary>
            /// Quotes to display during loading
            /// Always length 8, but not all strings
            /// need to be valid
            /// </summary>
            public String[] quotes;
            /// <summary>
            /// A list of people involved in charting
            /// this song
            /// </summary>
            public String[] charters;
            /// <summary>
            /// The barlines for visual purposes
            /// </summary>
            public Barline[] barlines;
            /// <summary>
            /// After we run out of barlines, how long
            /// should measures be while the song trails off
            /// In milliseconds
            /// </summary>
            public uint trailingBeatLen;
            /// <summary>
            /// Notice there is only one BRE
            /// </summary>
            public BigRockEnding bre;
            /// <summary>
            /// Array of harmony sections.
            /// Sorted by time
            /// </summary>
            public Harmony[] harmonies;

            /// <summary>
            /// what is displayed as the song revs up
            /// </summary>
            public String[] SongDisplayInfo;

            public void GenerateSongDisplayInfo()
            {
                SongDisplayInfo = new string[3 + charters.Length];
                SongDisplayInfo[0] = name;
                SongDisplayInfo[1] = artist;
                SongDisplayInfo[2] = "Charter" + (charters.Length > 1 ? "s:" : ":");
                for (int i = 0; i < charters.Length; i++)
                    SongDisplayInfo[i + 3] = charters[i];
            }
        }
        
        /// <summary>
        /// Contains all the information for a specific
        /// instruments at a specific difficulty
        /// </summary>
        public class DifficultySet
        {
            public int diff;
            public SongData.Phrase[] phrases;
            public uint[] starScoreLevels;

            public DifficultySet()
            {
                diff = -1;
                phrases = null;
                starScoreLevels = new uint[6];
            }
        }

        /// <summary>
        /// holds all information for each instrument
        /// including which instrument it maps to
        /// </summary>
        public class SongDataInstrument
        {
            /// <summary>
            /// The 3-character instrument code name
            /// </summary>
            public String instrumentType;
            /// <summary>
            /// How hard this instrument is
            /// on a scale of 1-100
            /// </summary>
            public byte difficulty;
            /// <summary>
            /// All rock power phrases
            /// </summary>
            public SongData.RockPowerPhrase[] rpPhrases;
            /// <summary>
            /// All solo sections
            /// should be length 0 for non solo types
            /// </summary>
            public SongData.Solo[] solos;
            /// <summary>
            /// All fills
            /// should be length 0 for non fill RPEnable types
            /// </summary>
            public SongData.Fill[] fills;
            /// <summary>
            /// the actual note data
            /// should be length 4
            /// </summary>
            public DifficultySet[] diffSets;

            public SongDataInstrument()
            {
                instrumentType = "NUL";
                difficulty = 0;
                rpPhrases = null;
                solos = null;
                fills = null;
                diffSets = new DifficultySet[SongLoader.NumDifficulties];
            }
        }

        public enum TYPE { REGULAR = 0, BLANK = 1, RHYTHM = 2 };
        public enum RTYPE { TAMBOURINE = 0, COWBELL = 1, CLAP = 2 };

        public FullBandChunk info;
        public SongDataInstrument[] instruments;
        public EffectsTrack effects;

        public SongData()
        {
            info = new FullBandChunk();
            //make sure quotes is length 8
            info.quotes = new string[8];
            effects = new EffectsTrack();
        }
    }

    /// <summary>
    /// Loads songdata files into a SongData struct
    /// </summary>
    public class SongLoader
    {
        public const int NumDifficulties = 4;

        public static TimeSpan LengthStringToTimeSpan(String songLength)
        {
            String z = songLength;
            uint iSongLength = 0;
            iSongLength += 3600u*UInt32.Parse(z.Substring(0, z.IndexOf(':')));
            z = z.Substring(z.IndexOf(':') + 1);
            iSongLength += 60u*UInt32.Parse(z.Substring(0, z.IndexOf(':')));
            iSongLength += UInt32.Parse(z.Substring(z.IndexOf(':') + 1));
            return TimeSpan.FromSeconds(iSongLength);
        }

        private static String[] GetCharters(List<String> charters)
        {
            int numCharters = charters.Count; ;
            int[] numC = new int[numCharters];
            string[] charters2 = new string[numCharters];
            charters2[0] = charters[0];
            numC[0]++;
            int nC2 = 1;
            for (int i = 1; i < numCharters; i++)
            {
                int k;
                for (k = 0; k < nC2; k++)
                    if (charters[i].Equals(charters2[k]))
                    {
                        numC[k]++;
                        break;
                    }
                if (k >= nC2)
                {
                    charters2[k] = charters[i];
                    numC[k]++;
                    nC2++;
                }
            }
            if (nC2 > 2)
            {
                for (int i = 2; i < nC2; i++)
                {
                    for (int k = i - 1; k >= 1; k--)
                    {
                        if (numC[k] > numC[k + 1])
                        {
                            int t = numC[k];
                            numC[k] = numC[k + 1];
                            numC[k + 1] = t;
                            string s = charters2[k];
                            charters2[k] = charters2[k + 1];
                            charters2[k + 1] = s;
                        }
                        else
                            break;
                    }
                }
            }
            String[] ret = new String[nC2];
            for (int i = 0; i < nC2; i++)
                ret[i] = charters2[i];
            return ret;
        }

        public static SongData LoadSong12(String fn)
        {
            SongData ret = new SongData();

            String dir = "";

            if (fn.Contains("\\") || fn.Substring(0,fn.Length-1).ToLower().EndsWith(".gb"))
            {
                dir = fn.Substring(0, fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(0, fn.LastIndexOf('.'));
            }
            ret.info.filename = fn;
            if (!File.Exists(dir + fn + ".gba"))
            {
#if WINDOWS
                System.Windows.Forms.MessageBox.Show("songdata not found");
#endif
                return null;
            }
            BinaryReader reader = new BinaryReader(File.OpenRead(dir + fn + ".gba"));
            ret.info.version = reader.ReadByte();
            if (ret.info.version < 12)
            {
#if WINDOWS
                System.Windows.Forms.MessageBox.Show("SongData Version too old");
#endif
                return null;
            }
            ret.info.name = reader.ReadString();
            ret.info.artist = reader.ReadString();
            ret.info.year = reader.ReadUInt32();
            ret.info.genre = reader.ReadString();
            String lengthString = reader.ReadString();
            ret.info.length = LengthStringToTimeSpan(lengthString);
            ret.info.quotes = new string[8];
            for (int i = 0; i < ret.info.quotes.Length; i++)
                ret.info.quotes[i] = reader.ReadString();

            int numCharters = 6;
            List<String> chtemp = new List<String>();
            for (int i = 0; i < numCharters; i++)
                chtemp.Add(reader.ReadString());
            ret.info.charters = GetCharters(chtemp);

            byte[] difficulties = new byte[4];
            for (int i = 0; i < 4; i++)
                difficulties[i] = reader.ReadByte();
            ret.info.barlines = new SongData.Barline[reader.ReadInt32()];
            for (int c = 0; c < ret.info.barlines.Length; c++)
            {
                ret.info.barlines[c] = new SongData.Barline(reader.ReadUInt32(), reader.ReadUInt32());
            }
            ret.info.trailingBeatLen = reader.ReadUInt32();

            reader.Close();

            ret.info.harmonies = new SongData.Harmony[0];

            ret.instruments = new SongData.SongDataInstrument[4];

            reader = new BinaryReader(File.OpenRead(dir + fn + ".gbg"));

            reader.ReadByte();//version
            {
                SongData.SongDataInstrument guitar = new SongData.SongDataInstrument();
                guitar.instrumentType = "LGT";
                guitar.difficulty = difficulties[0];

                guitar.rpPhrases = new SongData.RockPowerPhrase[reader.ReadInt32()];
                for (int i = 0; i < guitar.rpPhrases.Length; i++)
                {
                    guitar.rpPhrases[i] = new SongData.RockPowerPhrase();
                    guitar.rpPhrases[i].time = reader.ReadUInt32();
                    guitar.rpPhrases[i].len = reader.ReadUInt32();
                }
                guitar.solos = new SongData.Solo[0];
                guitar.diffSets = new SongData.DifficultySet[4];
                for (int i = 0; i < 4; i++)
                {
                    int k = reader.ReadInt32();
                    guitar.diffSets[k] = new SongData.DifficultySet();
                    guitar.diffSets[k].diff = k;
                    guitar.diffSets[k].phrases = new SongData.Phrase[1];
                    guitar.diffSets[k].phrases[0] = new SongData.Phrase();
                    guitar.diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadInt32()];
                    for (int j = 0; j < guitar.diffSets[k].phrases[0].notes.Length; j++)
                    {
                        guitar.diffSets[k].phrases[0].notes[j] = new SongData.NoteSet();
                        guitar.diffSets[k].phrases[0].notes[j].type = reader.ReadByte();
                        guitar.diffSets[k].phrases[0].notes[j].time = reader.ReadUInt32();
                        guitar.diffSets[k].phrases[0].notes[j].length = reader.ReadUInt32();
                    }
                    guitar.diffSets[k].starScoreLevels = new uint[6];
                    for (int j = 0; j < 5; j++)
                        guitar.diffSets[k].starScoreLevels[j] = reader.ReadUInt32();
                }
                reader.Close();

                ret.instruments[0] = guitar;
            }

            reader = new BinaryReader(File.OpenRead(dir + fn + ".gbb"));

            reader.ReadByte();//version

            SongData.SongDataInstrument bass = new SongData.SongDataInstrument();
            bass.instrumentType = "BAS";
            bass.difficulty = difficulties[3];

            bass.rpPhrases = new SongData.RockPowerPhrase[reader.ReadInt32()];
            for (int i = 0; i < bass.rpPhrases.Length; i++)
            {
                bass.rpPhrases[i] = new SongData.RockPowerPhrase(reader.ReadUInt32(), reader.ReadUInt32());
            }
            bass.diffSets = new SongData.DifficultySet[4];
            for (int i = 0; i < 4; i++)
            {
                int k = reader.ReadInt32();
                bass.diffSets[k] = new SongData.DifficultySet();
                bass.diffSets[k].phrases = new SongData.Phrase[1];
                bass.diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadInt32()];
                SongData.NoteSet[] arr = bass.diffSets[k].phrases[0].notes;
                for (int j = 0; j < arr.Length; j++)
                {
                    arr[j] = new SongData.NoteSet();
                    arr[j].type = reader.ReadByte();
                    arr[j].time = reader.ReadUInt32();
                    arr[j].length = reader.ReadUInt32();
                }
                bass.diffSets[k].starScoreLevels = new uint[6];
                for (int j = 0; j < 5; j++)
                    bass.diffSets[k].starScoreLevels[j] = reader.ReadUInt32();
            }
            reader.Close();

            ret.instruments[3] = bass;

            reader = new BinaryReader(File.OpenRead(dir + fn + ".gbd"));


            SongData.SongDataInstrument drums = new SongData.SongDataInstrument();
            drums.instrumentType = "SET";
            drums.difficulty = difficulties[2];

            reader.ReadByte();//version
            drums.rpPhrases = new SongData.RockPowerPhrase[reader.ReadInt32()];
            for (int i = 0; i < drums.rpPhrases.Length; i++)
            {
                drums.rpPhrases[i] = new SongData.RockPowerPhrase(reader.ReadUInt32(), reader.ReadUInt32());
            }
            drums.fills = new SongData.Fill[reader.ReadInt32()];
            for (int i = 0; i < drums.fills.Length; i++)
            {
                drums.fills[i] = new SongData.Fill(reader.ReadUInt32(), reader.ReadUInt32());
            }
            drums.diffSets = new SongData.DifficultySet[4];
            for (int i = 0; i < 4; i++)
            {
                int k = reader.ReadInt32();
                drums.diffSets[k] = new SongData.DifficultySet();
                drums.diffSets[k].phrases = new SongData.Phrase[1];
                drums.diffSets[k].phrases[0] = new SongData.Phrase();
                drums.diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadInt32()];
                SongData.NoteSet[] arr = drums.diffSets[k].phrases[0].notes;
                for (int j = 0; j < arr.Length; j++)
                {
                    arr[j] = new SongData.NoteSet();
                    byte type = reader.ReadByte();
                    byte ntp = (byte)(type & 0x0E);
                    if ((type & 0x10) != 0)
                        ntp |= 0x01;
                    if ((type & 0x01) != 0)
                        ntp |= 0x10;
                    if ((ntp & 0x01) != 0)
                        ntp |= 0x20;
                    ntp >>= 1;
                    arr[j].type = ntp;
                    arr[j].time = reader.ReadUInt32();
                }
                drums.diffSets[k].starScoreLevels = new uint[6];
                for (int j = 0; j < 5; j++)
                    drums.diffSets[k].starScoreLevels[j] = reader.ReadUInt32();
            }
            reader.Close();

            ret.instruments[2] = drums;

            reader = new BinaryReader(File.OpenRead(dir + fn + ".gbv"));

            SongData.SongDataInstrument vocals = new SongData.SongDataInstrument();

            reader.ReadByte();//version

            vocals.instrumentType = "LVX";
            vocals.difficulty = difficulties[1];
            vocals.rpPhrases = new SongData.RockPowerPhrase[0];
            vocals.fills = new SongData.Fill[0];
            vocals.diffSets = new SongData.DifficultySet[1];
            vocals.diffSets[0] = new SongData.DifficultySet();
            vocals.diffSets[0].phrases = new SongData.Phrase[reader.ReadUInt32()];
            for (int i = 0; i < vocals.diffSets[0].phrases.Length; i++)
            {
                vocals.diffSets[0].phrases[i] = new SongData.Phrase();
                vocals.diffSets[0].phrases[i].time = reader.ReadUInt32();
                vocals.diffSets[0].phrases[i].type = (SongData.TYPE)reader.ReadByte();
                vocals.diffSets[0].phrases[i].notes = new SongData.NoteSet[reader.ReadInt32()];
                if (vocals.diffSets[0].phrases[i].type == SongData.TYPE.REGULAR)
                {
                    for (int k = 0; k < vocals.diffSets[0].phrases[i].notes.Length; k++)
                    {
                        vocals.diffSets[0].phrases[i].notes[k] = new SongData.NoteSet();
                        vocals.diffSets[0].phrases[i].notes[k].time = reader.ReadUInt32();
                        vocals.diffSets[0].phrases[i].notes[k].length = reader.ReadUInt32() - vocals.diffSets[0].phrases[i].notes[k].time;
                        vocals.diffSets[0].phrases[i].notes[k].type = (ulong)reader.ReadInt16();
                        vocals.diffSets[0].phrases[i].notes[k].endtype = vocals.diffSets[0].phrases[i].notes[k].type;
                        vocals.diffSets[0].phrases[i].notes[k].text = reader.ReadString();
                    }
                }
                else if (vocals.diffSets[0].phrases[i].type == SongData.TYPE.RHYTHM)
                {
                    vocals.diffSets[0].phrases[i].rType = (SongData.RTYPE)reader.ReadByte();
                    for (int k = 0; k < vocals.diffSets[0].phrases[i].notes.Length; k++)
                    {
                        vocals.diffSets[0].phrases[i].notes[k] = new SongData.NoteSet();
                        vocals.diffSets[0].phrases[i].notes[k].time = reader.ReadUInt32();
                    }
                }
                else if (vocals.diffSets[0].phrases[i].type == SongData.TYPE.BLANK)
                    vocals.diffSets[0].phrases[i].notes = new SongData.NoteSet[0];
            }
            vocals.diffSets[0].starScoreLevels = new uint[6];
            for (int j = 0; j < 5; j++)
                vocals.diffSets[0].starScoreLevels[j] = reader.ReadUInt32();
            reader.Close();

            ret.instruments[1] = vocals;

            reader = new BinaryReader(File.OpenRead(dir + fn + ".gbe"));

            reader.ReadByte();//version
            ret.effects.cameraSwitches = new uint[reader.ReadInt32()];
            for (int i = 0; i < ret.effects.cameraSwitches.Length; i++)
                ret.effects.cameraSwitches[i] = reader.ReadUInt32();

            reader.Close();

            return ret;
        }

        public static SongData LoadSong17(String fn)
        {

            SongData ret = new SongData();
            String dir = "";
            if (fn.Contains("\\") || fn.ToLower().EndsWith("uns"))
            {
                dir = fn.Substring(0, fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(0, fn.LastIndexOf('.'));
            }
            ret.info.filename = fn;

            if (!File.Exists(dir + fn + ".uns"))
            {
#if WINDOWS
                System.Windows.Forms.MessageBox.Show("songdata not found");
#endif
                return null;
            }

            BinaryReader reader = new BinaryReader(File.OpenRead("songdata\\" + fn + ".uns"));

            reader.ReadBytes(3);//UNS

            int offsetToGBA = reader.ReadInt32();
            int offsetToGBG = reader.ReadInt32();
            int offsetToGBB = reader.ReadInt32();
            int offsetToGBD = reader.ReadInt32();
            int offsetToGBV = reader.ReadInt32();
            int offsetToGBE = reader.ReadInt32();

            ret.info.version = reader.ReadByte();
            if (ret.info.version != 17)
            {
#if WINDOWS
                System.Windows.Forms.MessageBox.Show("SongData Version too old");
#endif
                return null;
            }

            ret.info.name = reader.ReadString();
            ret.info.artist = reader.ReadString();
            ret.info.year = reader.ReadUInt32();
            ret.info.genre = reader.ReadString();
            ret.info.length = LengthStringToTimeSpan(reader.ReadString());
            ret.info.quotes = new string[8];
            for (int i = 0; i < 8; i++)
                ret.info.quotes[i] = reader.ReadString();

            List<String> charters = new List<String>();
            for (int i = 0; i < 6; i++)
            {
                String str = reader.ReadString();
                if (charters.Contains(str))
                    charters.Add(str);
            }
            ret.info.charters = charters.ToArray();

            reader.ReadUInt32();//irrelevant diffs

            ret.info.barlines = new SongData.Barline[reader.ReadInt32()];
            for (int i = 0; i < ret.info.barlines.Length; i++)
                ret.info.barlines[i] = new SongData.Barline(reader.ReadUInt32(), reader.ReadUInt32());

            ret.info.trailingBeatLen = reader.ReadUInt32();

            ret.info.bre.enabled = reader.ReadBoolean();
            ret.info.bre.start = reader.ReadUInt32();
            ret.info.bre.end = reader.ReadUInt32();

            ret.info.harmonies = new SongData.Harmony[reader.ReadUInt32()];
            for (int i = 0; i < ret.info.harmonies.Length; i++)
                ret.info.harmonies[i] = new SongData.Harmony(reader.ReadUInt32(), reader.ReadUInt32(), (ulong)reader.ReadByte());

            ret.instruments = new SongData.SongDataInstrument[4];

            //guitar
            ret.instruments[0] = new SongData.SongDataInstrument();
            ret.instruments[0].instrumentType = "LGT";
            ret.instruments[0].rpPhrases = new SongData.RockPowerPhrase[reader.ReadUInt32()];
            for (int i = 0; i < ret.instruments[0].rpPhrases.Length; i++)
                ret.instruments[0].rpPhrases[i] = new SongData.RockPowerPhrase(reader.ReadUInt32(), reader.ReadUInt32());
            ret.instruments[0].solos = new SongData.Solo[reader.ReadUInt32()];
            for (int i = 0; i < ret.instruments[0].solos.Length; i++)
                ret.instruments[0].solos[i] = new SongData.Solo(reader.ReadUInt32(), reader.ReadUInt32());
            ret.instruments[0].diffSets = new SongData.DifficultySet[4];
            for (int ir = 0; ir < 4; ir++)
            {
                uint k = reader.ReadUInt32();
                ret.instruments[0].diffSets[k] = new SongData.DifficultySet();
                ret.instruments[0].diffSets[k].phrases = new SongData.Phrase[1];
                ret.instruments[0].diffSets[k].phrases[0] = new SongData.Phrase();
                ret.instruments[0].diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadUInt32()];
                for(int i=0;i<ret.instruments[0].diffSets[k].phrases[0].notes.Length;i++)
                {
                    SongData.NoteSet note = new SongData.NoteSet();
                    note.type = (ulong)reader.ReadByte();
                    note.time = reader.ReadUInt32();
                    note.length = reader.ReadUInt32();
                    ret.instruments[0].diffSets[k].phrases[0].notes[i] = note;
                }
                ret.instruments[0].diffSets[k].starScoreLevels = new uint[6];
                for (int i = 0; i < 5; i++)
                    ret.instruments[0].diffSets[k].starScoreLevels[i] = reader.ReadUInt32();
            }


            //bass
            ret.instruments[3] = new SongData.SongDataInstrument();
            ret.instruments[3].instrumentType = "BAS";
            ret.instruments[3].rpPhrases = new SongData.RockPowerPhrase[reader.ReadUInt32()];
            for (int i = 0; i < ret.instruments[3].rpPhrases.Length; i++)
                ret.instruments[3].rpPhrases[i] = new SongData.RockPowerPhrase(reader.ReadUInt32(), reader.ReadUInt32());
            ret.instruments[3].diffSets = new SongData.DifficultySet[4];
            for (int ir = 0; ir < 4; ir++)
            {
                uint k = reader.ReadUInt32();
                ret.instruments[3].diffSets[k] = new SongData.DifficultySet();
                ret.instruments[3].diffSets[k].phrases = new SongData.Phrase[1];
                ret.instruments[3].diffSets[k].phrases[0] = new SongData.Phrase();
                ret.instruments[3].diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadUInt32()];
                for (int i = 0; i < ret.instruments[3].diffSets[k].phrases[0].notes.Length; i++)
                {
                    SongData.NoteSet note = new SongData.NoteSet();
                    note.type = (ulong)reader.ReadByte();
                    note.time = reader.ReadUInt32();
                    note.length = reader.ReadUInt32();
                    ret.instruments[3].diffSets[k].phrases[0].notes[i] = note;
                }
                ret.instruments[3].diffSets[k].starScoreLevels = new uint[6];
                for (int i = 0; i < 5; i++)
                    ret.instruments[3].diffSets[k].starScoreLevels[i] = reader.ReadUInt32();
            }


            //drums
            ret.instruments[2] = new SongData.SongDataInstrument();
            ret.instruments[2].instrumentType = "SET";
            ret.instruments[2].rpPhrases = new SongData.RockPowerPhrase[reader.ReadUInt32()];
            for (int i = 0; i < ret.instruments[2].rpPhrases.Length; i++)
                ret.instruments[2].rpPhrases[i] = new SongData.RockPowerPhrase(reader.ReadUInt32(), reader.ReadUInt32());
            ret.instruments[2].fills = new SongData.Fill[reader.ReadUInt32()];
            for (int i = 0; i < ret.instruments[2].fills.Length; i++)
                ret.instruments[2].fills[i] = new SongData.Fill(reader.ReadUInt32(), reader.ReadUInt32());
            ret.instruments[2].diffSets = new SongData.DifficultySet[4];
            for (int ir = 0; ir < 4; ir++)
            {
                uint k = reader.ReadUInt32();
                ret.instruments[2].diffSets[k] = new SongData.DifficultySet();
                ret.instruments[2].diffSets[k].phrases = new SongData.Phrase[1];
                ret.instruments[2].diffSets[k].phrases[0] = new SongData.Phrase();
                ret.instruments[2].diffSets[k].phrases[0].notes = new SongData.NoteSet[reader.ReadUInt32()];
                for (int i = 0; i < ret.instruments[2].diffSets[k].phrases[0].notes.Length; i++)
                {
                    SongData.NoteSet note = new SongData.NoteSet();
                    byte type = reader.ReadByte();
                    byte ntp = (byte)(type & 0x0E);
                    if ((type & 0x10) != 0)
                        ntp |= 0x01;
                    if ((type & 0x01) != 0)
                        ntp |= 0x10;
                    if ((ntp & 0x01) != 0)
                        ntp |= 0x20;
                    ntp >>= 1;
                    note.type = ntp;
                    note.time = reader.ReadUInt32();
                    ret.instruments[2].diffSets[k].phrases[0].notes[i] = note;
                }
                ret.instruments[2].diffSets[k].starScoreLevels = new uint[6];
                for (int i = 0; i < 5; i++)
                    ret.instruments[2].diffSets[k].starScoreLevels[i] = reader.ReadUInt32();
            }


            //vocals
            ret.instruments[1] = new SongData.SongDataInstrument();
            ret.instruments[1].instrumentType = "LVX";
            ret.instruments[1].diffSets = new SongData.DifficultySet[4];
            for(int i=0;i<4;i++)
                ret.instruments[1].diffSets[i] = new SongData.DifficultySet();
            ret.instruments[1].diffSets[3].phrases = new SongData.Phrase[reader.ReadUInt32()];
            for (int j = 0; j < ret.instruments[1].diffSets[3].phrases.Length; j++)
            {
                ret.instruments[1].diffSets[3].phrases[j] = new SongData.Phrase();
                ret.instruments[1].diffSets[3].phrases[j].type = (SongData.TYPE)reader.ReadByte();
                ret.instruments[1].diffSets[3].phrases[j].rockpower = reader.ReadBoolean();
                ret.instruments[1].diffSets[3].phrases[j].notes = new SongData.NoteSet[reader.ReadUInt32()];
                if (ret.instruments[1].diffSets[3].phrases[j].type == SongData.TYPE.REGULAR)
                {
                    for (int i = 0; i < ret.instruments[1].diffSets[3].phrases[i].notes.Length; i++)
                    {
                        SongData.NoteSet note = new SongData.NoteSet();
                        note.time = reader.ReadUInt32();
                        note.length = reader.ReadUInt32()-note.time;
                        note.type = reader.ReadUInt16();
                        note.endtype = reader.ReadUInt16();
                        note.text = reader.ReadString();
                        ret.instruments[1].diffSets[3].phrases[j].notes[i] = note;
                    }
                }
                else if (ret.instruments[1].diffSets[3].phrases[j].type == SongData.TYPE.RHYTHM)
                {
                    ret.instruments[1].diffSets[3].phrases[j].rType = (SongData.RTYPE)reader.ReadByte();
                    for (int i = 0; i < ret.instruments[1].diffSets[3].phrases[i].notes.Length; i++)
                    {
                        SongData.NoteSet note = new SongData.NoteSet();
                        note.time = reader.ReadUInt32();
                        ret.instruments[1].diffSets[3].phrases[j].notes[i] = note;
                    }
                }
            }
            for (int ir = 0; ir < 4; ir++)
            {
                ret.instruments[1].diffSets[ir].starScoreLevels = new uint[6];
                for (int i = 0; i < 5; i++)
                    ret.instruments[1].diffSets[ir].starScoreLevels[i] = reader.ReadUInt32();
            }


            //effects
            ret.effects.effects = new SongData.SpecialEffect[0];
            ret.effects.cameraSwitches = new uint[reader.ReadUInt32()];
            for (int i = 0; i < ret.effects.cameraSwitches.Length; i++)
                ret.effects.cameraSwitches[i] = reader.ReadUInt32();

            reader.Close();

            return ret;

        }

        public static SongData LoadSong20(String filename)
        {
            SongData ret = new SongData();
            String fn = filename;
            String dir = "";
            if (fn.Contains("\\") || fn.ToLower().EndsWith("uns"))
            {
                dir = fn.Substring(0, fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(fn.LastIndexOf('\\') + 1);
                fn = fn.Substring(0, fn.LastIndexOf('.'));
            }
            ret.info.filename = fn;

            if (!File.Exists(dir + fn + ".uns"))
            {
#if WINDOWS
                System.Windows.Forms.MessageBox.Show("songdata not found");
#endif
                return null;
            }

            BinaryReader reader = new BinaryReader(File.OpenRead("songdata\\" + fn + ".uns"));

            reader.ReadBytes(3);//UNS

            reader.ReadBytes(6 * 4);//redundant

            ret.info.version = reader.ReadByte();

            if (ret.info.version != 20)
            {
                return LoadSong17(filename);
            }

            ret.instruments = new SongData.SongDataInstrument[reader.ReadUInt32()];

            ret.info.name = reader.ReadString();

            ret.info.artist = reader.ReadString();

            ret.info.year = reader.ReadUInt32();

            ret.info.genre = reader.ReadString();

            ret.info.length = new TimeSpan(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

            for (int i = 0; i < 8; i++)
                ret.info.quotes[i] = reader.ReadString();

            ret.info.charters = new string[reader.ReadUInt32()];
            for (int i = 0; i < ret.info.charters.Length; i++)
                ret.info.charters[i] = reader.ReadString();

            ret.info.barlines = new SongData.Barline[reader.ReadUInt32()];
            for (int i = 0; i < ret.info.barlines.Length; i++)
            {
                ret.info.barlines[i].time = reader.ReadUInt32();
                ret.info.barlines[i].numBeats = reader.ReadByte();
            }

            ret.info.trailingBeatLen = reader.ReadUInt32();

            ret.info.bre = new SongData.BigRockEnding(reader.ReadByte()!=0, reader.ReadUInt32(), reader.ReadUInt32());

            ret.info.harmonies = new SongData.Harmony[reader.ReadUInt32()];

            for(int i=0;i<ret.info.harmonies.Length;i++)
            {
                ret.info.harmonies[i].start = reader.ReadUInt32();
                ret.info.harmonies[i].end = reader.ReadUInt32();
                ret.info.harmonies[i].instruments = reader.ReadUInt64();
            }

            for (int instr = 0; instr < ret.instruments.Length; instr++)
            {
                SongData.SongDataInstrument instrument = new SongData.SongDataInstrument();
                instrument.instrumentType = "";
                for (int i = 0; i < 3; i++)
                    instrument.instrumentType += reader.ReadChar();
                instrument.instrumentType = instrument.instrumentType.ToUpper();
                Instrument instrType = InstrumentMaster.GetSingleton().GetInstrument(instrument.instrumentType);
                instrument.rpPhrases = new SongData.RockPowerPhrase[reader.ReadUInt32()];
                for (int i = 0; i < instrument.rpPhrases.Length; i++)
                {
                    instrument.rpPhrases[i].time = reader.ReadUInt32();
                    instrument.rpPhrases[i].len = reader.ReadUInt32();
                }
                if (instrType.HasSolos)
                {
                    instrument.solos = new SongData.Solo[reader.ReadUInt32()];
                    for (int i = 0; i < instrument.solos.Length; i++)
                    {
                        instrument.solos[i].time = reader.ReadUInt32();
                        instrument.solos[i].len = reader.ReadUInt32();
                    }
                }
                if ((instrType.RPEnableType&Instrument.RockPowerEnableTypes.FILL)!=0)
                {
                    instrument.fills = new SongData.Fill[reader.ReadUInt32()];
                    for (int i = 0; i < instrument.fills.Length; i++)
                    {
                        instrument.fills[i].time = reader.ReadUInt32();
                        instrument.fills[i].len = reader.ReadUInt32();
                    }
                }
                instrument.diffSets = new SongData.DifficultySet[reader.ReadUInt32()];
                for (int ds = 0; ds < instrument.diffSets.Length; ds++)
                {
                    instrument.diffSets[ds] = new SongData.DifficultySet();
                    instrument.diffSets[ds].diff = reader.ReadByte();
                    instrument.diffSets[ds].phrases = new SongData.Phrase[reader.ReadUInt32()];
                    for (int ph = 0; ph < instrument.diffSets[ds].phrases.Length; ph++)
                    {
                        if(instrType.TypesOfPhrases != Instrument.PhraseType.NONE)
                        {
                            instrument.diffSets[ds].phrases[ph].time = reader.ReadUInt32();
                            instrument.diffSets[ds].phrases[ph].type = (SongData.TYPE)reader.ReadByte();
                            instrument.diffSets[ds].phrases[ph].rockpower = reader.ReadByte()!=0;
                            if((instrType.TypesOfPhrases&Instrument.PhraseType.RHYTHM)!=0)
                                instrument.diffSets[ds].phrases[ph].rType = (SongData.RTYPE)reader.ReadByte();
                        }
                        instrument.diffSets[ds].phrases[ph].notes = new SongData.NoteSet[reader.ReadUInt32()];
                        for (int nt = 0; nt < instrument.diffSets[ds].phrases[ph].notes.Length; nt++)
                        {
                            instrument.diffSets[ds].phrases[ph].notes[nt] = new SongData.NoteSet();
                            instrument.diffSets[ds].phrases[ph].notes[nt].type = reader.ReadUInt64();
                            if (instrType.PitchShifts)
                                instrument.diffSets[ds].phrases[ph].notes[nt].endtype = reader.ReadUInt64();
                            instrument.diffSets[ds].phrases[ph].notes[nt].time = reader.ReadUInt32();
                            if (instrType.ContainsHeldNotes)
                                instrument.diffSets[ds].phrases[ph].notes[nt].length = reader.ReadUInt32();
                            if (instrType.ContainsText)
                                instrument.diffSets[ds].phrases[ph].notes[nt].text = reader.ReadString();
                        }
                    }
                    instrument.diffSets[ds].starScoreLevels = new uint[6];
                    for (int i = 0; i < 6; i++)
                        instrument.diffSets[ds].starScoreLevels[i] = reader.ReadUInt32();
                }
                ret.instruments[instr] = instrument;
            }

            ret.effects.cameraSwitches = new uint[reader.ReadUInt32()];
            for (int i = 0; i < ret.effects.cameraSwitches.Length; i++)
                ret.effects.cameraSwitches[i] = reader.ReadUInt32();
            ret.effects.effects = new SongData.SpecialEffect[reader.ReadUInt32()];
            for (int i = 0; i < ret.effects.effects.Length; i++)
            {
                uint time = reader.ReadUInt32();
                char[] tp = reader.ReadChars(2);
                uint len = reader.ReadUInt32();
                if (tp[0] == 'l' && tp[1] == 'n')
                {
                    SongData.NormalLightingSpecialEffect ef = new SongData.NormalLightingSpecialEffect(time, len, new SongData.Color(255, 255, 255));
                    uint color = reader.ReadUInt32();
                    ef.color = new SongData.Color((byte)(color & 0x000000FF), (byte)((color & 0x0000FF00) >> 8), (byte)((color & 0x00FF0000) >> 16), (byte)((color & 0xFF000000) >> 24));
                }
            }
            return ret;
        }

        public static SongData LoadSong(String filename)
        {
            if (filename.ToLower().EndsWith(".uns"))
                return LoadSong20(filename);
            else if (filename.Substring(0,filename.Length-1).ToLower().EndsWith(".gb"))
                return LoadSong12(filename);
            return null;
        }

        public static void SaveSong(SongData songdata, String filename)
        {
            BinaryWriter writer = null;
            try
            {
                writer = new BinaryWriter(File.OpenWrite(filename));

                writer.Write('U');
                writer.Write('N');
                writer.Write('S');

                for (int i = 0; i < 6 * 4; i++) writer.Write('\0');

                writer.Write((byte)20);

                writer.Write((uint)songdata.instruments.Length);

                writer.Write(songdata.info.name);
                writer.Write(songdata.info.artist);
                writer.Write((uint)songdata.info.year);
                writer.Write(songdata.info.genre);
                writer.Write((byte)songdata.info.length.Hours);
                writer.Write((byte)songdata.info.length.Minutes);
                writer.Write((byte)songdata.info.length.Seconds);

                for (int i = 0; i < 8; i++)
                    writer.Write(songdata.info.quotes[i] == null ? "" : songdata.info.quotes[i]);

                if (songdata.info.charters == null)
                { writer.Write((uint)0); }
                else
                {
                    writer.Write((uint)songdata.info.charters.Length);
                    for (int i = 0; i < songdata.info.charters.Length; i++)
                        writer.Write(songdata.info.charters[i]);
                }

                writer.Write((uint)songdata.info.barlines.Length);
                for (int i = 0; i < songdata.info.barlines.Length; i++)
                {
                    writer.Write((uint)songdata.info.barlines[i].time);
                    writer.Write((byte)songdata.info.barlines[i].numBeats);
                }

                writer.Write((uint)songdata.info.trailingBeatLen);

                writer.Write((bool)songdata.info.bre.enabled);
                writer.Write((uint)songdata.info.bre.start);
                writer.Write((uint)songdata.info.bre.end);

                writer.Write((uint)songdata.info.harmonies.Length);
                for (int i = 0; i < songdata.info.harmonies.Length; i++)
                {
                    writer.Write((uint)songdata.info.harmonies[i].start);
                    writer.Write((uint)songdata.info.harmonies[i].end);
                    writer.Write((ulong)songdata.info.harmonies[i].instruments);
                }

                for (int instr = 0; instr < songdata.instruments.Length; instr++)
                {
                    SongData.SongDataInstrument instrument = songdata.instruments[instr];

                    for (int i = 0; i < 3; i++)
                        writer.Write((char)instrument.instrumentType.ToCharArray()[i]);

                    writer.Write(instrument.rpPhrases.Length);
                    for (int i = 0; i < instrument.rpPhrases.Length; i++)
                    {
                        writer.Write((uint)instrument.rpPhrases[i].time);
                        writer.Write((uint)instrument.rpPhrases[i].len);
                    }

                    Instrument theType = InstrumentMaster.GetSingleton().GetInstrument(instrument.instrumentType);
                    if (theType == null)
                    {
                        throw new InvalidOperationException("Error! Cannot find " + instrument.instrumentType + " instrument file!\nSaving Failed!\nPlease place the instrument file in the correct folder,\nreload instruments, and try to save again.");
                    }

                    if (theType.HasSolos)
                    {
                        writer.Write((uint)instrument.solos.Length);
                        for (int i = 0; i < instrument.solos.Length; i++)
                        {
                            writer.Write((uint)instrument.solos[i].time);
                            writer.Write((uint)instrument.solos[i].len);
                        }
                    }

                    if ((theType.RPEnableType & Instrument.RockPowerEnableTypes.FILL) != 0)
                    {
                        writer.Write((uint)instrument.fills.Length);
                        for (int i = 0; i < instrument.fills.Length; i++)
                        {
                            writer.Write((uint)instrument.fills[i].time);
                            writer.Write((uint)instrument.fills[i].len);
                        }
                    }

                    writer.Write((uint)instrument.diffSets.Length);

                    for (int dsi = 0; dsi < instrument.diffSets.Length; dsi++)
                    {
                        SongData.DifficultySet set = instrument.diffSets[dsi];

                        writer.Write((byte)set.diff);

                        if (set.phrases == null)
                        {
                            writer.Write((uint)0);
                        }
                        else
                        {
                            writer.Write((uint)set.phrases.Length);

                            for (int pi = 0; pi < set.phrases.Length; pi++)
                            {
                                if (theType.TypesOfPhrases != Instrument.PhraseType.NONE)
                                {
                                    writer.Write((uint)set.phrases[pi].time);
                                    writer.Write((byte)set.phrases[pi].type);
                                    writer.Write((bool)set.phrases[pi].rockpower);
                                    if ((theType.TypesOfPhrases & Instrument.PhraseType.RHYTHM) != 0)
                                        writer.Write((byte)set.phrases[pi].rType);
                                }

                                writer.Write((uint)set.phrases[pi].notes.Length);

                                for (int ni = 0; ni < set.phrases[pi].notes.Length; ni++)
                                {
                                    writer.Write((ulong)set.phrases[pi].notes[ni].type);
                                    if (theType.PitchShifts)
                                        writer.Write((ulong)set.phrases[pi].notes[ni].endtype);
                                    writer.Write((uint)set.phrases[pi].notes[ni].time);
                                    if (theType.ContainsHeldNotes)
                                        writer.Write((uint)set.phrases[pi].notes[ni].length);
                                    if (theType.ContainsText)
                                        writer.Write(set.phrases[pi].notes[ni].text);
                                }
                            }
                        }

                        for (int i = 0; i < 6; i++)
                            writer.Write((uint)set.starScoreLevels[i]);
                    }
                }

                writer.Write((uint)songdata.effects.cameraSwitches.Length);
                for (int i = 0; i < songdata.effects.cameraSwitches.Length; i++)
                    writer.Write((uint)songdata.effects.cameraSwitches[i]);

                writer.Write((uint)songdata.effects.effects.Length);
                for (int ei = 0; ei < songdata.effects.effects.Length; ei++)
                {
                    SongData.SpecialEffect effect = songdata.effects.effects[ei];
                    
                    writer.Write((uint)effect.time);
                    writer.Write((char)effect.type.ToCharArray()[0]);
                    writer.Write((char)effect.type.ToCharArray()[1]);
                    writer.Write((uint)effect.length);

                    if (effect is SongData.NormalLightingSpecialEffect)
                    {
                        writer.Write((byte)((effect as SongData.NormalLightingSpecialEffect).color.R));
                        writer.Write((byte)((effect as SongData.NormalLightingSpecialEffect).color.G));
                        writer.Write((byte)((effect as SongData.NormalLightingSpecialEffect).color.B));
                        writer.Write((byte)((effect as SongData.NormalLightingSpecialEffect).color.A));
                    }
                }
            }
            finally
            {
                if(writer!=null)
                    writer.Close();
            }
        }
    }
}
