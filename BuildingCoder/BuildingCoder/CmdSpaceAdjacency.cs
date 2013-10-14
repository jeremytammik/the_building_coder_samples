#region Header
//
// CmdSpaceAdjacency.cs - determine space adjacencies.
//
// Copyright (C) 2009-2010 by Martin Schmid and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
// todo: report and resolve this, this should not be required: 'RE: ambiguous BoundarySegmentArrayArray'
//using BoundarySegmentArrayArray = Autodesk.Revit.DB.Mechanical.BoundarySegmentArrayArray; // 2011
//using BoundarySegmentArray = Autodesk.Revit.DB.Mechanical.BoundarySegmentArray; // 2011
//using BoundarySegment = Autodesk.Revit.DB.Mechanical.BoundarySegment; // 2011
using BoundarySegment = Autodesk.Revit.DB.BoundarySegment; // 2012
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdSpaceAdjacency : IExternalCommand
  {
    #region Segment Class
    class Segment
    {
      XYZ _sp;
      XYZ _ep;
      Space _space;

      public XYZ StartPoint
      {
        get { return _sp; }
      }

      public XYZ EndPoint
      {
        get { return _ep; }
      }

      public Space Space
      {
        get { return _space; }
        set { _space = value; }
      }

      public Segment( XYZ sp, XYZ ep, Space space )
      {
        _sp = sp;
        _ep = ep;
        _space = space;
      }

      public double Slope
      {
        get
        {
          double deltaX = _sp.X - _ep.X;
          double deltaY = _sp.Y - _ep.Y;
          if( deltaX != 0 )
          {
            return deltaY / deltaX;
          }
          return 0;
        }
      }

      public bool IsHorizontal
      {
        get
        {
          return _sp.Y == _ep.Y;
        }
      }

      public bool IsVertical
      {
        get
        {
          return _sp.X == _ep.X;
        }
      }

      public new string ToString()
      {
        return string.Format( "{0} {1}",
          Util.PointString( _sp ),
          Util.PointString( _ep ) );
      }

      public XYZ MidPoint
      {
        get
        {
          return Util.Midpoint( _sp, _ep );
        }
      }

      public XYZ DirectionTo( Segment a )
      {
        XYZ v = a.MidPoint - MidPoint;
        return v.IsZeroLength() ? v : v.Normalize();
      }

      public double Distance( Segment a )
      {
        return MidPoint.DistanceTo( a.MidPoint );
      }

      public bool Parallel( Segment a )
      {
        return ( IsVertical && a.IsVertical )
          || ( IsHorizontal && a.IsHorizontal )
          || Util.IsEqual( Slope, a.Slope );
      }
    }
    #endregion // Segment Class

    const double D2mm = 2.0 / 25.4 / 12; // 2 mm in ft units
    const double MaxWallThickness = 14 / 12;

    private void GetBoundaries(
      List<Segment> segments,
      Space space )
    {
      //BoundarySegmentArrayArray boundaries = space.Boundary; // 2011

      IList<IList<BoundarySegment>> boundaries   // 2012
        = space.GetBoundarySegments(             // 2012
          new SpatialElementBoundaryOptions() ); // 2012

      //foreach( BoundarySegmentArray b in boundaries ) // 2011
      foreach( IList<BoundarySegment> b in boundaries ) // 2012
      {
        foreach( BoundarySegment s in b )
        {
          Curve curve = s.Curve;
          IList<XYZ> a = curve.Tessellate();
          for( int i = 1; i < a.Count; ++i )
          {
            Segment segment = new Segment(
              a[i - 1], a[i], space );

            segments.Add( segment );
          }
        }
      }
    }

    private void FindClosestSegments(
      Dictionary<Segment, Segment> segmentPairs,
      List<Segment> segments )
    {
      foreach( Segment segOuter in segments )
      {
        bool first = true;
        double dist = 0;
        Segment closest = null;

        foreach( Segment segInner in segments )
        {
          if( segOuter == segInner )
            continue;

          if( segInner.Space == segOuter.Space )
            continue;

          double d = segOuter.Distance(
            segInner );

          if( first || d < dist )
          {
            dist = d;
            first = false;
            closest = segInner;
          }
        }

        segmentPairs.Add( segOuter, closest );
      }
    }

    private void DetermineAdjacencies(
      Dictionary<Space, List<Space>> a,
      Dictionary<Segment, Segment> segmentPairs )
    {
      foreach( Segment s in segmentPairs.Keys )
      {

        // Analyse the relationship between the two
        // closest segments s and t. If their distance
        // exceeds the maximum wall thickness, the
        // spaces are not considered adjacent.
        // Otherwise, calculate a test point 2 mm
        // away from s in the direction of t and
        // use the Space.IsPointInSpace method:

        Segment t = segmentPairs[s];
        double d = s.Distance( t );
        if( d < MaxWallThickness )
        {
          XYZ direction = s.DirectionTo( t );
          XYZ startPt = t.MidPoint;
          XYZ testPoint = startPt + direction * D2mm;
          if( t.Space.IsPointInSpace( testPoint ) )
          {
            if( !a.ContainsKey( s.Space ) )
            {
              a.Add( s.Space, new List<Space>() );
            }
            if( !a[s.Space].Contains( t.Space ) )
            {
              a[s.Space].Add( t.Space );
            }
          }
        }
      }
    }

    private void PrintSpaceInfo(
      string indent,
      Space space )
    {
      Debug.Print( "{0}{1} {2}", indent,
        space.Name, space.Number );
    }

    private void ReportAdjacencies(
      Dictionary<Space, List<Space>> spaceAdjacencies )
    {
      Debug.WriteLine( "\nReport Space Adjacencies:" );
      foreach( Space space in spaceAdjacencies.Keys )
      {
        PrintSpaceInfo( "", space );
        foreach( Space adj in spaceAdjacencies[space] )
        {
          PrintSpaceInfo( "  ", adj );
        }
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements)
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> spaces = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        spaces, uidoc, typeof( Space ) ) )
      {
        Selection sel = uidoc.Selection;
        message = (0 < sel.Elements.Size)
          ? "Please select some space elements."
          : "No space elements found.";
        return Result.Failed;
      }

      List<Segment> segments = new List<Segment>();

      foreach( Space space in spaces )
      {
        GetBoundaries( segments, space );
      }

      Dictionary<Segment, Segment> segmentPairs
        = new Dictionary<Segment, Segment>();

      FindClosestSegments( segmentPairs, segments );

      Dictionary<Space, List<Space>> spaceAdjacencies
        = new Dictionary<Space, List<Space>>();

      DetermineAdjacencies(
        spaceAdjacencies, segmentPairs );

      ReportAdjacencies( spaceAdjacencies );

      return Result.Failed;
    }
  }
}
