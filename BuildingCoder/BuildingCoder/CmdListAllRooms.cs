#region Header
//
// CmdListAllRooms.cs - list properties from all rooms
//
// Copyright (C) 2011-2018 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using BoundarySegment = Autodesk.Revit.DB.BoundarySegment;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListAllRooms : IExternalCommand
  {
    /// <summary>
    /// Room boundary tolerance for considering 
    /// slightly differing boundary points equal
    /// </summary>
    static double _tolerance
      = Util.MmToFoot( 1.2 );

    /// <summary>
    /// Create a comma-delimited CSV file instead of 
    /// a human readable text file?
    /// </summary>
    const bool _exportCsv = true;

    /// <summary>
    /// CSV export headers
    /// </summary>
    const string _csv_headers = "Room nr;Name;Center;"
      + "Lower left;Boundary;Convex hull;Bounding box;"
      + "Area in sq ft";

    /// <summary>
    /// Export format string
    /// </summary>
    const string _format_string = _exportCsv
      ? "{0},{1},{2},{3},({4}),({5}),({6}),{7}"
      : "Room nr. '{0}' named '{1}' at {2} with "
        + "lower left corner {3}, "
        + "boundary points ({4}), convex hull ({5}), "
        + "bounding box {6} and area {7} sqf has "
        + "{8} loop{9} and {10} segment{11} in first "
        + "loop.";


    /// <summary>
    /// Draft for method to distinguish 'Not Placed', 
    /// 'Redundant' and 'Not Enclosed' rooms.
    /// </summary>
    void DistinguishRoomsDraft(
      Document doc,
      ref StringBuilder sb,
      ref int numErr,
      ref int numWarn )
    {
      FilteredElementCollector rooms
        = new FilteredElementCollector( doc );

      rooms.WherePasses( new RoomFilter() );

      foreach( Room r in rooms )
      {
        sb.AppendFormat( "\r\n  Room {0}:'{1}': ",
          r.Id.ToString(), r.Name );

        if( r.Area > 0 ) // OK if having Area
        {
          sb.AppendFormat( "OK (A={0}[ft3])", r.Area );
        }
        else if( null == r.Location ) // Unplaced if no Location
        {
          sb.AppendFormat( "UnPlaced (Location is null)" );
        }
        else
        {
          sb.AppendFormat( "NotEnclosed or Redundant "
            + "- how to distinguish?" );
        }
      }
    }

    public enum RoomState
    {
      Unknown,
      Placed,
      NotPlaced,
      NotEnclosed,
      Redundant
    }

    /// <summary>
    /// Distinguish 'Not Placed',  'Redundant' 
    /// and 'Not Enclosed' rooms.
    /// </summary>
    RoomState DistinguishRoom( Room room )
    {
      RoomState res = RoomState.Unknown;

      if( room.Area > 0 )
      {
        // Placed if having Area

        res = RoomState.Placed;
      }
      else if( null == room.Location )
      {
        // No Area and No Location => Unplaced

        res = RoomState.NotPlaced;
      }
      else
      {
        // must be Redundant or NotEnclosed

        SpatialElementBoundaryOptions opt
          = new SpatialElementBoundaryOptions();

        IList<IList<BoundarySegment>> segs
          = room.GetBoundarySegments( opt );

        res = ( null == segs || segs.Count == 0 )
          ? RoomState.NotEnclosed
          : RoomState.Redundant;
      }
      return res;
    }

    /// <summary>
    /// Add new points to the list.
    /// Skip the first new point if it equals the last 
    /// old existing one. Actually, we can test all points 
    /// and always ignore very close consecutive ones.
    /// </summary>
    static void AddNewPoints(
      IList<XYZ> pts,
      IList<XYZ> newpts )
    {
      foreach( XYZ p in newpts )
      {
        if( 0 == pts.Count
          || !Util.IsEqual( p, pts.Last(),
            _tolerance ) )
        {
          pts.Add( p );
        }
      }
    }

    /// <summary>
    /// Return room boundary points retrieved 
    /// from the room boundary segments. 
    /// </summary>
    static List<XYZ> GetBoundaryPoints(
      IList<IList<BoundarySegment>> boundary )
    {
      List<XYZ> pts = new List<XYZ>();

      int n = boundary.Count;

      if( 1 > n )
      {
        Debug.Print( "Boundary contains no loops" );
      }
      else
      {
        if( 1 < n )
        {
          Debug.Print(
            "Boundary contains {0} loop{1}; "
            + "skipping all but first.",
            n, Util.PluralSuffix( n ) );
        }

        foreach( IList<BoundarySegment> loop in boundary )
        {
          foreach( BoundarySegment seg in loop )
          {
            Curve c = seg.GetCurve();
            AddNewPoints( pts, c.Tessellate() );
          }

          double z = pts[0].Z;

          foreach( XYZ p in pts )
          {
            Debug.Assert(
              Util.IsEqual( p.Z, z, _tolerance ),
              "expected horizontal room boundary" );
          }

          // Break after first loop, which is hopefully 
          // the exterior one, and hopefully the only one.
          // Todo: add better handling for more complex cases.

          break;
        }
      }
      return pts;
    }

    /// <summary>
    /// Return bounding box calculated from the room 
    /// boundary segments. The lower left corner turns 
    /// out to be identical with the one returned by 
    /// the standard room bounding box.
    /// </summary>
    static BoundingBoxXYZ GetBoundingBox(
      IList<IList<BoundarySegment>> boundary )
    {
      BoundingBoxXYZ bb = new BoundingBoxXYZ();
      double infinity = double.MaxValue;

      bb.Min = new XYZ( infinity, infinity, infinity );
      bb.Max = -bb.Min;

      foreach( IList<BoundarySegment> loop in boundary )
      {
        foreach( BoundarySegment seg in loop )
        {
          Curve c = seg.GetCurve();
          IList<XYZ> pts = c.Tessellate();
          foreach( XYZ p in pts )
          {
            bb.ExpandToContain( p );
          }
        }
      }
      return bb;
    }

    /// <summary>
    /// Return bounding box calculated from the room 
    /// boundary segments. The lower left corner turns 
    /// out to be identical with the one returned by 
    /// the standard room bounding box.
    /// </summary>
    static List<XYZ> GetConvexHullOfRoomBoundary(
      IList<IList<BoundarySegment>> boundary )
    {
      List<XYZ> convex_hull = new List<XYZ>();

      if( 0 < boundary.Count )
      {
        List<XYZ> pts = new List<XYZ>();

        foreach( IList<BoundarySegment> loop in boundary )
        {
          foreach( BoundarySegment seg in loop )
          {
            Curve c = seg.GetCurve();
            pts.AddRange( c.Tessellate() );
          }
        }
        int n = pts.Count;

        pts = new List<XYZ>(
          pts.Distinct<XYZ>( new CmdWallTopFaces
            .XyzEqualityComparer( 1.0e-4 ) ) );

        Debug.Print(
          "{0} points from tessellated room boundaries, "
          + "{1} points after cleaning up duplicates",
          n, pts.Count );

        convex_hull = Util.ConvexHull( pts );
      }
      return convex_hull;
    }

    /// <summary>
    /// List some properties of a given room to the
    /// Visual Studio debug output window.
    /// </summary>
    void ListRoomData( Room room )
    {
      SpatialElementBoundaryOptions opt
        = new SpatialElementBoundaryOptions();

      string nr = room.Number;
      string name = room.Name;
      double area = room.Area;

      Location loc = room.Location;
      LocationPoint lp = loc as LocationPoint;
      XYZ p = ( null == lp ) ? XYZ.Zero : lp.Point;

      BoundingBoxXYZ bb = room.get_BoundingBox( null );

      IList<IList<BoundarySegment>> boundary
        = room.GetBoundarySegments( opt );

      int nLoops = boundary.Count;

      int nFirstLoopSegments = 0 < nLoops
        ? boundary[0].Count
        : 0;

      BoundingBoxXYZ boundary_bounding_box
        = GetBoundingBox( boundary );

      List<XYZ> convex_hull
        = GetConvexHullOfRoomBoundary( boundary );

      List<XYZ> boundary_pts = GetBoundaryPoints(
        boundary );

      double llx = boundary_bounding_box.Min.X;

      string lower_left = Util.PointString( boundary_bounding_box.Min );

      if( double.MaxValue == llx )
      {
        lower_left = "undefined";
        Debug.Assert( 0 == boundary_pts.Count,
          "expected empty boundary for undefined lower left corner" );
        Debug.Assert( 0 == convex_hull.Count,
          "expected empty convex hull for undefined lower left corner" );
      }

      Debug.Print( string.Format( _format_string,
        nr, name, Util.PointString( p ), lower_left,
        Util.PointArrayString( boundary_pts ),
        Util.PointArrayString( convex_hull ),
        Util.BoundingBoxString( bb, true ), area,
        nLoops, Util.PluralSuffix( nLoops ),
        nFirstLoopSegments, Util.PluralSuffix(
          nFirstLoopSegments ) ) );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

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

      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( SpatialElement ) );

      IEnumerable<Element> rooms = collector
        .Where<Element>( e => e is Room );

      if( _exportCsv )
      {
        Debug.Print( _csv_headers );
      }

      foreach( Room room in rooms )
      {
        ListRoomData( room );
      }
      return Result.Succeeded;
    }
  }
}

// C:\Program Files\Autodesk\Revit Architecture 2012\Program\Samples\rac_basic_sample_project.rvt
