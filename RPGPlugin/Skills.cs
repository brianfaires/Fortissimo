using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fortissimo;
using SongDataIO;
using Microsoft.Xna.Framework;

namespace RPGPlugin
{
    public class BiggerHeartShapedBox : Skill
    {
        float oldMaxHealth = 0.0F;
        public BiggerHeartShapedBox(RPGPlayer player) : base("Bigger Heart Shaped Box", player)
        {
            onGoing = true;
        }

        float CalculateHealth()
        {
            // 25% more health per level.
            float percentHealth = (float) calculateModifier();
            return player.MaxHealth * percentHealth;
        }

        public override bool RunSkill()
        {
            if (oldMaxHealth != 0.0F)
                return false;
            oldMaxHealth = player.MaxHealth;
            player.MaxHealth = CalculateHealth();
            player.Health = player.MaxHealth;
            base.RunSkill();
            return true;
        }

        public override bool EndSkill()
        {
            if ( oldMaxHealth == 0.0F )
                return false;
            player.MaxHealth = oldMaxHealth;
            oldMaxHealth = 0.0f;
            if (player.Health > player.MaxHealth)
                player.Health = player.MaxHealth;
            return true;
        }
    }

    public class NumberOfTheBeast : Skill
    {
        int prevNotesHit;
        int prevNotesMissed;

        public NumberOfTheBeast(RPGPlayer player) : base("The Number of the Beast", player)
        {
            prevNotesHit = player.NotesHit;
            prevNotesMissed = player.NotesMissed;
            onGoing = true;
        }

        public override bool RunSkill()
        {
            if (player.Health <= 0)
                return false; // Dont want to resurect player and cause weirdness.
            double diff = ((player.NotesHit - prevNotesHit) - (player.NotesMissed - prevNotesMissed) * 0.5) * calculateModifier();
            player.Health += (float)diff;
            if (player.Health > player.MaxHealth)
                player.Health = player.MaxHealth;
            prevNotesHit = player.NotesHit;
            prevNotesMissed = player.NotesMissed;
            base.RunSkill();
            return true;
        }

        public override bool EndSkill()
        {
            return true;
        }
    }

    public class TurnItToEleven : Skill
    {
        float oldWorth = 0;

        public TurnItToEleven(RPGPlayer player) : base("Turn It To 11 (10% more points)", player)
        {
            onGoing = true;
        }

        float CalculateScore()
        {
            // 10% more points per level.
            float percentHealth = (float) calculateModifier();
            return player.MaxHealth * percentHealth;
        }

        public override bool RunSkill()
        {
            if (oldWorth != 0)
                return false;
            oldWorth = player.NoteWorth;
            player.MaxHealth = CalculateScore();
            base.RunSkill();
            return true;
        }

        public override bool EndSkill()
        {
            if (oldWorth == 0)
                return false;
            player.MaxHealth = oldWorth;
            oldWorth = 0;
            if (player.Health > player.MaxHealth)
                player.Health = player.MaxHealth;
            return true;
        }
    }

    /// <summary>
    /// This skill will force all notes/chords to play as long as at least n - 1 of their frets are activated.
    /// </summary>
    public class MilliVanilli : Skill
    {
        private Action<SongData.NoteSet> action;
        public MilliVanilli(RPGPlayer player) : base("Rock It Like Milli Vanilli", player)
        {
            action = new Action<SongData.NoteSet>(player_NoteWasMissed);
            onGoing = true;
        }

        /// <summary>
        /// This enables Vanilli, it depends on the NoteWasMissed event being fired.
        /// It also fires the skill used event.
        /// </summary>
        /// <returns>True, Always.</returns>
        public override bool RunSkill()
        {
            player.NoteWasMissed += action;
            base.RunSkill();
            return true;
        }

         
        /// <summary>
        /// This is the function to be evoked on a NoteWasMissed event.
        /// </summary>
        /// <param name="ns">The note set that was missed.</param> 
        void player_NoteWasMissed(SongData.NoteSet ns)
        {
           if (ns.NumMissed == 1 && (ns.NumHit + ns.NumMissed) > 1) // You made it almost there, strum that shiz, 
               ns.Strum();                                          // unless of course you suck harder than expected and missed a single note...
        }

        /// <summary>
        /// This unregisters the vanilli event.
        /// </summary>
        /// <returns></returns>
        public override bool EndSkill()
        {
            player.NoteWasMissed -= action;
            return true;
        }
    }

    public class Thunderstruck : Skill
    {
        int numberOfNotesToSwap = 0;
        int startIndex = 0;
        int endIndex = 0;

        public Thunderstruck(RPGPlayer player) : base("Thunderstruck", player)
        {
            numberOfNotesToSwap = CalculateNumberOfNotesToSwap();
        }

        int CalculateNumberOfNotesToSwap()
        {
            return (int)(15 * calculateModifier());
        }

        public override bool RunSkill()
        {
            if (startIndex == 0 && endIndex == 0 && player.Notes != null)
            {
                int songLength = player.Notes.Length;
                startIndex = player.Notes.CurrentBeginIndex; // Does this get the next note?

                if ((startIndex + numberOfNotesToSwap) > songLength)
                {
                    startIndex = songLength - numberOfNotesToSwap;
                    endIndex = songLength;
                }
                else
                {
                    endIndex = startIndex + numberOfNotesToSwap;
                }

                for (int i = startIndex; i < endIndex; i++)
                {
                    player.Notes.NoteSet[i].Strum();
                }
                base.RunSkill();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool EndSkill()
        {
            return true;
        }

        public int StartIndex
        {
            get { return startIndex; }
        }

        public int EndIndex
        {
            get { return endIndex; }
        }
    }

    /// <summary>
    /// Randomly selects a note to automatically play for the life of this instance, and then
    /// automatically plays the note anytime it comes up.
    /// </summary>
    public class ColorMeBad : Skill
    {
        private int noteId;
        private ulong note;
        private TimeSpan timeRemaining;
        public ColorMeBad(RPGPlayer player) : base("Color Me Bad", player)
        {
        }
        public TimeSpan CalculateRunningTime()
        {
            return TimeSpan.FromMilliseconds((int)(10000 * calculateModifier()));
        }
        public override bool RunSkill()
        {
            if (!active)
            {
                Random r = new Random();
                noteId = r.Next(0, 4);
                note = (ulong)(1 << noteId);
                timeRemaining = CalculateRunningTime();
                active = true;
                base.RunSkill();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool UpdateSkill(GameTime gameTime)
        {
            if ( active )
            {
                if (player.Notes.CurrentNoteSet.visible[noteId] != NoteX.VIS_STATE.INVISIBLE)
                {
                    NoteManager notes = player.Notes;
                    bool containsNote = (note & notes.CurrentNoteSet.type) == note;
                    bool validRange = notes.ValidRange();
                    if (containsNote && validRange)
                    {
                        player.HitNote(player.Notes.CurrentNoteSet);
                        player.Notes.CurrentNoteSet.exploding |= note;
                        if (noteId < player.Notes.CurrentNoteSet.visible.Length)
                            player.Notes.CurrentNoteSet.visible[noteId] = NoteX.VIS_STATE.INVISIBLE;
                    }
                }

                timeRemaining -= gameTime.ElapsedGameTime;
                active = timeRemaining > TimeSpan.Zero;
                return active;
            }
            return false;
        }

        public override bool EndSkill()
        {
            note = 99999999;
            active = false;
            return true;
        }
    }

    /// <summary>
    /// Based on the player's level, a certain number of notes will be swapped out.  This makes
    /// it easy for a player to gain quick points/xp/life.
    /// </summary>
    public class ChocolateRain : Skill
    {
        private ulong note = 0;
        int numberOfNotesToSwap = 0;
        int startIndex = 0;
        int endIndex = 0;
        public ChocolateRain(RPGPlayer player) : base("Chocolate Rain", player)
        {
            //Select note that will be used as the swap note:
            Random r = new Random();
            int temp = r.Next(0, 4);
            note = (ulong)(1 << temp);

            numberOfNotesToSwap = CalculateNumberOfNotesToSwap();
        }

        int CalculateNumberOfNotesToSwap()
        {
            return (int)(10 * calculateModifier());
        }

        public override bool RunSkill()
        {
            //Randomly select the start note and end note:
            if (startIndex == 0 && endIndex == 0 && player.Notes != null)
            {
                int songLength = player.Notes.Length;
                //Random r = new Random();
                //startIndex = r.Next(1, songLength);
                startIndex = player.Notes.CurrentBeginIndex;

                if ((startIndex + numberOfNotesToSwap) > songLength)
                {
                    startIndex = songLength - numberOfNotesToSwap;
                    endIndex = songLength;
                }
                else
                {
                    endIndex = startIndex + numberOfNotesToSwap;
                }

                for (int i = startIndex; i < endIndex; i++)
                {
                    // Hack for HOPOs...
                    bool isHopo = (player.Notes.NoteSet[i].type & 32) != 0;
                    player.Notes.NoteSet[i].type = note;
                    if (isHopo)
                        player.Notes.NoteSet[i].type |= 32;
                }
                base.RunSkill();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool EndSkill()
        {
            startIndex = 0;
            endIndex = 0;
            return true;
        }

        public int StartIndex
        {
            get { return startIndex; }
        }

        public int EndIndex
        {
            get { return endIndex; }
        }
    }

    public class GreatBallsOfFire : Skill
    {
        uint millisecondsForward = 1000;
        public GreatBallsOfFire(RPGPlayer player) : base("Great Balls of Fire", player)
        {
        }

        uint CalculateMillisecondsForward()
        {
            return (uint)(1000 + (calculateModifier() * 500));
        }

        public override bool RunSkill()
        {
            millisecondsForward = CalculateMillisecondsForward();

            NoteManager notes = player.Notes;
            SongData.NoteSet[] noteSet = notes.NoteSet;
            int idx = notes.CurrentBeginIndex;
            if ( idx >= noteSet.Length )
                return false;

            double startTime = notes.SongTime.TotalMilliseconds;
            while (idx < notes.Length)
            {
                if (noteSet[idx].time - startTime <= millisecondsForward)
                {
                    player.HitNote(noteSet[idx]);
                    for (int i = 0; i < noteSet[idx].visible.Length; i++)
                        noteSet[idx].visible[i] = SongData.NoteSet.VIS_STATE.INVISIBLE;
                    noteSet[idx].exploding = noteSet[idx].type;
                }
                else
                    break;
                idx++;
            }
            // TODO: Right now this is coded as a one time skill.  We have no
            // notion of updates for skills.  It would be nice to allow this
            // skill to be run for a certain time frame... But as designed 
            // now this isn't possible.
            base.RunSkill();
            return true;
        }

        public override bool EndSkill()
        {
            // Nothing to do, unless the above TODO is taken care of.
            return true;
        }
    }

    /// <summary>
    /// This skill makes each note you hit worth more...furthermore, the more notes you hit in a row, the more points you get.
    /// This acts seperatly from the multiplier.
    /// </summary>
    public class PickOfDestiny : Skill
    {
        private int notesHit;
        private const int baseNoteWorth = 100;
        private Action<SongData.NoteSet> hit;
        private Action<SongData.NoteSet> miss;

        public PickOfDestiny(RPGPlayer player) : base("Pick of Destiny", player)
        {
            hit = new Action<SongData.NoteSet>(player_NoteWasHit);
            miss = new Action<SongData.NoteSet>(player_NoteWasMissed);
            notesHit = 0;
            onGoing = true;
        }

        public override bool RunSkill()
        {
            player.NoteWasHit += hit;
            player.NoteWasMissed += miss;

            base.RunSkill();
            return true;
        }

        void player_NoteWasMissed(SongData.NoteSet obj)
        {
            notesHit = 0;
        }

        void player_NoteWasHit(SongData.NoteSet obj)
        {
            notesHit++;
            player.NoteWorth = (uint)(baseNoteWorth + 3 * notesHit * calculateModifier());

        }

        public override bool EndSkill()
        {
            player.NoteWasHit -= hit;
            player.NoteWasMissed -= miss;
            notesHit = 0;
            return true;
        }
    }
}
