#region Header

//
// CmdDisallowJoin.cs - allow or disallow join at wall ends
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     For case 1253888 [Allow Join / Disallow Join via Revit API].
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdDisallowJoin : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Debug.Assert(false,
                "setting the disallow join property was not possible prior to Revit 2012. "
                + "In Revit 2012, you can use the WallUtils.DisallowWallJoinAtEnd method.");

            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var s = "a wall, to retrieve its join types";

            if (Util.SelectSingleElementOfType(
                uidoc, typeof(Wall), s, false) is not Wall wall)
            {
                message = "Please select a wall.";
            }
            else
            {
                var a1 = (JoinType[]) Enum.GetValues(typeof(JoinType));
                var a = new List<JoinType>((JoinType[]) Enum.GetValues(typeof(JoinType)));
                var n = a.Count;

                var lc = wall.Location as LocationCurve;

                s = $"{Util.ElementDescription(wall)}:\n";

                /*for( int i = 0; i < 2; ++i )
                {
                  JoinType jt = lc.get_JoinType( i );
                  int j = a.IndexOf( jt ) + 1;
                  JoinType jtnew = a[ j < n ? j : 0];
                  lc.set_JoinType( j, jtnew );
                  s += string.Format(
                    "\nChanged join type at {0} from {1} to {2}.",
                    ( 0 == i ? "start" : "end" ), jt, jtnew );
                }
                // wall.Location = lc; // Property or indexer 'Autodesk.Revit.Element.Location' cannot be assigned to -- it is read only
                */

                using var t = new Transaction(doc);
                t.Start("Set Wall Join Type");

                for (var i = 0; i < 2; ++i)
                {
                    var jt = ((LocationCurve) wall.Location).get_JoinType(i);
                    var j = a.IndexOf(jt) + 1;
                    var jtnew = a[j < n ? j : 0];
                    ((LocationCurve) wall.Location).set_JoinType(j, jtnew);
                    s += $"\nChanged join type at {(0 == i ? "start" : "end")} from {jt} to {jtnew}.";
                }

                t.Commit();
            }

            Util.InfoMsg(s);
            return Result.Succeeded;
        }
    }
}