//
//  UiUtils.cs
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
using Gtk;

namespace LoganekPlaner
{
    public static class UiUtils
    {
        public static ResponseType ShowYesNoDialog (Window parent, string message)
        {
            var dialog = new MessageDialog (parent, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, message);
            ResponseType ret = (ResponseType) dialog.Run ();
            dialog.Destroy ();

            return ret;
        }

        public static ResponseType ShowYesNoCancelDialog (Window parent, string message)
        {
            var dialog = new MessageDialog (parent, DialogFlags.Modal, MessageType.Question, ButtonsType.None, message);
            dialog.AddButton ("Close without Saving", (int) ResponseType.No);
            dialog.AddButton ("Cancel", (int) ResponseType.Cancel);
            dialog.AddButton ("Save", (int) ResponseType.Yes);
            ResponseType ret = (ResponseType) dialog.Run ();
            dialog.Destroy ();

            return ret;
        }
    }
}

