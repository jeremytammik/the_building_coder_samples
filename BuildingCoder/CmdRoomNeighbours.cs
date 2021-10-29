#region Header

//
// CmdRoomNeighbours.cs - determine neighbouring room at midpoint of each room boundary segment
//
// Copyright (C) 2013-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdRoomNeighbours : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            // Interactively select elements of type Room,
            // either via pre-selection before launching the
            // command, or interactively via post-selection.

            var selector
                = new JtSelectorMulti<Room>(
                    uidoc, BuiltInCategory.OST_Rooms, "room",
                    e => e is Room);

            if (selector.IsEmpty) return selector.ShowResult();

            var rooms = selector.Selected;

            var msg = new List<string>();

            var n = rooms.Count;

            msg.Add($"{n} room{Util.PluralSuffix(n)} selected{Util.DotOrColon(n)}\r\n");

            var opt
                = new SpatialElementBoundaryOptions();

            IList<IList<BoundarySegment>> loops;

            Room neighbour;
            int i = 0, j, k;

            foreach (var room in rooms)
            {
                ++i;

                loops = room.GetBoundarySegments(opt);

                n = loops.Count;

                msg.Add($"{i}. {Util.ElementDescription(room)} has {n} loop{Util.PluralSuffix(n)}{Util.DotOrColon(n)}");

                j = 0;

                foreach (var loop in loops)
                {
                    ++j;

                    n = loop.Count;

                    msg.Add($"  {j}. Loop has {n} boundary segment{Util.PluralSuffix(n)}{Util.DotOrColon(n)}");

                    k = 0;

                    foreach (var seg in loop)
                    {
                        ++k;

                        neighbour = GetRoomNeighbourAt(seg, room);

                        msg.Add($"    {k}. Boundary segment has neighbour {(null == neighbour ? "<nil>" : Util.ElementDescription(neighbour))}");
                    }
                }
            }

            Util.InfoMsg2("Room Neighbours",
                string.Join("\n", msg.ToArray()));

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return the neighbouring room to the given one
        ///     on the other side of the midpoint of the given
        ///     boundary segment.
        /// </summary>
        private Room GetRoomNeighbourAt(
            BoundarySegment bs,
            Room r)
        {
            var doc = r.Document;

            //Wall w = bs.Element as Wall;// 2015
            var w = doc.GetElement(bs.ElementId) as Wall; // 2016

            var wallThickness = w.Width;

            var wallLength = (w.Location as
                LocationCurve).Curve.Length;

            var derivatives = bs.GetCurve()
                .ComputeDerivatives(0.5, true);

            var midPoint = derivatives.Origin;

            Debug.Assert(
                midPoint.IsAlmostEqualTo(
                    bs.GetCurve().Evaluate(0.5, true)),
                "expected same result from Evaluate and derivatives");

            var tangent = derivatives.BasisX.Normalize();

            var normal = new XYZ(tangent.Y,
                tangent.X * -1, tangent.Z);

            var p = midPoint + wallThickness * normal;

            var otherRoom = doc.GetRoomAtPoint(p);

            if (null != otherRoom)
                if (otherRoom.Id == r.Id)
                {
                    normal = new XYZ(tangent.Y * -1,
                        tangent.X, tangent.Z);

                    p = midPoint + wallThickness * normal;

                    otherRoom = doc.GetRoomAtPoint(p);

                    Debug.Assert(null == otherRoom
                                 || otherRoom.Id != r.Id,
                        "expected different room on other side");
                }

            return otherRoom;
        }
    }
}