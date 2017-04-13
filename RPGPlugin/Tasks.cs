using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fortissimo;

namespace RPGPlugin
{
    class Tasks
    {

        public class KingOfRock : Task
        {
            String[] DifficultyString = { "Easy", "Medium", "Hard", "Expert" };

            public KingOfRock(RPGPlayer player)
                : base("The King of Rock", player)
            {
                Depends = "Nuthin\' but a score Thang";
                attributeInt = 2;
                IsComplete = false;
            }

            public override bool RunTask()
            {
                if (!IsComplete)
                {
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    if (attributeInt == ((RhythmGame)player.Game).Difficulty)
                    {
                        IsComplete = true;
                    }
                    Running = false;
                }
                
                
                return IsComplete;
            }

            public override void UpdateTask()
            {
                // Nothing, check at end.
            }

            public override string GetDescription()
            {
                if (attributeInt > (DifficultyString.Length - 1)) 
                {
                    attributeInt = (DifficultyString.Length - 1);
                }

                return "Complete a song on " + DifficultyString[attributeInt] + " difficulty setting";
            }
        }



        /// <summary>
        /// Score 90% or higher on any song
        /// </summary>
        public class ScoreThang : Task
        {
            public ScoreThang(RPGPlayer player)
                : base("Nuthin\' but a score Thang", player)
            {
                Depends = "Dream On";
                attributeInt = 90;
                IsComplete = false;
            }

            public override bool RunTask()
            {
                if (!IsComplete)
                {
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    if (player.NotesHit + player.NotesMissed > 0)
                    {
                        if ((player.NotesHit / (player.NotesHit + player.NotesMissed)) >= attributeInt)
                        {
                            IsComplete = true;
                        }
                    }
                    Running = false;
                }
                return IsComplete;
            }

            public override void UpdateTask()
            {
                // Nothing, check at end.
            }

            public override string GetDescription()
            {
                return "Get over " + attributeInt + " percent on a song";
            }
        }

        /// <summary>
        /// Hit 45 notes or more in a row.
        /// </summary>
        public class MusicTalking : Task
        {
            public MusicTalking(RPGPlayer player)
                : base("Let The Music Do The Talking", player)
            {
                Depends = "Get Funkadelic";
                attributeInt = 45;
                IsComplete = false;
            }

            public override bool RunTask()
            {
                if (!IsComplete)
                {
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    if (player.NoteStreak > attributeInt)
                    {
                        IsComplete = true;
                    }
                    Running = false;
                }
                return IsComplete;
            }

            public override void UpdateTask()
            {
                if(Running) 
                {
                    if (player.NoteStreak > attributeInt)
                    {
                        IsComplete = true;
                    }
                }
            }

            public override string GetDescription()
            {
                return "Hit " + attributeInt + " notes in a row.";
            }
        }



        public class DreamOn : Task
        {
            //public SkillManager oldSkills;

            public DreamOn(RPGPlayer player)
                : base("Dream On", player)
            {
                IsComplete = false;
            }

            public override bool RunTask()
            {
                //oldSkills = player.ReplaceSkills(new SkillManager());
                if (!IsComplete)
                {
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    //player.ReplaceSkills(oldSkills);

                    //if (player.Health > 0)
                    //{
                    //    IsComplete = true;
                    //}

                }
                Running = false;
                return IsComplete;
            }

            public override void UpdateTask()
            {
                // Nothing, check at end
            }

            public override string GetDescription()
            {
                return "Pass song without using any skills";
            }
        }



        public class GoodRiddance : Task
        {

            public GoodRiddance(RPGPlayer player)
                : base("Good Riddance (Score Of Your Life)", player)
            {
                Depends = "Let The Music Do The Talking";
                attributeInt = 100000;
                IsComplete = false;
            }

            public override bool RunTask()
            {
                if(!IsComplete)
                {
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    if (player.Score >= attributeInt)
                    {
                        IsComplete = true;
                    }
                    Running = false;
                }
                return IsComplete;
            }

            public override void UpdateTask()
            {
                // Nothing, check at end.
            }

            public override string GetDescription()
            {
                return "Score higher than " + attributeInt + " points on a certain song";
            }
        }



        public class Funkadelic : Task
        {
            bool[] colors = new bool[5];
            ulong[] bits = { 1, 2, 4, 8, 16 };
            string[] colorStrings = { "Green", "Red", "Yellow", "Blue", "Orange" };
            Random random;


            public Funkadelic(RPGPlayer player)
                : base("Get Funkadelic", player)
            {
                reset();
                IsComplete = false;
            }

            private void reset()
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = false;
                }
            }

            private void writeColorString(int index)
            {
                Console.WriteLine("Missed " + colorStrings[index] + " note");
            }


            public override bool RunTask()
            {
                if (!IsComplete)
                {
                    random = new Random();
                    attributeInt = random.Next(0, colors.Length);
                    Console.WriteLine("Missed Note Task Color is:" + colorStrings[attributeInt]);

                    reset();
                    Running = true;
                    return true;
                }
                return false;
            }

            public override bool EndTask()
            {
                if(Running)
                {
                    if (!(colors.Contains(true)))
                    {
                        IsComplete = true;
                    }
                    Running = false;
                }
                return IsComplete;
            }

            public override void UpdateTask()
            {
                if (Running && player.Notes != null && !(colors[attributeInt])) 
                {
                    if ((player.LastMissType & bits[attributeInt]) != 0)
                    {
                        colors[attributeInt] = true;
                        writeColorString(attributeInt);
                    }
                }
            }

            public override string GetDescription()
            {
                return "Don't miss any notes of a specific (randomly chosen) color";
            }
        }


        // Change to all songs in set completed
        public class FinalTask : Task
        {
            public FinalTask(RPGPlayer player)
                : base("Why Don\'t You Get A Job?", player)
            {
                IsComplete = false;
            }

            public override bool RunTask()
            {
                if (!IsComplete)
                {
                    int count = 0;
                    int complete = 0;
                    Dictionary<String, ScoreAndStars> d = ((RhythmGame)player.Game).CurrentBand.SongStats;

                    foreach (KeyValuePair<String, ScoreAndStars> pair in d)
                    {
                        //if(pair.Value > 0)
                        //{
                        //    complete++;
                        //}
                        //count++;
                    }

                    Console.WriteLine("Songs Left = " + (count - complete));
                    Console.WriteLine("Songs Left = " + ((RhythmGame)player.Game).CurrentBand.SongStats.Count);
                    return Running = true;
                }
                return false;
            }

            public override bool EndTask()
            {
                 if(Running)
                 {
                     
                     // Check all songs in set completed
                     Running = false;
                 }
                 return IsComplete;
            }

            public override void UpdateTask()
            {
                // Nothing, check at end
            }

            public override string GetDescription()
            {
                return "All songs in set completed";
            }

        }


    }
}
