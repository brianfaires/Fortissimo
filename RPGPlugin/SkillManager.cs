using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RPGPlugin
{
    internal class SkillManager
    {
        private Dictionary<string, Skill> allSkills;
        private Dictionary<string, Texture2D> skillTextures;
        private Dictionary<string, Skill> ownedSkills;
        public delegate bool SkillFunction();
        public delegate bool OngoingFunction(GameTime gameTime);
        private OngoingFunction ongoing;

        private List<Texture2D> activeSkillTextures;
        public List<Texture2D> ActiveSkillTextures
        {
            get
            {
                return activeSkillTextures;
            }
        }

        public Dictionary<string, Texture2D> SkillTextures
        {
            get
            {
                return skillTextures;
            }
        }

        public SkillManager()
        {
            allSkills = new Dictionary<string, Skill>();
            ownedSkills = new Dictionary<string, Skill>();
            skillTextures = new Dictionary<string, Texture2D>();
            activeSkillTextures = new List<Texture2D>();
        }

        public void addPossibleSkill(Skill skill)
        {
            allSkills.Add(skill.Name, skill);
        }

        public bool addSkillTexture(string skill, Texture2D texture)
        {
            try
            {
                skillTextures.Add(skill, texture);
                return true;
            }
            catch (Exception) { return false; }
        }

        public bool gainSkill(string skill)
        {
            try
            {
                ownedSkills.Add(skill, allSkills[skill]);
                return true;
            }
            catch (Exception) { return false; }
        }

        public bool gainSkill(Skill skill)
        {
            try
            {
                Type t = skill.GetType();
                ownedSkills.Add(t.ToString(), allSkills[t.ToString()]);
                return true;
            }
            catch (Exception) { return false; }
        }

        public bool removeSkill(string skill)
        {
            try
            {
                ownedSkills.Remove(skill);
                return true;
            }
            catch (Exception) { return false; }
        }

        public bool removeSkill(Skill skill)
        {
            try
            {
                Type t = skill.GetType();
                ownedSkills.Remove(skill.ToString());
                return true;
            }
            catch (Exception) { return false; }
        }

        public SkillFunction getSkillBegin(string name)
        {
            try
            {
                activeSkillTextures.Add(skillTextures[name]);
            }
            catch (Exception) { return null; }

            try
            {
                return allSkills[name].RunSkill;
            }
            catch (Exception) { return null; }
        }

        public SkillFunction getSkillEnd(string name)
        {
            try
            {
                activeSkillTextures.Remove(skillTextures[name]);
            }
            catch (Exception) { return null; }

            try
            {
                return allSkills[name].EndSkill;
            }
            catch (Exception) { return null; }
        }

        public bool addOngoingSkill(string name) 
        {
            if (ownedSkills.ContainsKey(name))
            {
                ongoing += ownedSkills[name].UpdateSkill;
                ownedSkills[name].RunSkill();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool endOngoingSkill(string name)
        {
            if (ownedSkills.ContainsKey(name))
            {
                ongoing -= ownedSkills[name].UpdateSkill;
                ownedSkills[name].EndSkill();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UpdateOngoingSkills(GameTime gameTime)
        {
            if ( ongoing != null )
                ongoing(gameTime);
        }
        
        public int NumSkills { get { return ownedSkills.Count; } }
        public int TotalSkills { get { return allSkills.Count; } }
        public Dictionary<string, Skill> Skills { get { return ownedSkills; } }
        public Dictionary<string, Skill> AllSkills { get { return allSkills; } }
    }
}
