#region Header

//
// CmdRoomWallAdjacency.cs - determine part
// of wall face area that bounds a room.
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

// todo: report and resolve this, this should not be required: 'RE: ambiguous BoundarySegmentArrayArray'
//using BoundarySegmentArrayArray = Autodesk.Revit.DB.Architecture.BoundarySegmentArrayArray; // 2011
//using BoundarySegmentArray = Autodesk.Revit.DB.Architecture.BoundarySegmentArray; // 2011
//using BoundarySegment = Autodesk.Revit.DB.Architecture.BoundarySegment; // 2011

// 2012

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Determine part of wall face area that bounds a room.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdRoomWallAdjacency : IExternalCommand
    {
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

            foreach (Room room in rooms)
                DetermineAdjacentElementLengthsAndWallAreas(
                    room);
            return Result.Failed;
        }
        // Originally implemented by Richard @RPThomas108 Thomas in VB.NET in
        // https://forums.autodesk.com/t5/revit-api-forum/extract-the-names-of-the-rooms-separated-by-a-wall/m-p/10428696

        /// <summary>
        ///     For all rooms, determine all adjacent walls,
        ///     create dictionary mapping walls to adjacent rooms,
        ///     and tag the walls with the adjacent room names.
        /// </summary>
        private void TagWallsWithAdjacentRooms(Document doc)
        {
            var rooms
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Rooms);

            var map_wall_to_rooms
                = new Dictionary<ElementId, List<string>>();

            var opts
                = new SpatialElementBoundaryOptions();

            foreach (Room room in rooms)
            {
                var loops
                    = room.GetBoundarySegments(opts);

                foreach (var loop in loops)
                foreach (var seg in loop)
                {
                    var idWall = seg.ElementId;

                    if (ElementId.InvalidElementId != idWall)
                    {
                        if (!map_wall_to_rooms.ContainsKey(idWall))
                            map_wall_to_rooms.Add(
                                idWall, new List<string>());

                        var room_name = room.Name;

                        if (!map_wall_to_rooms[idWall].Contains(room_name)) map_wall_to_rooms[idWall].Add(room_name);
                    }
                }
            }

            using var tx = new Transaction(doc);
            tx.Start("Add list of adjacent rooms to wall comments");

            var ids
                = map_wall_to_rooms.Keys;

            foreach (var id in ids)
            {
                var wall = doc.GetElement(id);

                var p = wall.get_Parameter(
                    BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                if (null != p)
                {
                    var s = string.Join(" / ",
                        map_wall_to_rooms[id]);

                    p.Set(s);
                }
            }

            tx.Commit();
        }

        private void DetermineAdjacentElementLengthsAndWallAreas(
            Room room)
        {
            var doc = room.Document;

            // 'Autodesk.Revit.DB.Architecture.Room.Boundary' is obsolete:
            // use GetBoundarySegments(SpatialElementBoundaryOptions) instead.

            //BoundarySegmentArrayArray boundaries = room.Boundary; // 2011

            var boundaries
                = room.GetBoundarySegments(
                    new SpatialElementBoundaryOptions()); // 2012

            // a room may have a null boundary property:

            var n = 0;

            if (null != boundaries)
                //n = boundaries.Size; // 2011
                n = boundaries.Count; // 2012

            Debug.Print(
                "{0} has {1} boundar{2}{3}",
                Util.ElementDescription(room),
                n, Util.PluralSuffixY(n),
                Util.DotOrColon(n));

            if (0 < n)
            {
                int iBoundary = 0, iSegment;

                //foreach( BoundarySegmentArray b in boundaries ) // 2011
                foreach (var b in boundaries) // 2012
                {
                    ++iBoundary;
                    iSegment = 0;
                    foreach (var s in b)
                    {
                        ++iSegment;

                        //Element neighbour = s.Element; // 2015
                        var neighbour = doc.GetElement(s.ElementId); // 2016

                        //Curve curve = s.Curve; // 2015
                        var curve = s.GetCurve(); // 2016

                        var length = curve.Length;

                        Debug.Print(
                            "  Neighbour {0}:{1} {2} has {3}"
                            + " feet adjacent to room.",
                            iBoundary, iSegment,
                            Util.ElementDescription(neighbour),
                            Util.RealString(length));

                        if (neighbour is Wall wall)
                        {
                            var p = wall.get_Parameter(
                                BuiltInParameter.HOST_AREA_COMPUTED);

                            var area = p.AsDouble();

                            var lc
                                = wall.Location as LocationCurve;

                            var wallLength = lc.Curve.Length;

                            //Level bottomLevel = wall.Level; // 2013
                            var bottomLevel = doc.GetElement(wall.LevelId) as Level; // 2014
                            var bottomElevation = bottomLevel.Elevation;
                            var topElevation = bottomElevation;

                            p = wall.get_Parameter(
                                BuiltInParameter.WALL_HEIGHT_TYPE);

                            if (null != p)
                            {
                                var id = p.AsElementId();
                                var topLevel = doc.GetElement(id) as Level;
                                topElevation = topLevel.Elevation;
                            }

                            var height = topElevation - bottomElevation;

                            Debug.Print(
                                "    This wall has a total length,"
                                + " height and area of {0} feet,"
                                + " {1} feet and {2} square feet.",
                                Util.RealString(wallLength),
                                Util.RealString(height),
                                Util.RealString(area));
                        }
                    }
                }
            }
        }
    }
}