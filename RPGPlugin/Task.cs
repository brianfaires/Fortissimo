using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Defines a Task
 * 
 * Nate Crandall
 * 2009/04/19
 * 
 */

namespace RPGPlugin
{
    public abstract class Task
    {
        protected int attributeInt;
        private bool isComplete;
        private string name;
        protected RPGPlayer player;
        private bool running;
        internal string Depends;

        public Task(string name, RPGPlayer player)
        {
            this.name = name;
            this.player = player;
            this.Depends = "None";
            this.isComplete = false;
            this.running = false;
        }

        public abstract bool EndTask();

        /*
         * Returns a description of the Task 
         */
        public abstract string GetDescription();

        /*
         * Returns true if task is complete
         */
        public bool IsComplete
        {
            get
            {
                return isComplete;
            }
            set
            {
                isComplete = value;
            }
        }

        /*
         * Returns name/short description of Task 
         */
        public string Name
        {
            get
            {
                return name;
            }
        }

        public abstract bool RunTask();

        public abstract void UpdateTask();

        public void CancelTask()
        {
            this.running = false;
        }


        public bool Running
        {
            get
            {
                return running;
            }

            set
            {
                running = value;
            }
        }


        /*
         * Flexibility to add extra info to a task
         * 
         * As int
         */
        public void SetAttribute(int number)
        {
            attributeInt = number;
        }
    }
}
