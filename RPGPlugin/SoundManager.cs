using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    class SoundManager
    {
        private Action<SongData.NoteSet> NoteMissedHandler;
        private SkillUsedEventHandler SkillUsedHandler;


        private Dictionary<string, SoundEffect> sounds;
        RPGPlayer player;
        Game g;
        ContentManager cm;
        public SoundManager(RPGPlayer player, Game g)
        {
            this.g = g;
            this.player = player;
            NoteMissedHandler = new Action<SongData.NoteSet>(player_NoteWasMissed);
            SkillUsedHandler = new SkillUsedEventHandler(player_SkillWasUsed);

            cm = new ContentManager(g.Services, @"Content");
            sounds = new Dictionary<string, SoundEffect>();
            loadSounds();
            
        }

        private bool loadSounds()
        {
            try
            {
                sounds.Add("crunch1", cm.Load<SoundEffect>("crunch1"));
                sounds.Add("crunch2", cm.Load<SoundEffect>("crunch2"));
                sounds.Add("crunch3", cm.Load<SoundEffect>("crunch3"));

                sounds.Add("fiba1", cm.Load<SoundEffect>("fiba1"));
                sounds.Add("fiba2", cm.Load<SoundEffect>("fiba2"));
                sounds.Add("fiba3", cm.Load<SoundEffect>("fiba3"));
                sounds.Add("fiba4", cm.Load<SoundEffect>("fiba4"));
                sounds.Add("fiba5", cm.Load<SoundEffect>("fiba5"));
                sounds.Add("fiba6", cm.Load<SoundEffect>("fiba6"));

                sounds.Add("in", cm.Load<SoundEffect>("in"));
                sounds.Add("out", cm.Load<SoundEffect>("out"));

                sounds.Add("menu", cm.Load<SoundEffect>("menu"));

                sounds.Add("myhero", cm.Load<SoundEffect>("myhero"));

                sounds.Add("perfect1", cm.Load<SoundEffect>("perfect1"));
                sounds.Add("perfect2", cm.Load<SoundEffect>("perfect2"));
                sounds.Add("perfect3", cm.Load<SoundEffect>("perfect3"));

                sounds.Add("start", cm.Load<SoundEffect>("start"));

                return true;
            }
            catch (Exception e) { Console.WriteLine(e); return false; }
        }

        void player_NoteWasMissed(SongData.NoteSet obj)
        {
            Console.WriteLine("NoteWasMissed");
            sounds["fiba6"].Play(.25f, 0, 0); // 4.0change
        }

        void player_SkillWasUsed(object sender, EventArgs e)
        {
            Console.WriteLine("Skill was Used");
            sounds["in"].Play();
        }


        public void enableSounds()
        {
            Console.WriteLine("Sounds Enabled");
            player.NoteWasMissed += NoteMissedHandler;
            foreach (Skill s in player.SkillManager.AllSkills.Values)
            {
                s.SkillUsed += SkillUsedHandler;
            }
        }

        public void disableSounds()
        {
            player.NoteWasMissed -= NoteMissedHandler;
            foreach (Skill s in player.SkillManager.AllSkills.Values)
            {
                s.SkillUsed -= SkillUsedHandler;
            }
        }

        public void playSound(string soundName)
        {
            try
            {
                sounds[soundName].Play();
            }
            catch (Exception e) { Console.WriteLine(e); }
        }
        // Example
        //ContentManager cm = new ContentManager(game.Services, @"Content\");
        //SoundEffect se;
        //String sn = "perfect1";
        //se = cm.Load<SoundEffect>(sn);
        //se.Play();
    }
}
