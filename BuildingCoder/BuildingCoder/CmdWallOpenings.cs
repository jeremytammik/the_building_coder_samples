#region Header
//
// CmdWallOpenings.cs - peport wall opening start and end points along location line
//
// Copyright (C) 2015 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// A simple class with two coordinates 
  /// and some other basic info.
  /// </summary>
  class WallOpening2D
  {
    public XYZ Start { get; set; }
    public XYZ End { get; set; }
    override public string ToString()
    {
      return "("
        + Util.PointString( Start ) + "-"
        + Util.PointString( End ) + ")";
    }
  }

  [Transaction( TransactionMode.Manual )]
  class CmdWallOpenings : IExternalCommand
  {
    /// <summary>
    /// Move out of wall and up from floor a bit
    /// </summary>
    const double _offset = 0.1; // feet

    /// <summary>
    /// A small number
    /// </summary>
    const double _eps = .1e-5;

    /// <summary>
    /// Predicate: is the given number even?
    /// </summary>
    static bool IsEven( int i )
    {
      return 0 == i % 2;
    }

    /// <summary>
    /// Predicate: does the given reference refer to a surface?
    /// </summary>
    static bool IsSurface( Reference r )
    {
      return ElementReferenceType.REFERENCE_TYPE_SURFACE
        == r.ElementReferenceType;
    }

    class XyzProximityComparer : IComparer<XYZ>
    {
      XYZ _p;

      public XyzProximityComparer( XYZ p )
      {
        _p = p;
      }

      public int Compare( XYZ x, XYZ y )
      {
        double dx = x.DistanceTo( _p );
        double dy = y.DistanceTo( _p );
        return Util.IsEqual( dx, dy ) ? 0
          : ( dx < dy ? -1 : 1 );
      }
    }
    
    class XyzEqualityComparer : IEqualityComparer<XYZ>
    {
      public bool Equals( XYZ a, XYZ b )
      {
        return _eps > a.DistanceTo( b );
      }

      public int GetHashCode( XYZ a )
      {
        return Util.PointString( a ).GetHashCode();
      }
    }

    /// <summary>
    /// Retrieve all wall openings, 
    /// including at start and end of wall.
    /// </summary>
    List<WallOpening2D> GetWallOpenings(
      Wall wall,
      View3D view )
    {
      Document doc = wall.Document;
      Level level = doc.GetElement( wall.LevelId ) as Level;
      double elevation = level.Elevation;
      Curve c = ( wall.Location as LocationCurve ).Curve;
      XYZ wallOrigin = c.GetEndPoint( 0 );
      XYZ wallEndPoint = c.GetEndPoint( 1 );
      XYZ wallDirection = wallEndPoint - wallOrigin;
      double wallLength = wallDirection.GetLength();
      wallDirection = wallDirection.Normalize();
      UV offsetOut = _offset * new UV( wallDirection.X, wallDirection.Y );

      XYZ rayStart = new XYZ( wallOrigin.X - offsetOut.U,
        wallOrigin.Y - offsetOut.V, elevation + _offset );

      ReferenceIntersector intersector
        = new ReferenceIntersector( view );

      IList<ReferenceWithContext> refs
        = intersector.Find( rayStart, wallDirection );

      // Extract the intersection points:
      // - only surfaces
      // - within wall length plus offset at each end
      // - sorted by proximity
      // - eliminating duplicates

      List<XYZ> pointList = new List<XYZ>( refs
        .Where<ReferenceWithContext>( r => IsSurface(
          r.GetReference() ) )
        .Where<ReferenceWithContext>( r => r.Proximity
          < wallLength + _offset + _offset )
        .OrderBy<ReferenceWithContext, double>(
          r => r.Proximity )
        .Select<ReferenceWithContext, XYZ>( r
          => r.GetReference().GlobalPoint )
        .Distinct<XYZ>( new XyzEqualityComparer() ) );

      // Check if first point is at the wall start.
      // If so, the wall does not begin with an opening,
      // so that point can be removed. Else, add it.

      XYZ q = wallOrigin + _offset * XYZ.BasisZ;

      bool wallHasFaceAtStart = Util.IsEqual( 
        pointList[0], q );

      if( wallHasFaceAtStart )
      {
        pointList.RemoveAll( p
          => _eps > p.DistanceTo( q ) );
      }
      else
      {
        pointList.Insert( 0, wallOrigin );
      }

      // Check if last point is at the wall end.
      // If so, the wall does not end with an opening, 
      // so that point can be removed. Else, add it.

      q = wallEndPoint + _offset * XYZ.BasisZ;

      bool wallHasFaceAtEnd = Util.IsEqual(
        pointList.Last(), q );

      if( wallHasFaceAtEnd )
      {
        pointList.RemoveAll( p
          => _eps > p.DistanceTo( q ) );
      }
      else
      {
        pointList.Add( wallEndPoint );
      }

      int n = pointList.Count;

      Debug.Assert( IsEven( n ),
        "expected an even number of opening sides" );

      var wallOpenings = new List<WallOpening2D>(
        n / 2 );

      for( int i = 0; i < n; i += 2 )
      {
        wallOpenings.Add( new WallOpening2D
        {
          Start = pointList[i],
          End = pointList[i + 1]
        } );
      }
      return wallOpenings;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      if( null == doc )
      {
        message = "Please run this command in a valid document.";
        return Result.Failed;
      }

      View3D view = doc.ActiveView as View3D;

      if( null == view )
      {
        message = "Please run this command in a 3D view.";
        return Result.Failed;
      }

      Element e = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "wall", true );

      List<WallOpening2D> openings = GetWallOpenings(
        e as Wall, view );

      int n = openings.Count;

      string msg = string.Format(
        "{0} opening{1} found{2}",
        n, Util.PluralSuffix( n ),
        Util.DotOrColon( n ) );

      Util.InfoMsg2( msg, string.Join( 
        "\r\n", openings ) );

      //TaskDialog dlg = new TaskDialog( "Wall Openings" );
      //dlg.MainInstruction =
      //  string.Format( "{0} opening{1} found{2}",
      //    n, Util.PluralSuffix( n ),
      //    Util.DotOrColon( n ) );
      //dlg.MainContent = string.Join( "\r\n", openings );
      //dlg.Show();

      return Result.Succeeded;
    }
  }
}
