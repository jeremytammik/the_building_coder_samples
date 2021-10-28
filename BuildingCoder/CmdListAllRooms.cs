#region Header

//
// CmdListAllRooms.cs - list properties from all rooms
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListAllRooms : IExternalCommand
    {
        public enum RoomState
        {
            Unknown,
            Placed,
            NotPlaced,
            NotEnclosed,
            Redundant
        }

        /// <summary>
        ///     Create a comma-delimited CSV file instead of
        ///     a human readable text file?
        /// </summary>
        private const bool _exportCsv = true;

        /// <summary>
        ///     CSV export headers
        /// </summary>
        private const string _csv_headers = "Room nr;Name;Center;"
                                            + "Lower left;Boundary;Convex hull;Bounding box;"
                                            + "Area in sq ft";

        /// <summary>
        ///     Export format string
        /// </summary>
        private const string _format_string = _exportCsv
            ? "{0},{1},{2},{3},{4},{5},{6},{7}"
            : "Room nr. '{0}' named '{1}' at {2} with "
              + "lower left corner {3}, "
              + "boundary points ({4}), convex hull ({5}), "
              + "bounding box {6} and area {7} sqf has "
              + "{8} loop{9} and {10} segment{11} in first "
              + "loop.";

        /// <summary>
        ///     Room boundary tolerance for considering
        ///     slightly differing boundary points equal
        /// </summary>
        private static readonly double _tolerance
            = Util.MmToFoot(1.2);

        /// <summary>
        ///     Export in millimetres instead of imperial feet?
        /// </summary>
        private static readonly bool _exportInMillimetres = true;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // Filtering for Room elements throws an exception:
            // Input type is of an element type that exists in
            // the API, but not in Revit's native object model.
            // Try using Autodesk.Revit.DB.SpatialElement
            // instead, and then postprocessing the results to
            // find the elements of interest.

            //FilteredElementCollector a
            //  = new FilteredElementCollector( doc )
            //    .OfClass( typeof( Room ) );

            // Solution using SpatialElement and then
            // checking for Room type

            //FilteredElementCollector a
            //  = new FilteredElementCollector( doc )
            //    .OfClass( typeof( SpatialElement ) );

            //foreach( SpatialElement e in a )
            //{
            //  Room room = e as Room;

            //  if( null != room )
            //  {
            //    ListRoomData( room );
            //  }
            //}

            // Improvement suggested by 
            // Victor Chekalin using LINQ

            // http://thebuildingcoder.typepad.com/blog/2011/11/accessing-room-data.html
            // ?cid=6a00e553e168978833017c3690489f970b#comment-6a00e553e168978833017c3690489f970b
            // --> version 2013.0.100.2

            //FilteredElementCollector collector
            //  = new FilteredElementCollector( doc );

            //var rooms = collector
            //  .OfClass( typeof( SpatialElement ) )
            //  .OfType<Room>();

            //FilteredElementCollector collector
            //  = new FilteredElementCollector( doc );

            //var rooms = collector
            //  .OfClass( typeof( SpatialElement ) )
            //  .OfType<Room>();

            var collector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(SpatialElement));

            var rooms = collector
                .Where(e => e is Room);

            if (_exportCsv) Debug.Print(_csv_headers);

            foreach (Room room in rooms) ListRoomData(room);
            return Result.Succeeded;
        }


        /// <summary>
        ///     Draft for method to distinguish 'Not Placed',
        ///     'Redundant' and 'Not Enclosed' rooms.
        /// </summary>
        private void DistinguishRoomsDraft(
            Document doc,
            ref StringBuilder sb,
            ref int numErr,
            ref int numWarn)
        {
            var rooms
                = new FilteredElementCollector(doc);

            rooms.WherePasses(new RoomFilter());

            foreach (Room r in rooms)
            {
                sb.AppendFormat("\r\n  Room {0}:'{1}': ",
                    r.Id, r.Name);

                if (r.Area > 0) // OK if having Area
                    sb.AppendFormat("OK (A={0}[ft3])", r.Area);
                else if (null == r.Location) // Unplaced if no Location
                    sb.AppendFormat("UnPlaced (Location is null)");
                else
                    sb.AppendFormat("NotEnclosed or Redundant "
                                    + "- how to distinguish?");
            }
        }

        /// <summary>
        ///     Distinguish 'Not Placed',  'Redundant'
        ///     and 'Not Enclosed' rooms.
        /// </summary>
        private RoomState DistinguishRoom(Room room)
        {
            var res = RoomState.Unknown;

            if (room.Area > 0)
            {
                // Placed if having Area

                res = RoomState.Placed;
            }
            else if (null == room.Location)
            {
                // No Area and No Location => Unplaced

                res = RoomState.NotPlaced;
            }
            else
            {
                // must be Redundant or NotEnclosed

                var opt
                    = new SpatialElementBoundaryOptions();

                var segs
                    = room.GetBoundarySegments(opt);

                res = null == segs || segs.Count == 0
                    ? RoomState.NotEnclosed
                    : RoomState.Redundant;
            }

            return res;
        }

        /// <summary>
        ///     Add new points to the list.
        ///     Skip the first new point if it equals the last
        ///     old existing one. Actually, we can test all points
        ///     and always ignore very close consecutive ones.
        /// </summary>
        private static void AddNewPoints(
            IList<XYZ> pts,
            IList<XYZ> newpts)
        {
            foreach (var p in newpts)
                if (0 == pts.Count
                    || !Util.IsEqual(p, pts.Last(),
                        _tolerance))
                    pts.Add(p);
        }

        /// <summary>
        ///     Return room boundary points retrieved
        ///     from the room boundary segments.
        /// </summary>
        private static List<XYZ> GetBoundaryPoints(
            IList<IList<BoundarySegment>> boundary)
        {
            var pts = new List<XYZ>();

            var n = boundary.Count;

            if (1 > n)
            {
                Debug.Print("Boundary contains no loops");
            }
            else
            {
                if (1 < n)
                    Debug.Print(
                        "Boundary contains {0} loop{1}; "
                        + "skipping all but first.",
                        n, Util.PluralSuffix(n));

                foreach (var loop in boundary)
                {
                    foreach (var seg in loop)
                    {
                        var c = seg.GetCurve();
                        AddNewPoints(pts, c.Tessellate());
                    }

                    var z = pts[0].Z;

                    foreach (var p in pts)
                        Debug.Assert(
                            Util.IsEqual(p.Z, z, _tolerance),
                            "expected horizontal room boundary");

                    // Break after first loop, which is hopefully 
                    // the exterior one, and hopefully the only one.
                    // Todo: add better handling for more complex cases.

                    break;
                }
            }

            return pts;
        }

        /// <summary>
        ///     Return bounding box calculated from the room
        ///     boundary segments. The lower left corner turns
        ///     out to be identical with the one returned by
        ///     the standard room bounding box.
        /// </summary>
        private static BoundingBoxXYZ GetBoundingBox(
            IList<IList<BoundarySegment>> boundary)
        {
            var bb = new BoundingBoxXYZ();
            bb.Clear();

            foreach (var loop in boundary)
            foreach (var seg in loop)
            {
                var c = seg.GetCurve();
                var pts = c.Tessellate();
                foreach (var p in pts) bb.ExpandToContain(p);
            }

            return bb;
        }

        /// <summary>
        ///     Return bounding box calculated from the room
        ///     boundary segments. The lower left corner turns
        ///     out to be identical with the one returned by
        ///     the standard room bounding box.
        /// </summary>
        private static List<XYZ> GetConvexHullOfRoomBoundary(
            IList<IList<BoundarySegment>> boundary)
        {
            var convex_hull = new List<XYZ>();

            if (0 < boundary.Count)
            {
                var pts = new List<XYZ>();

                foreach (var loop in boundary)
                foreach (var seg in loop)
                {
                    var c = seg.GetCurve();
                    pts.AddRange(c.Tessellate());
                }

                var n = pts.Count;

                pts = new List<XYZ>(
                    pts.Distinct(new CmdWallTopFaces.XyzEqualityComparer(1.0e-4)));

                Debug.Print(
                    "{0} points from tessellated room boundaries, "
                    + "{1} points after cleaning up duplicates",
                    n, pts.Count);

                convex_hull = Util.ConvexHull(pts);
            }

            return convex_hull;
        }

        /// <summary>
        ///     List some properties of a given room to the
        ///     Visual Studio debug output window.
        /// </summary>
        private void ListRoomData(Room room)
        {
            var opt
                = new SpatialElementBoundaryOptions();

            var nr = room.Number;
            var name = room.Name;
            var area = room.Area;

            var loc = room.Location;
            var p = loc is not LocationPoint lp ? XYZ.Zero : lp.Point;

            var bb = room.get_BoundingBox(null);

            var boundary
                = room.GetBoundarySegments(opt);

            var nLoops = boundary.Count;

            var nFirstLoopSegments = 0 < nLoops
                ? boundary[0].Count
                : 0;

            var boundary_bounding_box
                = GetBoundingBox(boundary);

            var convex_hull
                = GetConvexHullOfRoomBoundary(boundary);

            var boundary_pts = GetBoundaryPoints(
                boundary);

            string room_point_str,
                lower_left_str,
                boundary_pts_str,
                convex_hull_str,
                bounding_box_str;

            var llx = boundary_bounding_box.Min.X;

            if (double.MaxValue == llx)
            {
                lower_left_str = "undefined";
                Debug.Assert(0 == boundary_pts.Count,
                    "expected empty boundary for undefined lower left corner");
                Debug.Assert(0 == convex_hull.Count,
                    "expected empty convex hull for undefined lower left corner");
            }
            else
            {
                lower_left_str = _exportInMillimetres
                    ? new IntPoint3d(boundary_bounding_box.Min)
                        .ToString(_exportCsv)
                    : Util.PointString(
                        boundary_bounding_box.Min, _exportCsv);
            }

            if (_exportInMillimetres)
            {
                room_point_str = new IntPoint3d(p)
                    .ToString(_exportCsv);

                var separator = _exportCsv ? " " : ", ";

                boundary_pts_str = string.Join(separator,
                    boundary_pts.Select(q
                        => new IntPoint2d(q.X, q.Y)
                            .ToString(_exportCsv)));

                convex_hull_str = string.Join(separator,
                    convex_hull.Select(q
                        => new IntPoint2d(q.X, q.Y)
                            .ToString(_exportCsv)));

                bounding_box_str = null == bb
                    ? "null"
                    : $"{new IntPoint3d(bb.Min).ToString(_exportCsv)}{separator}{new IntPoint3d(bb.Max).ToString(_exportCsv)}";
            }
            else
            {
                room_point_str = Util.PointString(
                    p, _exportCsv);

                var boundary_pts_2d = boundary_pts
                    .Select(q => new UV(q.X, q.Y));

                var convex_hull_2d = convex_hull
                    .Select(q => new UV(q.X, q.Y));

                boundary_pts_str = Util.PointArrayString(
                    boundary_pts_2d, _exportCsv);

                convex_hull_str = Util.PointArrayString(
                    convex_hull_2d, _exportCsv);

                bounding_box_str = null == bb
                    ? "null"
                    : Util.BoundingBoxString(bb, _exportCsv);
            }

            Debug.Print(_format_string, nr, name, room_point_str, lower_left_str, boundary_pts_str, convex_hull_str, bounding_box_str, area, nLoops, Util.PluralSuffix(nLoops),
                nFirstLoopSegments, Util.PluralSuffix(nFirstLoopSegments));
        }
    }
}

// C:\Program Files\Autodesk\Revit Architecture 2012\Program\Samples\rac_basic_sample_project.rvt