#region Header
//
// CmdGetDimensionPoints.cs - determine dimension segment start and end points
//
// Copyright (C) 2018-2019 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-a-dimension-s-segment-geometry/m-p/7145688
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdGetDimensionPoints : IExternalCommand
  {
    #region Obsolete initial attempts
    List<XYZ> GetDimensionPointsObsoleteFirstAttempt(
      Dimension dim )
    {
      Line dimLine = dim.Curve as Line;
      if( dimLine == null ) return null;
      List<XYZ> pts = new List<XYZ>();

      dimLine.MakeBound( 0, 1 );
      XYZ pt1 = dimLine.GetEndPoint( 0 );
      XYZ pt2 = dimLine.GetEndPoint( 1 );
      XYZ direction = pt2.Subtract( pt1 ).Normalize();
      pts.Add( pt1 );
      if( dim.Segments.Size == 0 )
      {
        pt2 = pt1.Add( direction.Multiply( (double) dim.Value ) );
        pts.Add( pt2 );
      }
      else
      {
        XYZ segmentPt0 = pt1;
        foreach( DimensionSegment seg in dim.Segments )
        {
          XYZ segmentPt1 = segmentPt0.Add( direction.Multiply( (double) seg.Value ) );
          Debug.Print( "pt  {0},  value  {1}", segmentPt1, (double) seg.Value );
          pts.Add( segmentPt1 );
          segmentPt0 = segmentPt1;
        }
      }
      return pts;
    }

    XYZ GetDimensionStartPointFirstAttempt(
      Dimension dim )
    {
      Document doc = dim.Document;

      Line dimLine = dim.Curve as Line;
      if( dimLine == null ) return null;
      dimLine.MakeBound( 0, 1 );

      XYZ dimStartPoint = null;
      XYZ pt1 = dimLine.GetEndPoint( 0 );

      // dim.Origin throws "Cannot access this method
      // if this dimension has more than one segment."
      //Debug.Assert( Util.IsEqual( pt1, dim.Origin ),
      //  "expected equal points" );

      foreach( Reference ref1 in dim.References )
      {
        XYZ refPoint = null;
        Element el = doc.GetElement( ref1.ElementId );
        GeometryObject obj = el.GetGeometryObjectFromReference(
          ref1 );

        if( obj == null )
        {
          // element is Grid or ReferencePlane or ??
          ReferencePlane refPl = el as ReferencePlane;
          if( refPl != null ) refPoint = refPl.GetPlane().Origin;
          Grid grid = el as Grid;
          if( grid != null ) refPoint = grid.Curve.GetEndPoint( 0 );
        }
        else
        {
          // reference to Line, Plane or Point?
          Line l = obj as Line;
          if( l != null ) refPoint = l.GetEndPoint( 0 );
          PlanarFace f = obj as PlanarFace;
          if( f != null ) refPoint = f.Origin;
        }

        if( refPoint != null )
        {
          //View v = doc.ActiveView;
          View v = dim.View;
          Plane WorkPlane = v.SketchPlane.GetPlane();
          XYZ normal = WorkPlane.Normal.Normalize();

          // Project the "globalpoint" of the reference onto the sketchplane

          XYZ refPtonPlane = refPoint.Subtract(
            normal.Multiply( normal.DotProduct(
              refPoint - WorkPlane.Origin ) ) );

          XYZ lineNormal = normal.CrossProduct(
            dimLine.Direction ).Normalize();

          // Project the result onto the dimensionLine

          dimStartPoint = refPtonPlane.Subtract(
            lineNormal.Multiply( lineNormal.DotProduct(
              refPtonPlane - pt1 ) ) );
        }
        break;
      }
      return dimStartPoint;
    }
    #endregion // Obsolete initial attempts

    /// <summary>
    /// Return dimension origin, i.e., the midpoint
    /// of the dimension or of its first segment.
    /// </summary>
    XYZ GetDimensionStartPoint(
      Dimension dim )
    {
      XYZ p = null;

      try
      {
        p = dim.Origin;
      }
      catch( Autodesk.Revit.Exceptions.ApplicationException ex )
      {
        Debug.Assert( ex.Message.Equals( "Cannot access this method if this dimension has more than one segment." ) );

        foreach( DimensionSegment seg in dim.Segments )
        {
          p = seg.Origin;
          break;
        }
      }
      return p;
    }

    /// <summary>
    /// Retrieve the start and end points of
    /// each dimension segment, based on the 
    /// dimension origin determined above.
    /// </summary>
    List<XYZ> GetDimensionPoints(
      Dimension dim,
      XYZ pStart )
    {
      Line dimLine = dim.Curve as Line;
      if( dimLine == null ) return null;
      List<XYZ> pts = new List<XYZ>();

      dimLine.MakeBound( 0, 1 );
      XYZ pt1 = dimLine.GetEndPoint( 0 );
      XYZ pt2 = dimLine.GetEndPoint( 1 );
      XYZ direction = pt2.Subtract( pt1 ).Normalize();

      if( 0 == dim.Segments.Size )
      {
        XYZ v = 0.5 * (double) dim.Value * direction;
        pts.Add( pStart - v );
        pts.Add( pStart + v );
      }
      else
      {
        XYZ p = pStart;
        foreach( DimensionSegment seg in dim.Segments )
        {
          XYZ v = (double) seg.Value * direction;
          if( 0 == pts.Count )
          {
            pts.Add( p = ( pStart - 0.5 * v ) );
          }
          pts.Add( p = p.Add( v ) );
        }
      }
      return pts;
    }

    /// <summary>
    /// Graphical debugging helper using model lines
    /// to draw an X at the given position.
    /// </summary>
    void DrawMarker(
      XYZ p,
      double size,
      SketchPlane sketchPlane )
    {
      size *= 0.5;
      XYZ v = new XYZ( size, size, 0 );
      Document doc = sketchPlane.Document;
      doc.Create.NewModelCurve( Line.CreateBound(
        p - v, p + v ), sketchPlane );
      v = new XYZ( size, -size, 0 );
      doc.Create.NewModelCurve( Line.CreateBound(
        p - v, p + v ), sketchPlane );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;

      ISelectionFilter f
        = new JtElementsOfClassSelectionFilter<Dimension>();

      Reference elemRef = sel.PickObject(
        ObjectType.Element, f, "Pick a dimension" );

      Dimension dim = doc.GetElement( elemRef ) as Dimension;

      XYZ p = GetDimensionStartPoint( dim );
      List<XYZ> pts = GetDimensionPoints( dim, p );

      int n = pts.Count;

      Debug.Print( "Dimension origin at {0} followed "
        + "by {1} further point{2}{3} {4}",
        Util.PointString( p ), n,
        Util.PluralSuffix( n ), Util.DotOrColon( n ),
        string.Join( ", ", pts.Select(
          q => Util.PointString( q ) ) ) );

      List<double> d = new List<double>( n );
      XYZ q0 = p;
      foreach( XYZ q in pts )
      {
        d.Add( q.X - q0.X );
        q0 = q;
      }

      Debug.Print(
        "Horizontal distances in metres: "
        + string.Join( ", ", d.Select( x =>
          Util.RealString( Util.FootToMetre( x ) ) ) ) );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Draw Point Markers" );

        SketchPlane sketchPlane = dim.View.SketchPlane;

        double size = 0.3;
        DrawMarker( p, size, sketchPlane );
        pts.ForEach( q => DrawMarker( q, size, sketchPlane ) );

        tx.Commit();
      }

      return Result.Succeeded;
    }

    /// <summary>
    /// Return a reference built directly from grid
    /// </summary>
    Reference GetGridRef( Document doc )
    {
      ElementId idGrid = new ElementId( 397028 );
      Element eGrid = doc.GetElement( idGrid );
      return new Reference( eGrid );
    }
  }
}
