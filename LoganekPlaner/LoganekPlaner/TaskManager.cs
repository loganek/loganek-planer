//
//  TaskManager.cs
//
//  Author:
//       Marcin Kolny <marcin.kolny@gmail.com>
//
//  Copyright (c) 2015 Marcin Kolny
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

namespace LoganekPlaner
{
    public enum TaskStatus
    {
        Add,
        Remove,
        Update
    }

    public class TaskEventArgs : EventArgs
    {
        public TaskEventArgs (Task task, TaskStatus status)
        {
            Task = task;
            Status = status;
        }

        public Task Task { get; private set; }
        public TaskStatus Status { get; private set; }
    }

    public sealed class TaskManager
    {
        static volatile TaskManager instance;
        static readonly object sync = new Object ();

        readonly List<Task> tasks = new List<Task> ();

        public event EventHandler<TaskEventArgs> TaskChanged;

        private TaskManager ()
        {
        }

        public void AddTask (Task task)
        {
            bool taskExists = tasks.Exists (t => t == task);
            if (!taskExists) {
                tasks.Add (task);
            }
            OnTaskChanged (task, taskExists ? TaskStatus.Update : TaskStatus.Add);
        }

        public void RemoveTask (Task task)
        {
            if (tasks.Exists (t => t == task)) {
                tasks.Remove (task);
                OnTaskChanged (task, TaskStatus.Remove);
            }
        }

        private void OnTaskChanged (Task task, TaskStatus status)
        {
            if (TaskChanged != null) {
                TaskChanged (this, new TaskEventArgs (task, status));
            }
        }

        public static TaskManager Instance {
            get {
                if (instance == null) {
                    lock (sync) {
                        if (instance == null)
                            instance = new TaskManager ();
                    }
                }

                return instance;
            }
        }
    }
}

