#region Header
//
// CmdNewSprinkler.cs - insert a new sprinkler family instance
//
// Copyright (C) 2010-2018 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewSprinkler : IExternalCommand
  {
    const string _path = "C:/Documents and Settings/All Users/Application Data/Autodesk/RME 2010/Metric Library/Fire Protection/Sprinklers/";
    const string _name = "M_Sprinkler - Pendent - Hosted";
    const string _ext = ".rfa";

    const string _filename = _path + _name + _ext;

    /// <summary>
    /// Return the largest horizontal face of the given
    /// element e, either top or bottom, optionally
    /// computing references.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="computReferences">Compute references?</param>
    /// <param name="bottomFace">Top or bottom?</param>
    PlanarFace GetLargestHorizontalFace(
      Element e,
      bool computReferences = true,
      bool bottomFace = true )
    {
      //Options opt = app.Application.Create.NewGeometryOptions();

      Options opt = new Options();
      opt.ComputeReferences = computReferences;

      GeometryElement geo = e.get_Geometry( opt );

      PlanarFace largest_face = null;

      foreach( GeometryObject obj in geo )
      {
        Solid solid = obj as Solid;

        if( null != solid )
        {
          foreach( Face face in solid.Faces )
          {
            PlanarFace pf = face as PlanarFace;

            if( null != pf )
            {
              XYZ normal = pf.FaceNormal.Normalize();

              if( Util.IsVertical( normal )
                && ( bottomFace ? 0.0 > normal.Z : 0.0 < normal.Z )
                && ( null == largest_face || largest_face.Area < pf.Area ) )
              {
                largest_face = pf;
                break;
              }
            }
          }
        }
      }
      return largest_face;
    }

    /// <summary>
    /// Return the median point of a triangle by
    /// taking the average of its three vertices.
    /// </summary>
    XYZ MedianPoint( MeshTriangle triangle )
    {
      XYZ p = XYZ.Zero;
      p += triangle.get_Vertex( 0 );
      p += triangle.get_Vertex( 1 );
      p += triangle.get_Vertex( 2 );
      p *= 0.3333333333333333;
      return p;
    }

    /// <summary>
    /// Return the are of a triangle as half of
    /// its height muliplied with its base length.
    /// </summary>
    double TriangleArea( MeshTriangle triangle )
    {
      XYZ a = triangle.get_Vertex( 0 );
      XYZ b = triangle.get_Vertex( 1 );
      XYZ c = triangle.get_Vertex( 2 );

      Line l = Line.CreateBound( a, b );

      double h = l.Project( c ).Distance;

      double area = 0.5 * l.Length * h;

      return area;
    }

    /// <summary>
    /// Return an arbitrary point on a planar face,
    /// namely the midpoint of the first mesh triangle.
    /// </summary>
    XYZ PointOnFace( PlanarFace face )
    {
      Mesh mesh = face.Triangulate();

      return 0 < mesh.NumTriangles
        ? MedianPoint( mesh.get_Triangle( 0 ) )
        : XYZ.Zero;
    }

    /// <summary>
    /// Return a 'good' point on a planar face, namely 
    /// the median point of its largest mesh triangle.
    /// </summary>
    XYZ PointOnFace2( PlanarFace face )
    {
      Mesh mesh = face.Triangulate();
      double max_area = 0;
      int selected = 0;

      for( int i = 0; i < mesh.NumTriangles; i++ )
      {
        double area = TriangleArea(
          mesh.get_Triangle( i ) );

        if( max_area < area )
        {
          max_area = area;
          selected = i;
        }
      }
      return MedianPoint( mesh.get_Triangle( selected ) );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;
      Result rc = Result.Failed;

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Place a New Sprinkler Instance" );


        // retrieve the sprinkler family symbol:

#if _2010
        Filter filter = app.Create.Filter.NewFamilyFilter(
          _name );

        List<Element> families = new List<Element>();
        doc.get_Elements( filter, families );
        Family family = null;

        foreach( Element e in families )
        {
          family = e as Family;
          if( null != family )
            break;
        }
#endif // _2010

        Family family = Util.GetFirstElementOfTypeNamed(
          doc, typeof( Family ), _name ) as Family;

        if( null == family )
        {
          if( !doc.LoadFamily( _filename, out family ) )
          {
            message = "Unable to load '" + _filename + "'.";
            return rc;
          }
        }

        FamilySymbol sprinklerSymbol = null;

        //foreach( FamilySymbol fs in family.Symbols ) // 2014

        foreach( ElementId id in
          family.GetFamilySymbolIds() ) // 2015
        {
          sprinklerSymbol = doc.GetElement( id )
            as FamilySymbol;

          break;
        }

        Debug.Assert( null != sprinklerSymbol,
          "expected at least one sprinkler symbol"
          + " to be defined in family" );

        // pick the host ceiling:

        Element ceiling = Util.SelectSingleElement(
          uidoc, "ceiling to host sprinkler" );

        if( null == ceiling
          || !ceiling.Category.Id.IntegerValue.Equals(
            (int) BuiltInCategory.OST_Ceilings ) )
        {
          message = "No ceiling selected.";
          return rc;
        }

        //Level level = ceiling.Level;

        //XYZ p = new XYZ( 40.1432351841559, 30.09700395984548, 8.0000 );

        // these two methods cannot create the sprinkler on the ceiling:

        //FamilyInstance fi = doc.Create.NewFamilyInstance( p, sprinklerSymbol, ceiling, level, StructuralType.NonStructural );
        //FamilyInstance fi = doc.Create.NewFamilyInstance( p, sprinklerSymbol, ceiling, StructuralType.NonStructural );

        // use this overload so get the bottom face of the ceiling instead:

        // FamilyInstance NewFamilyInstance( Face face, XYZ location, XYZ referenceDirection, FamilySymbol symbol )

        // retrieve the bottom face of the ceiling:

        PlanarFace ceilingBottom
          = GetLargestHorizontalFace( ceiling );

        if( null != ceilingBottom )
        {
          XYZ p = PointOnFace( ceilingBottom );

          // Create the sprinkler family instance:

          FamilyInstance fi = doc.Create.NewFamilyInstance(
            ceilingBottom, p, XYZ.BasisX, sprinklerSymbol );

          rc = Result.Succeeded;
        }
        t.Commit();
      }
      return rc;
    }
  }
}
