//
//  TaskTree.cs
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
using System.Diagnostics;
using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace LoganekPlaner
{
    public delegate bool ProcessTaskFunc (Task task);

    public class TaskTree
    {
        [UI] readonly CheckButton showDoneCheckButton;
        [UI] readonly TreeView tasksTreeView;
        [UI] readonly Button searchTaskButton;
        [UI] readonly Entry searchTaskEntry;

        readonly ListStore tasksList = new ListStore (typeof(Task), typeof(bool), typeof(string), typeof(string), typeof(string));
        TreeModelFilter filter;
        ProcessTaskFunc currentFilterFunc;

        public event EventHandler<Task> SelectedTaskChanged;

        public TaskTree (Builder builder)
        {
            builder.Autoconnect (this);

            filter = new TreeModelFilter (tasksList, null);
            filter.VisibleFunc = TasksTreeFilterFunc;
            tasksTreeView.Model = filter;

            tasksTreeView.Selection.Changed += TasksTreeView_Selection_Changed;

            showDoneCheckButton.Toggled += (sender, e) => filter.Refilter ();

            searchTaskButton.Clicked += (sender, e) => filter.Refilter ();

            searchTaskEntry.Activated += (sender, e) => filter.Refilter ();

            var toggleCell = new CellRendererToggle ();
            toggleCell.Toggled += ToggleCell_Toggled;
            AddColumn ("", toggleCell, null, new object[]{ "active", 1 });

            AddColumn ("Title", new CellRendererText (), RenderCell, new Object [] { "text", 2 });
            AddColumn ("Deadline", new CellRendererText (), RenderCell, new Object [] { "text", 3 });
            AddColumn ("Priority", new CellRendererText (), RenderCell, new Object [] { "text", 4 });

            TaskManager.Instance.TaskChanged += TaskChanged;
        }

        void ToggleCell_Toggled (object o, ToggledArgs args)
        {
            TreeIter iter;
            if (filter.GetIterFromString (out iter, args.Path)) {
                var t = (Task)filter.GetValue (iter, 0);
                t.IsDone = !t.IsDone;
                tasksList.SetValue (FindTask (t, tasksList), 1, t.IsDone); // NotImplementedException workaround
                TaskManager.Instance.AddTask (t);
            }
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

        void TasksTreeView_Selection_Changed (object o, EventArgs args)
        {
            TreeIter iter;
            tasksTreeView.Selection.GetSelected (out iter);

            if (SelectedTaskChanged != null) {
                SelectedTaskChanged (this, iter.UserData == IntPtr.Zero ? null : (Task)filter.GetValue (iter, 0));
            }
        }

        public void SetFilterFunc (ProcessTaskFunc func)
        {
            currentFilterFunc = func;
            filter.Refilter ();
        }

        bool TasksTreeFilterFunc (ITreeModel model, TreeIter iter)
        {
            var task = model.GetValue (iter, 0) as Task;
            if (task == null) {
                return true;
            }

            bool ok = !task.IsDone || showDoneCheckButton.Active;

            if (currentFilterFunc != null)
                ok &= currentFilterFunc (task);

            string searchText = searchTaskEntry.Text;
            if (!string.IsNullOrEmpty (searchText)) {
                ok &= task.Description.IndexOf (searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                task.Title.IndexOf (searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return ok;
        }

        static TreeIter FindTask (Task task, ITreeModel model)
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

        void RenderCell (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter)
        {
            var textCell = (cell as CellRendererText);
            Debug.Assert (cell != null);
            var task = (Task)model.GetValue (iter, 0);

            System.Action ResetColors = () => textCell.Background = "white";

            textCell.Strikethrough = task.IsDone;

            if (task.IsDone || !task.Deadline.HasValue) {
                ResetColors ();
            } else {
                var now = DateTime.Now.Date;
                var threshold = task.Deadline.Value.AddDays (-1);

                if (now > task.Deadline.Value) {
                    textCell.Background = "red";
                } else if (now > threshold) {
                    textCell.Background = "lightgreen";
                } else {
                    ResetColors ();
                } 
            }

            int columnNr = Array.IndexOf (tasksTreeView.Columns, column) + 1; // first column is not visible
            textCell.Text = (string) model.GetValue (iter, columnNr);
            tasksTreeView.QueueDraw ();
        }

        public void UpdateTask (Task task)
        {
            TreeIter iter = FindTask (task, tasksList);
            if (iter.Equals (TreeIter.Zero)) {
                return;
            }

            if (tasksList.GetValue (iter, 0) == task) {
                tasksList.SetValue (iter, 1, task.IsDone);
                tasksList.SetValue (iter, 2, task.Title);
                tasksList.SetValue (iter, 3, 
                    task.Deadline.HasValue ? task.Deadline.Value.ToShortDateString () : "infinity");
                tasksList.SetValue (iter, 4, task.Priority.ToString ());
            }
        }

        void AppendTask (Task task)
        {
            tasksList.AppendValues (task, task.IsDone, task.Title, task.Deadline.HasValue ? task.Deadline.Value.ToShortDateString () : "infinity", task.Priority.ToString ());
            var iter = FindTask (task, filter);

            if (!iter.Equals (TreeIter.Zero)) {
                tasksTreeView.Selection.SelectIter (iter);
            } else {
                tasksTreeView.Selection.UnselectAll ();
            }
        }

        void RemoveTask (Task task)
        {
            TreeIter iter = FindTask (task, tasksList);

            if (iter.Equals (TreeIter.Zero)) {
                return;
            }

            tasksList.Remove (ref iter);
            tasksTreeView.Selection.UnselectAll ();
        }
    }
}

