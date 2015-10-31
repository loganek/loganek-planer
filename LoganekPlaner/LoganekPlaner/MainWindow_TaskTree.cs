//
//  MainWindow_TaskTree.cs
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
using System.Collections.Generic;
using System.Diagnostics;

namespace LoganekPlaner
{
    internal partial class MainWindow
    {
        [UI] readonly CheckButton showDoneCheckButton;
        [UI] Button addNewTaskButton;
        [UI] readonly Button removeDoneButton;
        [UI] readonly TreeView tasksTreeView;

        readonly ListStore tasksList = new ListStore (typeof(Task), typeof(bool), typeof(string), typeof(string), typeof(string));
        TreeModelFilter filter;

        void InitTaskTree()
        {
            filter = new TreeModelFilter (tasksList, null);
            filter.VisibleFunc = TasksTreeFilterFunc;
            tasksTreeView.Model = filter;
                
            tasksTreeView.Selection.Changed += TasksTreeView_Selection_Changed;

            removeDoneButton.Clicked += RemoveDoneButton_Clicked;

            showDoneCheckButton.Toggled += (sender, e) => filter.Refilter ();
        }

        void AddColumn (string title, CellRenderer cellRenderer, TreeCellDataFunc func, Array attributes)
        {
            var column = new TreeViewColumn (title, cellRenderer, attributes);

            if (func != null) {
                column.SetCellDataFunc (cellRenderer, func);
            }
            column.Clickable = true;
            column.Title = title;
            tasksTreeView.AppendColumn (column);
        }

        void TasksTreeView_Selection_Changed(object o, EventArgs args)
        {
            TreeIter iter;
            tasksTreeView.Selection.GetSelected (out iter);

            SetCurrentTask (iter.UserData == IntPtr.Zero ? null :(Task)filter.GetValue (iter, 0));
            LoadCurrentTaskToEditor ();
        }

        bool TasksTreeFilterFunc(ITreeModel model, TreeIter iter)
        {
            var task = model.GetValue (iter, 0) as Task;
            if (task == null) {
                return true;
            }

            bool ok = !task.IsDone || showDoneCheckButton.Active;

            if (currentFilterFunc != null)
                ok &= currentFilterFunc (task);

            return ok;
        }

        void RemoveDoneButton_Clicked (object sender, EventArgs e)
        {
            var tasksToRemove = new List<Task> ();
            if (UiUtils.ShowYesNoDialog (this, "Are you sure?") == ResponseType.Yes) {
                TreeIter iter;
                filter.GetIterFirst (out iter);
                if (iter.Equals (TreeIter.Zero)) {
                    return;
                }
                do {
                    var t = filter.GetValue (iter, 0) as Task;
                    if (t.IsDone)
                        tasksToRemove.Add (t);
                } while (filter.IterNext (ref iter)); 
            }

            foreach (var task in tasksToRemove) {
                TaskManager.Instance.RemoveTask (task);
            }
        }

        void InitTasksList ()
        {
            var toggleCell = new CellRendererToggle ();

            toggleCell.Toggled += (o, args) => {
                TreeIter iter;
                if (filter.GetIterFromString (out iter, args.Path)) {
                    var t = (Task)filter.GetValue (iter, 0);
                    t.IsDone = !t.IsDone;
                    tasksList.SetValue (FindTask (t, tasksList), 1, t.IsDone); // NotImplementedException workaround
                    TaskManager.Instance.AddTask (t);
                }
            };

            AddColumn ("", toggleCell, null, new object[]{ "active", 1 });
            AddColumn ("Title", new CellRendererText (), RenderCell, new Object [] { "text", 2 });
            AddColumn ("Deadline", new CellRendererText (), RenderCell, new Object [] { "text", 3 });
            AddColumn ("Priority", new CellRendererText (), RenderCell, new Object [] { "text", 4 });

            filter = new TreeModelFilter (tasksList, null);
            tasksTreeView.Model = filter;
        }

        void RenderCell (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter)
        {
            var textCell = (cell as CellRendererText);
            Debug.Assert (cell != null);

            var t = (Task)model.GetValue (iter, 0);
            int columnNr = Array.IndexOf (tasksTreeView.Columns, column) + 1; // first column is not visible

            System.Action ResetColors = () => textCell.Background = "white";

            if (t.IsDone) {
                textCell.Strikethrough = true;
                ResetColors ();
            } else {
                textCell.Strikethrough = false;
                var now = DateTime.Now.Date;
                var threshold = t.DueDate.AddDays (-1);

                if (now > t.DueDate) {
                    textCell.Background = "red";
                } else if (now > threshold) {
                    textCell.Background = "lightgreen";
                } else {
                    ResetColors ();
                }
            }
            textCell.Text = (string)model.GetValue (iter, columnNr);
            tasksTreeView.QueueDraw ();
        }

    }
}

