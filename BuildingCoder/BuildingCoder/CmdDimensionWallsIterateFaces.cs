#region Header
//
// CmdDimensionWallsIterateFaces.cs - create dimensioning elements
// between opposing walls by iterating over their faces
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
  /// For simplicity, the dimension is defined from
  /// wall midpoint to midpoint, so the walls have
  /// to be exactly opposite each other for it to work.
  /// Iterate the wall solid faces to find the two
  /// closest opposing faces and use references to
  /// them to define the dimension element.
  ///
  /// First sample solution for case
  /// 1263071 [Revit 2011 Dimension Wall].
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  class CmdDimensionWallsIterateFaces : IExternalCommand
  {
    #region Developer Guide Sample Code
    public void DuplicateDimension(
      Document doc,
      Dimension dimension )
    {
      Line line = dimension.Curve as Line;

      if( null != line )
      {
        Autodesk.Revit.DB.View view = dimension.View;

        ReferenceArray references = dimension.References;

        Dimension newDimension = doc.Create.NewDimension(
          view, line, references );
      }
    }

    public Dimension CreateLinearDimension(
      Document doc )
    {
      Application app = doc.Application;

      // first create two lines

      XYZ pt1 = new XYZ( 5, 5, 0 );
      XYZ pt2 = new XYZ( 5, 10, 0 );
      Line line = Line.CreateBound( pt1, pt2 );
      Plane plane = app.Create.NewPlane(
        pt1.CrossProduct( pt2 ), pt2 );

      //SketchPlane skplane = doc.FamilyCreate.NewSketchPlane( plane ); // 2013

      SketchPlane skplane = SketchPlane.Create( doc, plane ); // 2014

      ModelCurve modelcurve1 = doc.FamilyCreate
        .NewModelCurve( line, skplane );

      pt1 = new XYZ( 10, 5, 0 );
      pt2 = new XYZ( 10, 10, 0 );
      line = Line.CreateBound( pt1, pt2 );
      plane = app.Create.NewPlane(
        pt1.CrossProduct( pt2 ), pt2 );

      //skplane = doc.FamilyCreate.NewSketchPlane( plane ); // 2013

      skplane = SketchPlane.Create( doc, plane ); // 2014

      ModelCurve modelcurve2 = doc.FamilyCreate
        .NewModelCurve( line, skplane );

      // now create a linear dimension between them

      ReferenceArray ra = new ReferenceArray();
      ra.Append( modelcurve1.GeometryCurve.Reference );
      ra.Append( modelcurve2.GeometryCurve.Reference );

      pt1 = new XYZ( 5, 10, 0 );
      pt2 = new XYZ( 10, 10, 0 );
      line = Line.CreateBound( pt1, pt2 );
      Dimension dim = doc.FamilyCreate
        .NewLinearDimension( doc.ActiveView, line, ra );

      // create a label for the dimension called "width"

      FamilyParameter param = doc.FamilyManager
        .AddParameter( "width",
          BuiltInParameterGroup.PG_CONSTRAINTS,
          ParameterType.Length, false );

      //dim.Label = param; // 2013
      dim.FamilyLabel = param; // 2014

      return dim;
    }
    #endregion // Developer Guide Sample Code

    const string _prompt
      = "Please select two parallel opposing straight walls.";

    #region CreateDimensionElement
    /// <summary>
    /// Create a new dimension element using the given
    /// references and dimension line end points.
    ///
    /// This method creates its own transaction.
    /// It assumes that no transaction is open yet
    /// and manual transaction mode is being used!
    ///
    /// This has only been tested using references to
    /// surfaces on planar walls in a plan view!
    ///
    /// Create a new dimension element using the given
    /// references and dimension line end points.
    ///
    /// This method opens and commits its own transaction,
    /// assuming that no transaction is open yet and manual
    /// transaction mode is being used.
    ///
    /// Note that this has only been tested so far using
    /// references to surfaces on planar walls in a plan
    /// view.
    /// </summary>
    public static void CreateDimensionElement(
      View view,
      XYZ p1,
      Reference r1,
      XYZ p2,
      Reference r2 )
    {
      Document doc = view.Document;
      Application app = doc.Application;

      // creation objects, or factories, for database
      // and non-database resident instances:

      Autodesk.Revit.Creation.Application creApp
        = app.Create;

      Autodesk.Revit.Creation.Document creDoc
        = doc.Create;

      ReferenceArray ra = new ReferenceArray();

      ra.Append( r1 );
      ra.Append( r2 );

      Line line = Line.CreateBound( p1, p2 );

      Transaction t = new Transaction( doc,
        "Dimension Two Walls" );

      t.Start();

      Dimension dim = creDoc.NewDimension(
        doc.ActiveView, line, ra );

      t.Commit();
    }
    #endregion // CreateDimensionElement

    #region GetClosestFace
    /// <summary>
    /// Return the closest planar face to a given point
    /// p on the element e with a given normal vector.
    /// </summary>
    static Face GetClosestFace(
      Element e,
      XYZ p,
      XYZ normal,
      Options opt )
    {
      Face face = null;
      double min_distance = double.MaxValue;
      GeometryElement geo = e.get_Geometry( opt );

      //GeometryObjectArray objects = geo.Objects; // 2012
      //foreach( GeometryObject obj in objects ) // 2012

      foreach( GeometryObject obj in geo ) // 2013
      {
        Solid solid = obj as Solid;
        if( solid != null )
        {
          FaceArray fa = solid.Faces;
          foreach( Face f in fa )
          {
            PlanarFace pf = f as PlanarFace;

            Debug.Assert( null != pf,
              "expected planar wall faces" );

            if( null != pf
              //&& normal.IsAlmostEqualTo( pf.Normal )
              && Util.IsParallel( normal, pf.Normal ) )
            {
              //XYZ q = pf.Project( p ).XYZPoint; // Project returned null once
              //double d = q.DistanceTo( p );

              XYZ v = p - pf.Origin;
              double d = v.DotProduct( -pf.Normal );
              if( d < min_distance )
              {
                face = f;
                min_distance = d;
              }
            }
          }
        }
      }
      return face;
    }
    #endregion // GetClosestFace

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // obtain the current selection and pick
      // out all walls from it:

      Selection sel = uidoc.Selection;
      List<Wall> walls = new List<Wall>( 2 );
      foreach( Element e in sel.Elements )
      {
        if( e is Wall )
        {
          walls.Add( e as Wall );
        }
      }

      if( 2 != walls.Count )
      {
        message = _prompt;
        return Result.Failed;
      }

      // ensure the two selected walls are straight and
      // parallel; determine their mutual normal vector
      // and a point on each wall for distance
      // calculations:

      List<Line> lines = new List<Line>( 2 );
      List<XYZ> midpoints = new List<XYZ>( 2 );
      XYZ normal = null;

      foreach( Wall wall in walls )
      {
        LocationCurve lc = wall.Location as LocationCurve;
        Curve curve = lc.Curve;

        if( !( curve is Line ) )
        {
          message = _prompt;
          return Result.Failed;
        }

        Line l = curve as Line;
        lines.Add( l );
        midpoints.Add( Util.Midpoint( l ) );

        if( null == normal )
        {
          normal = Util.Normal( l );
        }
        else
        {
          if( !Util.IsParallel( normal, Util.Normal( l ) ) )
          {
            message = _prompt;
            return Result.Failed;
          }
        }
      }

      // find the two closest facing faces on the walls;
      // they are vertical faces that are parallel to the
      // wall curve and closest to the other wall.

      Options opt = app.Create.NewGeometryOptions();

      opt.ComputeReferences = true;

      List<Face> faces = new List<Face>( 2 );
      faces.Add( GetClosestFace( walls[0], midpoints[1], normal, opt ) );
      faces.Add( GetClosestFace( walls[1], midpoints[0], normal, opt ) );

      // create the dimensioning:

      CreateDimensionElement( doc.ActiveView,
        midpoints[0], faces[0].Reference,
        midpoints[1], faces[1].Reference );

      return Result.Succeeded;
    }
  }
}
