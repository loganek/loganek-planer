//
//  TaskEditor.cs
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
    public class TaskEditor
    {
        [UI] readonly ComboBoxText priorityComboBoxText;
        [UI] readonly Button deadlineButton;
        [UI] readonly CheckButton noDeadlineCheckButton;
        [UI] readonly TextView descriptionTextView;
        [UI] readonly Entry taskTitleEntry;
        [UI] readonly Button saveTaskButton;
        [UI] readonly Button removeTaskButton;

        readonly Calendar calendar = new Calendar ();

        Task currentTask;

        public void SetCurrentTask (Task task)
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

        public TaskEditor (Builder builder)
        {
            builder.Autoconnect (this);

            deadlineButton.Clicked += DeadlineButton_Clicked;;
            deadlineButton.Label = DateTime.Now.ToShortDateString ();

            noDeadlineCheckButton.Toggled += (sender, e) => deadlineButton.Sensitive = !noDeadlineCheckButton.Active;

            removeTaskButton.Clicked += RemoveTaskButton_Clicked;

            saveTaskButton.Clicked += SaveTaskButton_Clicked;

            InitPriorityComboBox ();
        }

        void SaveTaskButton_Clicked (object sender, EventArgs e)
        {
            if (currentTask == null) {
                SetCurrentTask (new Task ());
            }
            if (string.IsNullOrEmpty (taskTitleEntry.Text)) {
                var dialog = new MessageDialog (null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Title cannot be empty");
                dialog.Run ();
                dialog.Destroy ();
                return;
            }

            Priority priority;

            if (!Enum.TryParse (priorityComboBoxText.ActiveText, out priority)) {
                // TODO show error?
                return;
            }

            if (noDeadlineCheckButton.Active) {
                currentTask.Deadline = null;
            } else {
                currentTask.Deadline = calendar.Date;
            }
            currentTask.CreateDate = DateTime.Now;
            currentTask.Description = descriptionTextView.Buffer.Text;
            currentTask.Priority = priority;
            currentTask.Title = taskTitleEntry.Text;

            TaskManager.Instance.AddTask (currentTask);
        }

        void RemoveTaskButton_Clicked (object sender, EventArgs e)
        {
            if (currentTask != null) {
                TaskManager.Instance.RemoveTask (currentTask);
            }
        }

        void DeadlineButton_Clicked (object sender, EventArgs e)
        {
            var dialog = new Dialog ("Sample", null, DialogFlags.DestroyWithParent);
            dialog.Modal = true;
            dialog.AddButton ("Cancel", ResponseType.Cancel);
            dialog.AddButton ("OK", ResponseType.Ok);
            dialog.ContentArea.Add (calendar);
            calendar.Show ();
            if (dialog.Run () == (int)ResponseType.Ok) {
                deadlineButton.Label = calendar.Date.ToShortDateString ();
            }
            dialog.Destroy ();
        }

        void InitPriorityComboBox ()
        {
            Array priorityValues = Enum.GetValues (typeof(Priority));
            foreach (var priority in priorityValues) {
                priorityComboBoxText.AppendText (priority.ToString ());
            }

            if (priorityValues.Length > 0) {
                priorityComboBoxText.Active = 0;
            }
        }

        public void LoadTask ()
        {
            if (currentTask == null) {
                calendar.Date = DateTime.Now;
                taskTitleEntry.Text = string.Empty;
                descriptionTextView.Buffer.Text = string.Empty;
                priorityComboBoxText.Active = 0;
            } else {
                if (currentTask.Deadline.HasValue) {
                    calendar.Date = currentTask.Deadline.Value;
                    noDeadlineCheckButton.Active = false;
                } else {
                    noDeadlineCheckButton.Active = true;
                }
                taskTitleEntry.Text = currentTask.Title;
                descriptionTextView.Buffer.Text = currentTask.Description;
                priorityComboBoxText.Active = (int)currentTask.Priority;
            }

            taskTitleEntry.GrabFocus ();
            deadlineButton.Label = calendar.Date.ToShortDateString ();
        }
    }
}

