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
using SongDataIO;

namespace RPGPlugin
{

    public class RPGPlayer : Player
    {
        private ExperienceManager experienceManager;
        internal SkillManager skillManager;
        SoundManager soundManager;
        TaskManager taskManager;
        Queue<Skill> readiedSkills;
        RhythmGame.GameStateType state;
        RhythmGame.GameStateType oldState;
        bool tasksActive = false;
        Dictionary<string, int> readiedSkillCounts;
        Random random = new Random();
        SpriteFont menuFont;
        String time;
        bool testing = false;

        public RPGPlayer(Game game)
            : base(game)
        {
            experienceManager = new ExperienceManager();
            soundManager = new SoundManager(this, this.Game);
            readiedSkills = new Queue<Skill>();
            readiedSkillCounts = new Dictionary<string, int>();
            InitializeSkillSet();
            InitializeTasks();
            soundManager.enableSounds();
            soundManager.playSound("in");
        }

        public void InitializeSkillSet()
        {
            skillManager = new SkillManager();
            skillManager.addPossibleSkill(new BiggerHeartShapedBox(this));
            skillManager.addPossibleSkill(new NumberOfTheBeast(this));
            skillManager.addPossibleSkill(new TurnItToEleven(this));
            skillManager.addPossibleSkill(new MilliVanilli(this));
            skillManager.addPossibleSkill(new ColorMeBad(this));
            skillManager.addPossibleSkill(new ChocolateRain(this));
            skillManager.addPossibleSkill(new GreatBallsOfFire(this));
            skillManager.addPossibleSkill(new PickOfDestiny(this));
            // We're missing thunderstruck... but its broken anyway.
        }

        public void InitializeTasks()
        {
            taskManager = new TaskManager();
            taskManager.AddTask(new Tasks.KingOfRock(this));
            taskManager.AddTask(new Tasks.DreamOn(this));
            taskManager.AddTask(new Tasks.Funkadelic(this));
            taskManager.AddTask(new Tasks.GoodRiddance(this));
            taskManager.AddTask(new Tasks.MusicTalking(this));
            taskManager.AddTask(new Tasks.ScoreThang(this));

            //taskManager.AddTask(new Tasks.FinalTask(this));
        }

        internal SkillManager ReplaceSkills(SkillManager s)
        {
            SkillManager old = skillManager;
            skillManager = s;
            return old;
        }

        public override void HitNote(SongData.NoteSet note)
        {
            //TODO:  Add possible skill to increase the amount of xp earned from hitting a note correctly.
            //if (NoteWasHit != null)....for some reason this causes the build to fail, even though the same statement exists in Player.cs.....
            this.XpManager.addExperience(100L);

            base.HitNote(note);
        }

        public void ReadyRandomSkill()
        {
            Dictionary<string, Skill> skills = skillManager.Skills;
            if (skills.Count <= 0)
                return;

            int numNonOngoing = 0;
            foreach (KeyValuePair<string, Skill> pair in skills)
            {
                if (!pair.Value.OnGoing)
                    numNonOngoing++;
            }
            int skillId = random.Next(numNonOngoing);


            foreach (KeyValuePair<string, Skill> pair in skills)
            {
                if (skillId == 0 && !pair.Value.OnGoing)
                {
                    readiedSkills.Enqueue(pair.Value);
                    if (readiedSkillCounts.ContainsKey(pair.Key))
                        readiedSkillCounts[pair.Key]++;
                    else
                        readiedSkillCounts[pair.Key] = 1;
                }
                if (!pair.Value.OnGoing)
                    skillId--;
            }
        }

        int lastNotesHit = 0;
        public override void Update(GameTime gameTime)
        {
            oldState = state;
            state = ((RhythmGame)Game).State;

            if (state == RhythmGame.GameStateType.Running)
            {
                if (testing)
                {
                    ((ASDFGInput)Input).fake = testing;
                    if (Notes.IsInNotePadding(Notes.CurrentNoteSet))
                    {
                        int index = 0;
                        Keys[] k = new Keys[7];
 
                        if((Notes.CurrentNoteSet.type & 1) > 0)
                        {
                            k[index++] = Keys.A;
                        }
                        
                        if ((Notes.CurrentNoteSet.type & 2) > 0)
                        {
                            k[index++] = Keys.S;
                        }

                        if ((Notes.CurrentNoteSet.type & 4) > 0)
                        {
                            k[index++] = Keys.D;
                        }

                        if ((Notes.CurrentNoteSet.type & 8) > 0)
                        {
                            k[index++] = Keys.F;
                        }

                        if ((Notes.CurrentNoteSet.type & 16) > 0)
                        {
                            k[index++] = Keys.G;
                        }

                        if(random.Next() > 0.5)
                        {
                            k[index++] = Keys.R;
                        }

                        k[index++] = Keys.Up;

                        ((ASDFGInput)Input).UpdateFakeKeys(k);
                    }
                }
            }

            base.Update(gameTime);

            if (Input.OtherKeyPressed(OtherKeyType.Power))
            {
                ActivateSkill();
            }

            if (NotesHit % 25 == 24 && lastNotesHit != NotesHit)
            {
                lastNotesHit = NotesHit;
                ReadyRandomSkill();
            }

            Dictionary<string, Skill> skills = skillManager.Skills;
            foreach (KeyValuePair<string, Skill> pair in skills)
            {
                if (!pair.Value.Active && !pair.Value.OnGoing)
                {
                    skillManager.getSkillEnd(pair.Key)();
                }
                else if (pair.Value.Active && !pair.Value.OnGoing)
                {
                    // Another hack.
                    pair.Value.UpdateSkill(gameTime);
                }
            }

            skillManager.UpdateOngoingSkills(gameTime);

            // endTasks();
            // Tasks

            if (state == RhythmGame.GameStateType.Running)
            {
                if (!tasksActive)
                {
                    taskManager.StartTasks();
                    tasksActive = true;
                }
                taskManager.Update();
            }

            if (state != oldState)
            {
                if (state == RhythmGame.GameStateType.SongSuccess)
                {
                    taskManager.EndTasks();
                    tasksActive = false;
                } /* Don't award complete if song failed or canceled */
                else if (state == RhythmGame.GameStateType.SongFail
                 || state == RhythmGame.GameStateType.SongCancel)
                {
                    taskManager.CancelTasks();
                    tasksActive = false;
                }
            }


        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;
            int y = 160;
            List<Texture2D> textures = skillManager.ActiveSkillTextures;
            int num = textures.Count;
            int id = 1;
            spriteBatch.Begin();

            // Without this, skills are displayed OVER THE MENU SCREENS!
            if (state == RhythmGame.GameStateType.Running)
            {
                foreach (Texture2D texture in skillManager.ActiveSkillTextures)
                {
                    double truePercent = ((float)id / (float)num);
                    double percent = truePercent * 0.5 + 0.5;
                    Color pixColor = Color.White;
                    pixColor.A = (byte)(truePercent * 255);
                    spriteBatch.Draw(texture, new Vector2(650, y), null, pixColor, 0.0f, Vector2.Zero, (float)(percent * .25f), SpriteEffects.None, 1.0f);
                    y += 15;
                    id++;
                }

                time = Notes.SongTime.ToString();
                int index = (time.LastIndexOf(":") - 2);
                if ((index + 7 <= time.Length) && (index > 0))
                    spriteBatch.DrawString(menuFont, time.Substring(index, 7), new Vector2(10, 520), Color.White);

                int x = 40;
                //SkillTextures
                Dictionary<string, Skill> skills = skillManager.Skills;
                foreach (KeyValuePair<string, Skill> pair in skills)
                {
                    Color pixColor = Color.White;
                    int numTimes = 0;
                    if (!readiedSkills.Contains(pair.Value) && !pair.Value.OnGoing)
                        pixColor.A = 100;
                    else if (readiedSkills.Contains(pair.Value))
                        numTimes = readiedSkillCounts[pair.Key];
                    Texture2D texture = skillManager.SkillTextures[pair.Key];
                    spriteBatch.Draw(texture, new Vector2(x, 50), null, pixColor, 0.0f, Vector2.Zero, (float)(.15f), SpriteEffects.None, 1.0f);
                    if (!pair.Value.OnGoing)
                        spriteBatch.DrawString(spriteFont, "" + numTimes, new Vector2(x, 50), pixColor);

                    x += 95;
                }

            }
            spriteBatch.End();
        }

        public override void Reset()
        {
            base.Reset();
            // Clear out.
            Dictionary<string, Skill> skills = skillManager.Skills;
            foreach (KeyValuePair<string, Skill> pair in skills)
            {
                if (pair.Value.OnGoing)
                    skillManager.endOngoingSkill(pair.Key);
            }
        }

        public override void StartSong(RhythmGame game, GameTime gameTime, SongDataPlus dataPlus, int instrument, float difficulty)
        {
            base.StartSong(game, gameTime, dataPlus, instrument, difficulty);
            
            Dictionary<string, Skill> skills = skillManager.Skills;
            foreach (KeyValuePair<string, Skill> pair in skills)
            {
                if (pair.Value.OnGoing)
                {
                    skillManager.addOngoingSkill(pair.Key);
                }
            }
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            skillManager.addSkillTexture("Bigger Heart Shaped Box", Game.Content.Load<Texture2D>("HeartShapedBox"));
            skillManager.addSkillTexture("The Number of the Beast", Game.Content.Load<Texture2D>("NumberOfTheBeast"));
            skillManager.addSkillTexture("Turn It To 11", Game.Content.Load<Texture2D>("RockItToEleven"));
            skillManager.addSkillTexture("Thunderstruck", Game.Content.Load<Texture2D>("Thunderstruck"));
            skillManager.addSkillTexture("Chocolate Rain", Game.Content.Load<Texture2D>("ChocolateRain"));
            skillManager.addSkillTexture("Pick of Destiny", Game.Content.Load<Texture2D>("PickOfDestiny"));
            skillManager.addSkillTexture("Great Balls of Fire", Game.Content.Load<Texture2D>("GreatBallsOfFire"));
            skillManager.addSkillTexture("Color Me Bad", Game.Content.Load<Texture2D>("ColorMeBadd"));
            skillManager.addSkillTexture("Rock It Like Milli Vanilli", Game.Content.Load<Texture2D>("RockItLikeMilli"));

            taskManager.CompletedTexture = Game.Content.Load<Texture2D>("complete");
            taskManager.IncompleteTexture = Game.Content.Load<Texture2D>("incomplete");

            menuFont = Game.Content.Load<SpriteFont>("Menu/LargeMenuFont");
        }

        public void ActivateSkill()
        {
            if (readiedSkills.Count > 0)
            {
                Skill nextSkill = readiedSkills.Dequeue();
                readiedSkillCounts[nextSkill.Name]--;
                skillManager.getSkillBegin(nextSkill.Name)();
            }
        }

        internal TaskManager TaskManager
        {
            get
            {
                return taskManager;
            }
        }

        internal SkillManager SkillManager
        {
            get
            {
                return skillManager;
            }
        }

        public ExperienceManager XpManager
        {
            get
            {
                return experienceManager;
            }
        }
    }
}
