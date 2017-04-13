using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

/*
 * TaskManager
 * 
 * Always pulls first task in list, moves completed tasks to back of list.
 * 
 * Nate Crandall
 * 2009/04/19
 * 
 */ 

namespace RPGPlugin
{
    internal class TaskManager
    {
        private LinkedList<Task> tasks;
        private Task currentTask;
        private Texture2D completedTexture;
        private Texture2D incompleteTexture;

        public TaskManager() {
            this.tasks = new LinkedList<Task>();
        }

        public Texture2D CompletedTexture
        {
            set { completedTexture = value; }
            get { return completedTexture; }
        }

        public Texture2D IncompleteTexture
        {
            set { incompleteTexture = value; }
            get { return incompleteTexture; }
        }

        /*
         * Adds Task to list
         */
        public bool AddTask(Task t) {
            if (tasks.First == null) {
                currentTask = t;
            }

            tasks.AddLast(t);
            return true;
        }

        /*
         * Returns name of the current task
         */
        public string CurrentTaskName {
            get {
                return currentTask.Name;
            }
        }

        public Task CurrentTask
        {
            get { return currentTask; }
        }

        /*
         * Remove Task by task name 
         */
        public bool RemoveTask(string taskName) {
            foreach (Task t in tasks) {
                if (t.Name.Equals(taskName)) {

                    if (t.Equals(currentTask)) {
                        next(false);
                    } else {
                        tasks.Remove(t);
                    }
                    return true;
                }
            }
            return false;
        }

        /*
         * Remove Task by task obj
         */
        public bool RemoveTask(Task t) {
            return RemoveTask(t.Name);
        }

        /*
         * Remove current Task
         */
        public bool RemoveTask() {
            return RemoveTask(currentTask.Name);
        }

        /*
         * Return percentage of current task complete.
         * If task is complete, moves next task to current task.
         */
        public bool TaskComplete() {
            if (currentTask != null) {
                if (currentTask.IsComplete) {
                    return NextTask();
                }
            }

            return false;
        }

        /*
         * Number of Tasks
         */ 
        public int TaskCount() {
            return tasks.Count;
        }

        /*
         * Skip current task, move to next
         */
        public bool NextTask() {
            return next(true);
        }


        /*
        * Skip current task, move to next
        */
        public void Update()
        {
            foreach (Task t in tasks)
            {
                t.UpdateTask();
            }
        }

        /*
         * Utility method to move to next task. 
         */
        private bool next(bool keepCurrent) {
            if (tasks.Last != tasks.First) {
                if (keepCurrent) {
                    tasks.AddLast(currentTask);
                }

                tasks.RemoveFirst();

                currentTask = tasks.First.Value;
                return true;
            }
            return false;
        }

        private bool checkComplete(string taskName)
        {
            if (taskName.Equals("None")) 
            {
                return true;
            }

            foreach (Task t in tasks)
            {
                if(t.Name.Equals(taskName))
                {
                    return t.IsComplete;
                }
            }

            // If for some strange reason we can't find the task, let them play
            return true;
        }

        public void StartTasks()
        {
            foreach (Task task in tasks)
            {
                if (!task.IsComplete)
                {
                    if(checkComplete(task.Depends))
                    {
                        task.RunTask();
                    }
                }
            }
        }

        public void EndTasks()
        {
            foreach (Task task in tasks)
            {
                if (task.Running)
                {
                    task.EndTask();
                }
            }
        }

        public void CancelTasks()
        {
            foreach (Task task in tasks)
            {
                if (task.Running)
                {
                    task.CancelTask();
                }
            }
        }

        /*
         * Returns the tasks LinkedList
         */
        public LinkedList<Task> getTasks() {
            return tasks;
        }

        public bool DrawTaskDisplayInfo(SpriteFont font, SpriteBatch spriteBatch, int x, int y)
        {
            bool result = true;
            spriteBatch.DrawString(font, "Tasks:", new Vector2(390, y), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
            spriteBatch.DrawString(font, "Tasks:", new Vector2(391, (y + 1)), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

            y += 50;

            String status;
            foreach (Task t in tasks)
            {
                spriteBatch.DrawString(font, t.Name, new Vector2(x, y), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                spriteBatch.DrawString(font, t.Name, new Vector2((x + 1), (y + 1)), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);

                if(t.IsComplete)
                {
                    status = "Complete";
                }
                else
                {
                    status = "Incomplete";
                    if(result)
                    {
                        result = false;
                    }
                }

                spriteBatch.DrawString(font, status, new Vector2(x + 350, y), Color.Yellow, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                spriteBatch.DrawString(font, status, new Vector2((x + 350 + 1), (y + 1)), Color.Black, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0F);
                y += 25;
            }
            return result;
        }

    }
}
