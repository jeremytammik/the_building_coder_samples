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
      UV offset = new UV( wallDirection.X, wallDirection.Y );
      double step_outside = offset.GetLength();

      XYZ rayStart = new XYZ( wallOrigin.X - offset.U,
        wallOrigin.Y - offset.V, elevation );

      ReferenceIntersector intersector 
        = new ReferenceIntersector( view );

      IList<ReferenceWithContext> refs 
        = intersector.Find( rayStart, wallDirection );

      List<XYZ> pointList = new List<XYZ>( refs
        .Where<ReferenceWithContext>( r => IsSurface( 
          r.GetReference() ) )
        .Where<ReferenceWithContext>( r => r.Proximity 
          < wallLength + step_outside )
        .Select<ReferenceWithContext, XYZ>( r 
          => r.GetReference().GlobalPoint ) );

      // Check if first point is at the wall start.
      // If so, the wall does not begin with an opening,
      // so that point can be removed. Else, add it.

      XYZ p = pointList[0];

      if( Util.IsEqual( p, wallOrigin ) )
      {
        pointList.RemoveAt( 0 );
      }
      else
      {
        pointList.Insert( 0, wallOrigin );
      }

      // Check if last point is not at wall end.
      // If so, the wall ends with an opening, so 
      // add its end point.

      p = pointList.Last();

      if( Util.IsEqual( p, wallEndPoint ) )
      {
        pointList.Remove( pointList.Last() );
      }
      else
      {
        pointList.Add( wallEndPoint );
      }

      //// If the point count in not even, the wall 
      //// ends with an opening, so add its end as 
      //// a new last point.
      //if( !IsEven( pointList.Count ) )
      //{
      //  pointList.Add( wallEndPoint );
      //}

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
      TaskDialog dlg = new TaskDialog( "Wall Openings" );

      dlg.MainInstruction =
        string.Format( "{0} opening{1} found{2}",
          n, Util.PluralSuffix( n ), 
          Util.DotOrColon( n ) );

      dlg.MainContent = string.Join( "\r\n", openings );

      dlg.Show();

      return Result.Succeeded;
    }
  }
}
