//
//  MainWindow_TaskEditor.cs
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
    internal partial class MainWindow
    {
        [UI] readonly ComboBoxText priorityComboBoxText;
        [UI] Button deadlineButton;
        [UI] CheckButton noDeadlineCheckButton;
        [UI] readonly TextView descriptionTextView;
        [UI] readonly Entry taskTitleEntry;
        [UI] readonly Button saveTaskButton;
        [UI] readonly Button removeTaskButton;

        readonly Calendar calendar = new Calendar ();

        void InitTaskEditor ()
        {
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
                var dialog = new MessageDialog (this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Title cannot be empty");
                dialog.Run ();
                dialog.Destroy ();
                return;
            }

            Priority priority;

            if (!Enum.TryParse (priorityComboBoxText.ActiveText, out priority)) {
                // TODO show error?
                return;
            }

            currentTask.DueDate = calendar.Date;
            currentTask.CreateDate = DateTime.Now;
            currentTask.Description = descriptionTextView.Buffer.Text;
            currentTask.Priority = priority;
            currentTask.Title = taskTitleEntry.Text;
            currentTask.IsDone = false;

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
            var dialog = new Dialog ("Sample", this, DialogFlags.DestroyWithParent);
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

        void LoadCurrentTaskToEditor ()
        {
            if (currentTask == null) {
                calendar.Date = DateTime.Now;
                taskTitleEntry.Text = string.Empty;
                descriptionTextView.Buffer.Text = string.Empty;
                priorityComboBoxText.Active = 0;
            } else {
                calendar.Date = currentTask.DueDate;
                taskTitleEntry.Text = currentTask.Title;
                descriptionTextView.Buffer.Text = currentTask.Description;
                priorityComboBoxText.Active = (int)currentTask.Priority;
            }

            deadlineButton.Label = calendar.Date.ToShortDateString ();
        }
    }
}

