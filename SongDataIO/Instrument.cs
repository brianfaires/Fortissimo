using System;
using System.Collections.Generic;
using System.Text;

namespace SongDataIO
{
    public class Instrument
    {
        //Rock Power Enable Types
        public enum RockPowerEnableTypes
        {
            SELECT = 1, // select button or guitar tilt
            FILL = 2, // same as HMX Rock Band drum fills

            // mix types.  users can use either type
            SELECT_FILL = 3,
        };

        public enum BoardDimensions
        {
            TWO_DIMENSIONAL = 2,
            THREE_DIMENSIONAL = 3,
        };

        public enum PhraseType
        {
            NONE = 0,

            BLANK = 1,
            REGULAR = 2,
            RHYTHM = 4,

            BLANK_REGULAR = 3,
            REGULAR_RHYTHM = 6,
            BLANK_RHYTHM = 5,

            BLANK_REGULAR_RHYTHM = 7,
        };

        // number of visible chord/note tracks (in RB, 5 for G/B and 4 for drums)
        public int NumDrawnTracks;


        // number of total chord/note tracks (in RB, 5 for G/B/D because Drum bass counts as a track)
        // if this is more than NumDrawnTracks, any others are represented as bars
        public int NumTracks;

        // how this instrument activates star power
        public RockPowerEnableTypes RPEnableType;

        // whether the board is 2d or 3d
        public BoardDimensions Dimensions;

        // Code name is 3-length string
        // full name is the name of the instrument in plain english
        public String CodeName, FullName;

        // whether notes have a length property
        public bool ContainsHeldNotes;

        // whether held notes can be pitch modulated by a whammy bar
        // irrelevant if !ContainsHeldNotes
        public bool CanWhammy;

        // whether <8th notes can be hit based on GH rules
        public bool CanHOPO;

        // whether the instrument supports solos
        public bool HasSolos;

        // whether a note can end on a different pitch than it started on
        public bool PitchShifts;

        // whether or not the note contains text
        public bool ContainsText;

        // what kinds of phrases are available
        public PhraseType TypesOfPhrases;

        // whether you need input other than frets to hit notes
        public bool NeedsStrum;

        // the max (non-RP) multiplier (default 4)
        public int MaxMultiplier;

        // what 5+ multipliers are called (e.g. "Bass Groove")
        public String OverMultiplier;

        // which notes, when hit, cause the board to bump. bitwise
        public ulong BumpNotes;

        // for instruments with fret boards that are out of order.  default is value=index
        public int[] colorIndices;

        // if true, only one diffset is defined in the file.
        public bool DiffSame;

        public Instrument()
        {
            colorIndices = new int[32];
            for (int i = 0; i < colorIndices.Length; i++)
                colorIndices[i] = i;
        }

        public void SetValue(String variable, String value)
        {
            if (variable.ToLower().Trim().Equals("numtracks"))
                NumTracks = Int32.Parse(value);
            else if (variable.ToLower().Trim().Equals("numdrawntracks"))
                NumDrawnTracks = Int32.Parse(value);
            else if (variable.ToLower().Trim().Equals("rpenabletype"))
                RPEnableType = (RockPowerEnableTypes)Enum.Parse(typeof(RockPowerEnableTypes), value.Trim().ToUpper());
            else if (variable.ToLower().Trim().Equals("dimensions"))
                Dimensions = (BoardDimensions)Enum.Parse(typeof(BoardDimensions), value.Trim().ToUpper());
            else if (variable.ToLower().Trim().Equals("codename"))
                CodeName = value;
            else if (variable.ToLower().Trim().Equals("fullname"))
                FullName = value;
            else if (variable.ToLower().Trim().Equals("containsheldnotes"))
                ContainsHeldNotes = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("canwhammy"))
                CanWhammy = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("canhopo"))
                CanHOPO = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("hassolos"))
                HasSolos = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("pitchshifts"))
                PitchShifts = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("containstext"))
                ContainsText = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("typesofphrase"))
                TypesOfPhrases = (PhraseType)Enum.Parse(Type.GetType("PhraseType"), value.Trim().ToUpper());
            else if (variable.ToLower().Trim().Equals("needsstrum"))
                NeedsStrum = Boolean.Parse(value);
            else if (variable.ToLower().Trim().Equals("maxmultiplier"))
                MaxMultiplier = Int32.Parse(value);
            else if (variable.ToLower().Trim().Equals("overmultiplier"))
                OverMultiplier = value;
            else if (variable.ToLower().Trim().Equals("bumpnotes"))
            {
                BumpNotes = 0;
                value = value.Trim();
                while (value.Length > 0)
                {
                    if (value.IndexOf(',') < 0)
                    {
                        BumpNotes |= (((ulong)1) << Int32.Parse(value.Trim()));
                        break;
                    }
                    else
                    {
                        BumpNotes |= (((ulong)1) << Int32.Parse(value.Substring(0, value.IndexOf(',')).Trim()));
                        value = value.Substring(value.IndexOf(',') + 1).Trim();
                    }
                }
            }
            else if (variable.ToLower().Trim().Equals("colors"))
            {
                value = value.Trim();
                int index = 0; ;
                while (value.Length > 0)
                {
                    if (value.IndexOf(',') < 0)
                        break;
                    colorIndices[index] = Int32.Parse(value.Substring(0, value.IndexOf(',')).Trim());
                    value = value.Substring(value.IndexOf(',') + 1).Trim();
                    index++;
                }
            }
            else if (variable.ToLower().Trim().Equals("diffsame"))
                DiffSame = Boolean.Parse(value);
            else
                throw new InvalidOperationException("Invalid Instrument Variable Name: " + variable);

        }
    }
}
