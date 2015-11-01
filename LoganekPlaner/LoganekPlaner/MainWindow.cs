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
    class MainWindow : Window
    {
        [UI] readonly RadioToolButton allTasksToolButton;
        [UI] readonly RadioToolButton todaysTasksToolButton;
        [UI] readonly RadioToolButton thisWeekToolButton;
        [UI] readonly RadioToolButton expiredToolButton;
        [UI] readonly RadioToolButton noDeadlineToolButton;
        [UI] readonly Button saveToFileButton;
        [UI] readonly Button addNewTaskButton;
        [UI] readonly Button removeDoneButton;

        readonly TaskEditor taskEditor;
        readonly TaskTree taskTree;

        public MainWindow (Builder builder) : base (builder.GetObject ("mainWindow").Handle)
        {
            builder.Autoconnect (this);

            taskEditor = new TaskEditor (builder);
            taskTree = new TaskTree (builder);

            taskTree.SelectedTaskChanged += (sender, e) => {
                taskEditor.SetCurrentTask (e);
                taskEditor.LoadTask ();
            };

            Destroyed += (sender, e) => Application.Quit ();

            DeleteEvent += (o, args) => {
                if (TaskManager.Instance.ModelModified) {
                    switch (UiUtils.ShowYesNoCancelDialog (this, "Save changes?")) {
                    case ResponseType.Yes:
                        TaskManager.Instance.SaveModel ();
                        break;
                    case ResponseType.Cancel:
                        args.RetVal = true;
                        break;
                    }
                }
            };

            removeDoneButton.Clicked += (sender, e) => TaskManager.Instance.RemoveTasks (task => task.IsDone);

            addNewTaskButton.Clicked += AddNewTaskButton_Clicked;

            saveToFileButton.Clicked += (sender, e) => TaskManager.Instance.SaveModel ();

            TaskManager.Instance.ModelStateModified += TaskManager_Instance_ModelStateModified;

            taskEditor.SetCurrentTask (null);

            RadioToolButton[] filterButtons = {
                allTasksToolButton,
                todaysTasksToolButton,
                thisWeekToolButton,
                expiredToolButton,
                noDeadlineToolButton
            };

            ProcessTaskFunc[] filterFuncs = {
                task => true,
                task => task.Deadline.HasValue && task.Deadline.Value.Date == DateTime.Now.Date,
                task => task.Deadline.HasValue && task.Deadline.Value.Date >= DateTime.Now.Date && task.Deadline.Value.Date < DateTime.Now.Date.AddDays (7),
                task => task.Deadline.HasValue && task.Deadline.Value.Date < DateTime.Now.Date,
                task => !task.Deadline.HasValue
            };

            foreach (var btn in filterButtons) {
                btn.Toggled += (sender, e) => {
                    if ((sender as RadioToolButton).Active) {
                        int index = Array.IndexOf (filterButtons, sender);
                        taskTree.SetFilterFunc (filterFuncs [index]);
                    }
                };
            }

            TaskManager.Instance.LoadModel ();
        }

        void TaskManager_Instance_ModelStateModified (object sender, ModelStateEventArgs e)
        {
            saveToFileButton.Label = "Save to file";

            if (e.Modified) {
                saveToFileButton.Label += "*";
            }
        }

        void AddNewTaskButton_Clicked (object sender, EventArgs args)
        {
            taskEditor.SetCurrentTask (null);
            taskEditor.LoadTask ();
        }
    }
}

