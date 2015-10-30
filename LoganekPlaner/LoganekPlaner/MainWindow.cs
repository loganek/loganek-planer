//
//  MainWindow.cs
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
using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace LoganekPlaner
{
    public delegate bool ProcessTaskFunc (Task task);

    internal partial class MainWindow : Window
    {
        [UI] readonly RadioToolButton allTasksToolButton;
        [UI] readonly RadioToolButton todaysTasksToolButton;
        [UI] readonly RadioToolButton thisWeekToolButton;
        [UI] readonly RadioToolButton expiredToolButton;
        [UI] readonly RadioToolButton noDeadlineToolButton;

        Task currentTask;

        ProcessTaskFunc currentFilterFunc;

        public MainWindow (Builder builder) : base (builder.GetObject ("mainWindow").Handle)
        {
            builder.Autoconnect (this);

            Destroyed += (sender, e) => Application.Quit ();

            InitTasksList ();

            addNewTaskButton.Clicked += AddNewTaskButton_Clicked;

            InitTaskTree ();

            InitTaskEditor ();

            TaskManager.Instance.TaskChanged += TaskChanged;

            SetCurrentTask (null);

            RadioToolButton[] filterButtons = {
                allTasksToolButton,
                todaysTasksToolButton,
                thisWeekToolButton,
                expiredToolButton,
                noDeadlineToolButton
            };

            ProcessTaskFunc[] filterFuncs = {
                task => true,
                task => task.DueDate.Date == DateTime.Now.Date,
                task => task.DueDate.Date >= DateTime.Now.Date && task.DueDate.Date < DateTime.Now.Date.AddDays (7),
                task => task.DueDate.Date < DateTime.Now.Date,
                task => task.DueDate.Date == DateTime.Now.Date, // todo
            };

            foreach (var btn in filterButtons) {
                btn.Toggled += (sender, e) => {
                    int index = Array.IndexOf (filterButtons, sender);
                    if ((sender as RadioToolButton).Active) {
                        currentFilterFunc = filterFuncs [index];
                    }
                    filter.Refilter ();
                };
            }

            TaskManager.Instance.AddTask (new Task {
                Title = "title1",
                DueDate = DateTime.Now,
                IsDone = true,
                Description = "Description"
            });
            TaskManager.Instance.AddTask (new Task {
                Title = "title2",
                DueDate = DateTime.Now.AddDays (10),
                IsDone = false,
                Description = "Description"
            });
            TaskManager.Instance.AddTask (new Task {
                Title = "title3",
                DueDate = DateTime.Now.AddDays (1),
                IsDone = true,
                Description = "Description"
            });
            TaskManager.Instance.AddTask (new Task {
                Title = "title4",
                DueDate = DateTime.Now,
                IsDone = true,
                Description = "Description"
            });
            TaskManager.Instance.AddTask (new Task {
                Title = "title5",
                DueDate = DateTime.Now.AddDays (-2),
                IsDone = false,
                Description = "Description"
            });
            TaskManager.Instance.AddTask (new Task {
                Title = "title6",
                DueDate = DateTime.Now.AddDays (4),
                IsDone = true,
                Description = "Description"
            });
        }

        void TaskChanged (object sender, TaskEventArgs e)
        {
            switch (e.Status) {
            case TaskStatus.Update:
                UpdateTask (e.Task);
                break;
            case TaskStatus.Add:
                AppendTask (e.Task);
                break;
            case TaskStatus.Remove:
                RemoveTask (e.Task);
                break;
            }
        }

        static TreeIter FindTask (object task, ITreeModel model)
        {
            TreeIter iter;
            model.GetIterFirst (out iter);

            if (iter.Equals (TreeIter.Zero)) {
                return TreeIter.Zero;
            }

            do {
                if (model.GetValue (iter, 0) == task) {
                    return iter;
                }
            } while (model.IterNext (ref iter)); 

            return TreeIter.Zero;
        }

        void UpdateTask (Task task)
        {
            TreeIter iter = FindTask (task, tasksList);
            if (iter.Equals (TreeIter.Zero)) {
                return;
            }

            var t = (Task)tasksList.GetValue (iter, 0);
            if (t == task) {
                tasksList.SetValue (iter, 1, t.IsDone);
                tasksList.SetValue (iter, 2, t.Title);
                tasksList.SetValue (iter, 3, t.DueDate.ToShortDateString ());
                tasksList.SetValue (iter, 4, t.Priority.ToString ());
            }
        }

        void SetCurrentTask (Task task)
        {
            currentTask = task;

            if (task == null) {
                removeTaskButton.Sensitive = false;
                saveTaskButton.Label = "Add new task";
            } else {
                removeTaskButton.Sensitive = true;
                saveTaskButton.Label = "Save";
            }
        }

        void AddNewTaskButton_Clicked (object sender, EventArgs args)
        {
            SetCurrentTask (null);
            tasksTreeView.Selection.UnselectAll ();
            taskTitleEntry.GrabFocus ();
            LoadCurrentTaskToEditor ();
        }


        void AppendTask (Task task)
        {
            tasksList.AppendValues (task, task.IsDone, task.Title, task.DueDate.ToShortDateString (), task.Priority.ToString ());
            var iter = FindTask (task, filter);

            if (!iter.Equals (TreeIter.Zero)) {
                tasksTreeView.Selection.SelectIter (iter);
            } else {
                SetCurrentTask (task);
                LoadCurrentTaskToEditor ();
            }
        }

        void RemoveTask (Task task)
        {
            TreeIter iter = FindTask (task, tasksList);

            if (iter.Equals (TreeIter.Zero)) {
                return;
            }

            tasksList.Remove (ref iter);

            if (task == currentTask) {
                SetCurrentTask (null);
            }
        }
    }
}

