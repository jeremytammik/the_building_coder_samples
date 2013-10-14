#region Header
//
// CmdWallProfile.cs - determine wall
// elevation profile boundary loop polygons
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
  class CmdWallProfile : IExternalCommand
  {
    /// <summary>
    /// Offset the generated boundary polygon loop
    /// model lines outwards to separate them from
    /// the wall edge, measured in feet.
    /// </summary>
    const double _offset = 1.0;

    /// <summary>
    /// Determine the elevation boundary profile
    /// polygons of the exterior vertical planar
    /// face of the given wall solid.
    /// </summary>
    /// <param name="polygons">Return polygonal boundary
    /// loops of exterior vertical planar face, i.e.
    /// profile of wall elevation incl. holes</param>
    /// <param name="solid">Input solid</param>
    /// <param name="w">Vector pointing along
    /// wall centre line</param>
    /// <param name="w">Vector pointing towards
    /// exterior wall face</param>
    /// <returns>False if no exterior vertical
    /// planar face was found, else true</returns>
    static bool GetProfile(
      List<List<XYZ>> polygons,
      Solid solid,
      XYZ v,
      XYZ w )
    {
      double d, dmax = 0;
      PlanarFace outermost = null;
      FaceArray faces = solid.Faces;
      foreach( Face f in faces )
      {
        PlanarFace pf = f as PlanarFace;
        if( null != pf
          && Util.IsVertical( pf )
          && Util.IsZero( v.DotProduct( pf.Normal ) ) )
        {
          d = pf.Origin.DotProduct( w );
          if( ( null == outermost )
            || ( dmax < d ) )
          {
            outermost = pf;
            dmax = d;
          }
        }
      }

      if( null != outermost )
      {
        XYZ voffset = _offset * w;
        XYZ p, q = XYZ.Zero;
        bool first;
        int i, n;
        EdgeArrayArray loops = outermost.EdgeLoops;
        foreach( EdgeArray loop in loops )
        {
          List<XYZ> vertices = new List<XYZ>();
          first = true;
          foreach( Edge e in loop )
          {
            IList<XYZ> points = e.Tessellate();
            p = points[0];
            if( !first )
            {
              Debug.Assert( p.IsAlmostEqualTo( q ),
                "expected subsequent start point"
                + " to equal previous end point" );
            }
            n = points.Count;
            q = points[n - 1];
            for( i = 0; i < n - 1; ++i )
            {
              XYZ a = points[i];
              a += voffset;
              vertices.Add( a );
            }
          }
          q += voffset;
          Debug.Assert( q.IsAlmostEqualTo( vertices[0] ),
            "expected last end point to equal"
            + " first start point" );
          polygons.Add( vertices );
        }
      }
      return null != outermost;
    }

    /// <summary>
    /// Return all wall profile boundary loop polygons
    /// for the given walls, offset out from the outer
    /// face of the wall by a certain amount.
    /// </summary>
    static public List<List<XYZ>> GetWallProfilePolygons(
      //Application app,
      List<Element> walls,
      Options opt )
    {
      XYZ p, q, v, w;
      //Options opt = app.Create.NewGeometryOptions();
      List<List<XYZ>> polygons = new List<List<XYZ>>();

      foreach( Wall wall in walls )
      {
        string desc = Util.ElementDescription( wall );

        LocationCurve curve
          = wall.Location as LocationCurve;

        if( null == curve )
        {
          throw new Exception( desc
            + ": No wall curve found." );
        }
        p = curve.Curve.GetEndPoint( 0 );
        q = curve.Curve.GetEndPoint( 1 );
        v = q - p;
        v = v.Normalize();
        w = XYZ.BasisZ.CrossProduct( v ).Normalize();
        if( wall.Flipped ) { w = -w; }

        GeometryElement geo = wall.get_Geometry( opt );

        //GeometryObjectArray objects = geo.Objects; // 2012
        //foreach( GeometryObject obj in objects ) // 2012

        foreach( GeometryObject obj in geo ) // 2013
        {
          Solid solid = obj as Solid;
          if( solid != null )
          {
            GetProfile( polygons, solid, v, w );
          }
        }
      }
      return polygons;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> walls = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        walls, uidoc, typeof( Wall ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some wall elements."
          : "No wall elements found.";
        return Result.Failed;
      }

      Options opt = app.Application.Create.NewGeometryOptions();

      List<List<XYZ>> polygons
        = GetWallProfilePolygons( walls, opt );

      int n = polygons.Count;

      Debug.Print(
        "{0} boundary loop{1} found.",
        n, Util.PluralSuffix( n ) );

      Creator creator = new Creator( doc );
      creator.DrawPolygons( polygons );

      return Result.Succeeded;
    }
  }
}
