#region Header
//
// CmdGetDimensionPoints.cs - determine dimension segment start and end points
//
// Copyright (C) 2017 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdGetDimensionPoints : IExternalCommand
  {
    List<XYZ> GetDimensionPoints( Dimension dim )
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

    XYZ GetDimensionStartPoint( Dimension dim )
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

      List<XYZ> pts = GetDimensionPoints( dim );
      XYZ p = GetDimensionStartPoint( dim );

      Debug.Print( "Start at {0}, points {1}.", 
        p, string.Join( ",", pts.Select( 
          q => Util.PointString( q ) ) ) );

      return Result.Succeeded;
    }
  }
}
