#region Header
//
// CmdSlabBoundaryArea.cs - determine
// slab boundary polygon loops and areas
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
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
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdSlabBoundaryArea : IExternalCommand
  {
    #region Flatten, i.e. project from 3D to 2D by dropping the Z coordinate
    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static UV Flatten( XYZ point )
    {
      return new UV( point.X, point.Y );
    }

    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static public List<UV> Flatten( List<XYZ> polygon )
    {
      double z = polygon[0].Z;
      List<UV> a = new List<UV>( polygon.Count );
      foreach( XYZ p in polygon )
      {
        Debug.Assert( Util.IsEqual( p.Z, z ),
          "expected horizontal polygon" );
        a.Add( Flatten( p ) );
      }
      return a;
    }

    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static List<List<UV>> Flatten( List<List<XYZ>> polygons )
    {
      double z = polygons[0][0].Z;
      List<List<UV>> a = new List<List<UV>>( polygons.Count );
      foreach( List<XYZ> polygon in polygons )
      {
        Debug.Assert( Util.IsEqual( polygon[0].Z, z ),
          "expected horizontal polygons" );
        a.Add( Flatten( polygon ) );
      }
      return a;
    }
    #endregion // Flatten, i.e. project from 3D to 2D by dropping the Z coordinate

    #region Two-dimensional polygon area
    /// <summary>
    /// Use the formula
    ///
    /// area = sign * 0.5 * sum( xi * ( yi+1 - yi-1 ) )
    ///
    /// to determine the winding direction (clockwise
    /// or counter) and area of a 2D polygon.
    /// Cf. also GetPolygonPlane.
    /// </summary>
    static public double GetSignedPolygonArea( List<UV> p )
    {
      int n = p.Count;
      double sum = p[0].U * ( p[1].V - p[n - 1].V ); // loop at beginning
      for( int i = 1; i < n - 1; ++i )
      {
        sum += p[i].U * ( p[i + 1].V - p[i - 1].V );
      }
      sum += p[n - 1].U * ( p[0].V - p[n - 2].V ); // loop at end
      return 0.5 * sum;
    }
    #endregion // Two-dimensional polygon area

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> floors = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        floors, uidoc, typeof( Floor ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      Options opt = app.Application.Create.NewGeometryOptions();

      List<List<XYZ>> polygons
        = CmdSlabBoundary.GetFloorBoundaryPolygons(
          floors, opt );

      List<List<UV>> flat_polygons
        = Flatten( polygons );

      int i = 0, n = flat_polygons.Count;
      double[] areas = new double[n];
      double a, maxArea = 0.0;
      foreach( List<UV> polygon in flat_polygons )
      {
        a = GetSignedPolygonArea( polygon );
        if( Math.Abs( maxArea ) < Math.Abs( a ) )
        {
          maxArea = a;
        }
        areas[i++] = a;
      }

      Debug.Print(
        "{0} boundary loop{1} found.",
        n, Util.PluralSuffix( n ) );

      for( i = 0; i < n; ++i )
      {
        Debug.Print(
          "  Loop {0} area is {1} square feet{2}",
          i,
          Util.RealString( areas[i] ),
          ( areas[i].Equals( maxArea )
            ? ", outer loop of largest floor slab"
            : "" ) );
      }

      Creator creator = new Creator( doc );
      creator.DrawPolygons( polygons );

      return Result.Succeeded;
    }
  }
}
