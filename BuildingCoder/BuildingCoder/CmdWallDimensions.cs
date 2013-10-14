#region Header
//
// CmdWallDimensions.cs - determine wall dimensions
// by iterating over wall geometry faces
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
using NormalAndOrigins
  = System.Collections.Generic.KeyValuePair<
    Autodesk.Revit.DB.XYZ,
    System.Collections.Generic.List<Autodesk.Revit.DB.XYZ>>;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// List dimensions for a quadrilateral wall with
  /// openings. In this algorithm, we collect all
  /// the faces with parallel normal vectors and
  /// calculate the maximal distance between any
  /// two pairs of them. This is the wall dimension
  /// in that direction.
  /// </summary>
  [Transaction( TransactionMode.Automatic )]
  class CmdWallDimensions : IExternalCommand
  {
    #region Geometry
    const double _eps = 1.0e-9;

    /// <summary>
    /// Check whether two real numbers are equal
    /// </summary>
    static bool DoubleEqual( double a, double b )
    {
      return Math.Abs( a - b ) < _eps;
    }

    /// <summary>
    /// Check whether two vectors are parallel
    /// </summary>
    static bool XyzParallel( XYZ a, XYZ b )
    {
      double angle = a.AngleTo( b );
      return _eps > angle
        || DoubleEqual( angle, Math.PI );
    }
    #endregion // Geometry

    /// <summary>
    /// Retrieve the planar face normal and origin
    /// from all of the solid's planar faces and
    /// insert them into the map mapping face normals
    /// to a list of all origins of different faces
    /// sharing this normal.
    /// </summary>
    /// <param name="naos">Map mapping each normal vector
    /// to a list of the origins of all planar faces
    /// sharing this normal direction</param>
    /// <param name="solid">Input solid</param>
    void getFaceNaos(
      Dictionary<XYZ, List<XYZ>> naos,
      Solid solid )
    {
      foreach( Face face in solid.Faces )
      {
        PlanarFace planarFace = face as PlanarFace;
        if( null != planarFace )
        {
          XYZ normal = planarFace.Normal;
          XYZ origin = planarFace.Origin;
          List<XYZ> normals = new List<XYZ>( naos.Keys );
          int i = normals.FindIndex(
            delegate( XYZ v )
            {
              return XyzParallel( v, normal );
            } );

          if( -1 == i )
          {
            Debug.Print(
                "Face at {0} has new normal {1}",
                Util.PointString( origin ),
                Util.PointString( normal ) );

            naos.Add( normal, new List<XYZ>() );
            naos[normal].Add( origin );
          }
          else
          {
            Debug.Print(
                "Face at {0} normal {1} matches {2}",
                Util.PointString( origin ),
                Util.PointString( normal ),
                Util.PointString( normals[i] ) );

            naos[normals[i]].Add( origin );
          }
        }
      }
    }

    /// <summary>
    /// Calculate the maximum distance between
    /// the given set of points in the given
    /// normal direction.
    /// </summary>
    /// <param name="pts">Points to compare</param>
    /// <param name="normal">Normal direction</param>
    /// <returns>Max distance along normal</returns>
    double getMaxDistanceAlongNormal(
      List<XYZ> pts,
      XYZ normal )
    {
      int i, j;
      int n = pts.Count;
      double dmax = 0;

      for( i = 0; i < n - 1; ++i )
      {
        for( j = i + 1; j < n; ++j )
        {
          XYZ v = pts[i].Subtract( pts[j] );
          double d = v.DotProduct( normal );
          if( d > dmax )
          {
            dmax = d;
          }
        }
      }
      return dmax;
    }

    /// <summary>
    /// Create a string listing the
    /// dimensions from a dictionary
    /// of normal vectors with associated
    /// face origins.
    /// </summary>
    /// <param name="naos">Normals and origins</param>
    /// <returns>Formatted string of dimensions</returns>
    string getDimensions(
      Dictionary<XYZ, List<XYZ>> naos )
    {
      string s, ret = string.Empty;

      foreach( NormalAndOrigins pair in naos )
      {
        XYZ normal = pair.Key.Normalize();
        List<XYZ> pts = pair.Value;

        if (1 == pts.Count)
        {
          s = string.Format(
              "Only one wall face in "
              + "direction {0} found.",
            Util.PointString(normal));
        }
        else
        {
          double dmax = getMaxDistanceAlongNormal(
            pts, normal );

          s = string.Format(
              "Max wall dimension in "
              + "direction {0} is {1} feet.",
              Util.PointString( normal ),
              Util.RealString( dmax ) );
        }
        Debug.WriteLine( s );
        ret += "\n" + s;
      }
      return ret;
    }

    private string ProcessWall( Wall wall )
    {
      string msg = string.Format(
        "Wall <{0} {1}>:",
        wall.Name, wall.Id.IntegerValue );

      Debug.WriteLine( msg );

      Options o = wall.Document.Application.Create.NewGeometryOptions();
      GeometryElement ge = wall.get_Geometry( o );

      //GeometryObjectArray objs = ge.Objects; // 2012

      IEnumerable<GeometryObject> objs = ge; // 2013
      
      // face normals and origins:
      Dictionary<XYZ, List<XYZ>> naos
         = new Dictionary<XYZ, List<XYZ>>();

      foreach( GeometryObject obj in objs )
      {
        Solid solid = obj as Solid;
        if( null != solid )
        {
          getFaceNaos( naos, solid );
        }
      }
      return msg
        + getDimensions( naos )
        + "\n";
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Selection sel = uidoc.Selection;
      string msg = string.Empty;

      foreach( Element e in sel.Elements )
      {
        Wall wall = e as Wall;
        if( null != wall )
        {
          msg += ProcessWall( wall );
        }
      }
      if( 0 == msg.Length )
      {
        msg = "Please select some walls.";
      }
      Util.InfoMsg( msg );
      return Result.Succeeded;
    }
  }
}
