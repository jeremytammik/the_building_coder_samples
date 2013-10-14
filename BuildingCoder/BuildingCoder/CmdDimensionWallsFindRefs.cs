#region Header
//
// CmdDimensionWallsFindRefs.cs - create dimensioning elements
// between opposing walls using FindReferencesByDirection
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Dimension two opposing parallel walls.
  /// Prompt user to select the first wall, and
  /// the second at the point at which to create
  /// the dimensioning. Use FindReferencesByDirection
  /// to determine the wall face references.
  ///
  /// Second sample solution for case 1263303 [Case
  /// Number 1263071 [Revit 2011 Dimension Wall]].
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  class CmdDimensionWallsFindRefs : IExternalCommand
  {
    const string _prompt
      = "Please select two parallel straight walls"
        + " with a partial projected overlap.";

    #region WallSelectionFilter
    class WallSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is Wall;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }
    #endregion // WallSelectionFilter

    #region Get3DView
    /// <summary>
    /// Return a 3D view from the given document.
    /// </summary>
    private View3D Get3DView( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( View3D ) );

      foreach( View3D v in collector )
      {
        // skip view templates here because they
        // are invisible in project browsers:

        if( v != null && !v.IsTemplate && v.Name == "{3D}" )
        {
          return v;
        }
      }
      return null;
    }
    #endregion // Get3DView

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // select two walls and the dimension line point:

      Selection sel = uidoc.Selection;
      ReferenceArray refs = new ReferenceArray();

      try
      {
        WallSelectionFilter f
          = new WallSelectionFilter();

        refs.Append( sel.PickObject(
          ObjectType.Element, f,
          "Please select first wall" ) );

        refs.Append( sel.PickObject(
          ObjectType.Element, f,
          "Please pick dimension line "
          + "point on second wall" ) );

        //rFace = sel.PickObject( ObjectType.Face,
        //  "Please select face on second wall at dimension line point" );
        //
        //rPoint = sel.PickObject( ObjectType.PointOnElement,
        //  "Please select point on first wall" );
      }
      catch( OperationCanceledException )
      {
        message = "No two walls selected";
        return Result.Failed;
      }

      // ensure the two selected walls are straight and
      // parallel; determine their mutual normal vector
      // and a point on each wall for distance
      // calculations:

      Wall[] walls = new Wall[2];
      List<int> ids = new List<int>( 2 );
      XYZ[] pts = new XYZ[2];
      Line[] lines = new Line[2];
      IntersectionResult ir;
      XYZ normal = null;
      int i = 0;

      foreach( Reference r in refs )
      {
        // 'Autodesk.Revit.DB.Reference.Element' is
        // obsolete: Property will be removed. Use
        // Document.GetElement(Reference) instead.
        //Wall wall = r.Element as Wall; // 2011

        Wall wall = doc.GetElement( r ) as Wall; // 2012

        walls[i] = wall;
        ids.Add( wall.Id.IntegerValue );

        // obtain location curve and
        // check that it is straight:

        LocationCurve lc = wall.Location
          as LocationCurve;

        Curve curve = lc.Curve;
        lines[i] = curve as Line;

        if( null == lines[i] )
        {
          message = _prompt;
          return Result.Failed;
        }

        // obtain normal vectors
        // and ensure that they are equal,
        // i.e. walls are parallel:

        if( null == normal )
        {
          normal = Util.Normal( lines[i] );
        }
        else
        {
          if( !Util.IsParallel( normal,
            Util.Normal( lines[i] ) ) )
          {
            message = _prompt;
            return Result.Failed;
          }
        }

        // obtain pick points and project
        // onto wall location lines:

        XYZ p = r.GlobalPoint;
        ir = lines[i].Project( p );

        if( null == ir )
        {
          message = string.Format(
            "Unable to project pick point {0} "
            + "onto wall location line.",
            i );

          return Result.Failed;
        }

        pts[i] = ir.XYZPoint;

        Debug.Print(
          "Wall {0} id {1} at {2}, {3} --> point {4}",
          i, wall.Id.IntegerValue,
          Util.PointString( lines[i].GetEndPoint( 0 ) ),
          Util.PointString( lines[i].GetEndPoint( 1 ) ),
          Util.PointString( pts[i] ) );

        if( 0 < i )
        {
          // project dimension point selected on second wall
          // back onto first wall, and ensure that normal
          // points from second wall to first:

          ir = lines[0].Project( pts[1] );
          if( null == ir )
          {
            message = string.Format(
              "Unable to project selected dimension "
              + "line point {0} on second wall onto "
              + "first wall's location line.",
              Util.PointString( pts[1] ) );

            return Result.Failed;
          }
          pts[0] = ir.XYZPoint;
        }

        ++i;
      }

      XYZ v = pts[0] - pts[1];
      if( 0 > v.DotProduct( normal ) )
      {
        normal = -normal;
      }

      // invoke FindReferencesByDirection, shooting ray
      // back from second picked wall towards first:

      View3D view = Get3DView( doc );

      //refs = doc.FindReferencesByDirection(
      //  pts[1], normal, view ); // 2011

      IList<ReferenceWithContext> refs2
        = doc.FindReferencesWithContextByDirection(
          pts[1], normal, view ); // 2012

      Debug.Print( "Shooting ray from {0} direction "
        + "{1} returns {2} references",
        Util.PointString( pts[1] ),
        Util.PointString( normal ),
        refs2.Count );

      // store the references to the wall surfaces:

      Reference[] surfrefs = new Reference[2] {
        null, null };

      // find the two closest intersection
      // points on each of the two walls:

      double[] minDistance = new double[2] {
        double.MaxValue,
        double.MaxValue };

      //foreach( Reference r in refs )
      foreach( ReferenceWithContext rc in refs2 )
      {
        // 'Autodesk.Revit.DB.Reference.Element' is
        // obsolete: Property will be removed. Use
        // Document.GetElement(Reference) instead.
        //Element e = r.Element; // 2011

        Reference r = rc.GetReference();
        Element e = doc.GetElement( r ); // 2012

        if( e is Wall )
        {
          i = ids.IndexOf( e.Id.IntegerValue );

          if( -1 < i
            && ElementReferenceType.REFERENCE_TYPE_SURFACE
              == r.ElementReferenceType )
          {
            //GeometryObject g = r.GeometryObject; // 2011
            GeometryObject g = e.GetGeometryObjectFromReference( r ); // 2012

            if( g is PlanarFace )
            {
              PlanarFace face = g as PlanarFace;

              Line line = ( e.Location as LocationCurve )
                .Curve as Line;

              Debug.Print(
                "Wall {0} at {1}, {2} surface {3} "
                + "normal {4} proximity {5}",
                e.Id.IntegerValue,
                Util.PointString( line.GetEndPoint( 0 ) ),
                Util.PointString( line.GetEndPoint( 1 ) ),
                Util.PointString( face.Origin ),
                Util.PointString( face.Normal ),
                rc.Proximity );

              // first reference: assert it is a face on this wall
              // and the distance is half the wall thickness
              //
              // second reference: the first reference on the other
              // wall; assert the distance between the two references
              // equals the distance between the wall location lines
              // minus half of the sum of the two wall thicknesses.

              if( rc.Proximity < minDistance[i] )
              {
                surfrefs[i] = r;
                minDistance[i] = rc.Proximity;
              }
            }
          }
        }
      }

      if( null == surfrefs[0] )
      {
        message = "No suitable face intersection "
          + "points found on first wall.";

        return Result.Failed;
      }

      if( null == surfrefs[1] )
      {
        message = "No suitable face intersection "
          + "points found on second wall.";

        return Result.Failed;
      }

      CmdDimensionWallsIterateFaces
        .CreateDimensionElement( doc.ActiveView,
        pts[0], surfrefs[0], pts[1], surfrefs[1] );

      return Result.Succeeded;
    }
  }
}
