using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGPlugin
{
    public class ExperienceManager
    {
        private int level;
        private int skillPoints;
        private long experience;
        private long nextLevelExperience = 10000;
        private double levelUpModifier = 0.50;

        public ExperienceManager()
        {
            this.level = 1;
            this.experience = 0;
        }

        public ExperienceManager(int level, long experience)
        {
            this.level = level;
            this.experience = experience;
            initNextLevelExperience(level);
        }

        public void addExperience(long amount)
        {
            // Modified to allow multiple level ups.
            this.experience += amount;
            while (this.experience > nextLevelExperience)
            {
                this.experience -= nextLevelExperience;
                this.levelUp();
            }

            //Don't allow xp lower than 0
            if (this.experience < 0)
            {
                this.experience = 0;
            }
        }

        //Sets up the nextLevelExperience variable.
        private void initNextLevelExperience(int level)
        {
            for (int i = 1; i < level; i++)
            {
                nextLevelExperience = nextLevelExperience + (long)(nextLevelExperience * levelUpModifier);
            }
        }

        private void levelUp()
        {
            level++;
            increaseSkillPoints();
            nextLevelExperience = nextLevelExperience + (long)(nextLevelExperience * levelUpModifier);
        }

        private void increaseSkillPoints()
        {
            //TODO:  Add possible skill modifier for skill points earned.
            skillPoints++;
        }

        public int Level
        {
            get
            {
                return this.level;
            }
        }

        public long Experience
        {
            get
            {
                return this.experience;
            }
        }

        public int SkillPoints
        {
            get
            {
                return this.skillPoints;
            }
            set
            {
                this.skillPoints = value;
            }
        }
    }
}
