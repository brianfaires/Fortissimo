using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fortissimo;
using Microsoft.Xna.Framework;

namespace RPGPlugin
{
    public delegate void SkillUsedEventHandler(object sender, EventArgs e);
    public abstract class Skill
    {
        private string name;
        protected bool active = false;
        public bool Active { get { return active; } }
        protected bool onGoing;
        public bool OnGoing { get { return onGoing; } }
        protected RPGPlayer player;
        protected int level;
        protected int maxLevel;
        protected double modifierScale = 0.2;
        protected int timesUsed = 0;

        public event SkillUsedEventHandler SkillUsed;

        

        public Skill(string name, RPGPlayer player)
        {
            this.name = name;
            this.player = player;
            this.level = 0;
            this.maxLevel = 3;
        }

        public virtual bool RunSkill() { if ( SkillUsed != null ) SkillUsed(this, new EventArgs()); return false; }

        public virtual bool UpdateSkill(GameTime gameTime) { return false; }

        public virtual bool EndSkill() { return false; }

        public bool AdvanceSkillLevel()
        {
            if (level == maxLevel) return false;
            level++;
            return true;
        }

        public bool Available()
        {
            // If it depends on any other skills being present...
            return true;
        }

        public int Cost
        {
            // This should grow according to the level...?
            get { return 1; }
        }

        protected double calculateModifier()
        {
            return 1.0 + modifierScale * level;
        }

        public string Name { get { return name; } }
        public int Level { get { return level; } }
        public int MaxLevel { get { return maxLevel; } }
        public int TimesUsed { get { return timesUsed; } }
    }
}
