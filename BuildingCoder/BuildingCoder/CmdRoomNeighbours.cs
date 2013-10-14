#region Header
//
// CmdRoomNeighbours.cs - determine neighbouring room at midpoint of each room boundary segment
//
// Copyright (C) 2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.IO;
using BoundarySegment = Autodesk.Revit.DB.BoundarySegment;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdRoomNeighbours : IExternalCommand
  {
    /// <summary>
    /// Return the neighbouring room to the given one
    /// on the other side of the midpoint of the given
    /// boundary segment.
    /// </summary>
    Room GetRoomNeighbourAt( 
      BoundarySegment bs,
      Room r )
    {
      Document doc = r.Document;

      Wall w = bs.Element as Wall;

      double wallThickness = w.Width;

      double wallLength = ( w.Location as 
        LocationCurve ).Curve.Length;

      Transform derivatives = bs.Curve
        .ComputeDerivatives(  0.5, true );

      XYZ midPoint = derivatives.Origin;
      
      Debug.Assert( 
        midPoint.IsAlmostEqualTo( 
          bs.Curve.Evaluate( 0.5, true ) ),
        "expected same result from Evaluate and derivatives" );

      XYZ tangent = derivatives.BasisX.Normalize();

      XYZ normal = new XYZ( tangent.Y, 
        tangent.X * ( -1 ), tangent.Z );

      XYZ p = midPoint + wallThickness * normal;

      Room otherRoom = doc.GetRoomAtPoint( p );

      if( null != otherRoom )
      {
        if( otherRoom.Id == r.Id )
        {
          normal = new XYZ( tangent.Y * ( -1 ), 
            tangent.X, tangent.Z );

          p = midPoint + wallThickness * normal;

          otherRoom = doc.GetRoomAtPoint( p );

          Debug.Assert( null == otherRoom 
              || otherRoom.Id != r.Id,
            "expected different room on other side" );
        }
      }
      return otherRoom;
    }

    public Result Execute( 
      ExternalCommandData commandData, 
      ref string message, 
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Interactively select elements of type Room,
      // either via pre-selection before launching the
      // command, or interactively via post-selection.

      JtSelectorMulti<Room> selector 
        = new JtSelectorMulti<Room>( 
          uidoc, BuiltInCategory.OST_Rooms, "room", 
          e => e is Room );

      if( selector.IsEmpty )
      {
        return selector.ShowResult();
      }

      IList<Room> rooms = selector.Selected;

      List<string> msg = new List<string>();

      int n = rooms.Count;

      msg.Add( string.Format(
        "{0} room{1} selected{2}\r\n",
        n, Util.PluralSuffix( n ), 
        Util.DotOrColon( n ) ) );

      SpatialElementBoundaryOptions opt 
        = new SpatialElementBoundaryOptions();

      IList<IList<BoundarySegment>> loops;

      Room neighbour;
      int i = 0, j, k;

      foreach( Room room in rooms )
      {
        ++i;
        
        loops = room.GetBoundarySegments( opt );

        n = loops.Count;

        msg.Add( string.Format(
          "{0}. {1} has {2} loop{3}{4}",
          i, Util.ElementDescription( room ),
          n, Util.PluralSuffix( n ),
          Util.DotOrColon( n ) ) );

        j = 0;

        foreach( IList<BoundarySegment> loop in loops )
        {
          ++j;

          n = loop.Count;

          msg.Add( string.Format(
            "  {0}. Loop has {1} boundary segment{2}{3}",
            j, n, Util.PluralSuffix( n ),
            Util.DotOrColon( n ) ) );

          k = 0;

          foreach( BoundarySegment seg in loop )
          {
            ++k;

            neighbour = GetRoomNeighbourAt( seg, room );

            msg.Add( string.Format(
              "    {0}. Boundary segment has neighbour {1}",
              k, 
              (null==neighbour 
                ? "<nil>" 
                : Util.ElementDescription( neighbour )) ) );
          }
        }
      }

      Util.InfoMsg2( "Room Neighbours", 
        string.Join( "\n", msg.ToArray() ) );

      return Result.Succeeded;
    }
  }
}
