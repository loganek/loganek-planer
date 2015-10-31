//
//  XMLDataStorage.cs
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
using System.Data;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace LoganekPlaner
{
    public class XMLDataStorage : IDataStorage
    {
        string path;

        public XMLDataStorage()
        {
            path = CreatePath ();
        }

        #region IDataStorage implementation

        public void SaveTasks (IEnumerable<Task> tasks)
        {
            DataSet ds = new DataSet ();
            var dt = new DataTable ();

            dt.Columns.Add (new DataColumn ("Task_Deadline", Type.GetType ("System.DateTime")));
            dt.Columns.Add (new DataColumn ("Task_CreateDate", Type.GetType ("System.DateTime")));
            dt.Columns.Add (new DataColumn ("Task_Title", Type.GetType ("System.String")));
            dt.Columns.Add (new DataColumn ("Task_Description", Type.GetType ("System.String")));
            dt.Columns.Add (new DataColumn ("Task_Priority", Type.GetType ("LoganekPlaner.Priority")));
            dt.Columns.Add (new DataColumn ("Task_IsDone", Type.GetType ("System.Boolean")));
            ds.Tables.Add (dt);
            ds.Tables [0].TableName = "product";

            foreach (var task in tasks) {
                DataRow dr;
                dr = dt.NewRow ();

                if (task.Deadline.HasValue) {
                    dr ["Task_Deadline"] = task.Deadline;
                } else {
                    dr ["Task_Deadline"] = DBNull.Value;
                }

                dr ["Task_CreateDate"] = task.CreateDate;
                dr ["Task_Title"] = task.Title;
                dr ["Task_Description"] = task.Description;
                dr ["Task_Priority"] = task.Priority;
                dr ["Task_IsDone"] = task.IsDone;
                dt.Rows.Add (dr);
            }

            StreamWriter serialWriter;
            serialWriter = new StreamWriter (path);
            XmlSerializer xmlWriter = new XmlSerializer (ds.GetType ());
            xmlWriter.Serialize (serialWriter, ds);
            serialWriter.Close ();
            ds.Clear ();
        }

        public IEnumerable<Task> LoadTasks ()
        {
            XmlSerializer xmlSerializer = new XmlSerializer (typeof(DataSet));
            DataSet ds;
            var tasks = new List<Task> ();

            try {
                FileStream readStream = new FileStream (path, FileMode.Open);
                ds = (DataSet)xmlSerializer.Deserialize (readStream);
                readStream.Close ();
            } catch (IOException) {
                return tasks;
            }

            foreach (var row in ds.Tables [0].AsEnumerable ()) {
                var task = new Task {
                    CreateDate = DateTime.Parse (row ["Task_CreateDate"].ToString ()),
                    Description = row ["Task_Description"].ToString (),
                    Title = row ["Task_Title"].ToString (),
                    IsDone = Boolean.Parse (row ["Task_IsDone"].ToString ()),
                    Priority = (Priority) Int32.Parse (row ["Task_Priority"].ToString ())
                };
                if (row ["Task_Deadline"] is DBNull) {
                    task.Deadline = null;
                } else {
                    task.Deadline = DateTime.Parse(row ["Task_Deadline"].ToString ());
                }
                tasks.Add (task);
            }

            return tasks;
        }

        #endregion

        string CreatePath ()
        {
            string dir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "loganek-planer");
            bool exists = Directory.Exists (dir);

            if (!exists)
                Directory.CreateDirectory (dir);

            return Path.Combine (dir, "data.xml");
        }
    }
}

