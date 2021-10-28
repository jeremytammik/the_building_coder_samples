#region Header

//
// CmdSetRoomOccupancy.cs - read and set room occupancy
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

//using Autodesk.Revit.Collections;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSetRoomOccupancy : IExternalCommand
    {
        private static char[] _digits;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var rooms = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                rooms, uidoc, typeof(Room)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some room elements."
                    : "No room elements found.";
                return Result.Failed;
            }

            using var t = new Transaction(doc);
            t.Start("Bump Room Occupancy");

            foreach (Room room in rooms) BumpOccupancy(room);
            t.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Analyse the given string.
        ///     If it ends in a sequence of digits representing a number,
        ///     return a string with the number oincremented by one.
        ///     Otherwise, return a string with a suffix "1" appended.
        /// </summary>
        private static string BumpStringSuffix(string s)
        {
            if (null == s || 0 == s.Length) return "1";
            if (null == _digits)
                _digits = new[]
                {
                    '0', '1', '2', '3', '4',
                    '5', '6', '7', '8', '9'
                };
            var n = s.Length;
            var t = s.TrimEnd(_digits);
            if (t.Length == n)
            {
                t += "1";
            }
            else
            {
                n = t.Length;
                n = int.Parse(s.Substring(n));
                ++n;
                t += n.ToString();
            }

            return t;
        }

        /// <summary>
        ///     Read the value of the element ROOM_OCCUPANCY parameter.
        ///     If it ends in a number, increment the number, else append "1".
        /// </summary>
        private static void BumpOccupancy(Element e)
        {
            var p = e.get_Parameter(
                BuiltInParameter.ROOM_OCCUPANCY);

            if (null == p)
            {
                Debug.Print(
                    "{0} has no room occupancy parameter.",
                    Util.ElementDescription(e));
            }
            else
            {
                var occupancy = p.AsString();

                var newOccupancy = BumpStringSuffix(
                    occupancy);

                p.Set(newOccupancy);
            }
        }
    }
}