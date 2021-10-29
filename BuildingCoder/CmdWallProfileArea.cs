#region Header

//
// CmdWallProfileAreas.cs - determine wall
// elevation profile boundary loop polygon areas
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdWallProfileArea : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var walls = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                walls, uidoc, typeof(Wall)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some wall elements."
                    : "No wall elements found.";
                return Result.Failed;
            }

            var opt = app.Application.Create.NewGeometryOptions();

            var polygons
                = CmdWallProfile.GetWallProfilePolygons(
                    walls, opt);

            int i = 0, n = polygons.Count;
            var areas = new double[n];
            double d, a, maxArea = 0.0;
            XYZ normal;
            foreach (var polygon in polygons)
            {
                GetPolygonPlane(polygon,
                    out normal, out d, out a);
                if (Math.Abs(maxArea) < Math.Abs(a)) maxArea = a;
                areas[i++] = a;

#if DEBUG

                // transform the 3D polygon into a horizontal plane
                // so we can use the 2D GetSignedPolygonArea() and
                // compare its results with the 3D calculation.

                // todo: compare the relative speed of
                // transforming 3d to 2d and using 2d area
                // calculation versus direct 3d area calculation.


                var t = GetTransformToZ(normal);

                var polygonHorizontal
                    = ApplyTransform(polygon, t);

                var polygon2d
                    = CmdSlabBoundaryArea.Flatten(
                        polygonHorizontal);

                var a2
                    = CmdSlabBoundaryArea.GetSignedPolygonArea(
                        polygon2d);

                Debug.Assert(Util.IsEqual(a, a2),
                    "expected same area from 2D and 3D calculations");
#endif
            }

            Debug.Print(
                "{0} boundary loop{1} found.",
                n, Util.PluralSuffix(n));

            for (i = 0; i < n; ++i)
                Debug.Print(
                    "  Loop {0} area is {1} square feet{2}",
                    i,
                    Util.RealString(areas[i]),
                    areas[i].Equals(maxArea)
                        ? ", outer loop of largest wall"
                        : "");

            var creator = new Creator(doc);

            using var tx = new Transaction(doc);
            tx.Start("Draw wall profile loops");
            creator.DrawPolygons(polygons);
            tx.Commit();

            return Result.Succeeded;
        }

        #region Three-dimensional polygon area

        /*
        /// <summary>
        /// Return the average of a list of values.
        /// Prerequisite: the underlying class T must supply
        /// operator*(double) and operator+(const T &).
        /// </summary>
        T Average<T>( List<T> a )
        {
          T result;
          bool first = true;
          foreach( T x in a )
          {
            if( first )
            {
              result = x;
            }
            else
            {
              result += x;
            }
          }
          return result * ( 1.0 / a.Count );
        }
    
        XYZ Sum( List<XYZ> a )
        {
          XYZ sum = XYZ.Zero;
          foreach( XYZ x in a )
          {
            sum += x;
          }
          return sum;
        }
    
        XYZ Average( List<XYZ> a )
        {
          return Sum( a ) * (1.0 / a.Count);
        }
    
        XYZ TriangleCenter( List<XYZ> pts )
        {
          Debug.Assert( 3 == pts.Count, "expected three points in triangle" );
          return Average( pts );
        }
        */

        /// <summary>
        ///     Return the plane properties of a given polygon,
        ///     i.e. the plane normal, area, and its distance
        ///     from the origin. Cf. also GetSignedPolygonArea.
        /// </summary>
        internal static bool GetPolygonPlane(
            List<XYZ> polygon,
            out XYZ normal,
            out double dist,
            out double area)
        {
            normal = XYZ.Zero;
            dist = area = 0.0;
            var n = null == polygon ? 0 : polygon.Count;
            var rc = 2 < n;
            switch (n)
            {
                case 3:
                {
                    // the general case returns a wrong result for the triangle
                    // ((-1 -1 -1) (1 -1 -1) (-1 -1 1)), so implement specific
                    // code for triangle:

                    var a = polygon[0];
                    var b = polygon[1];
                    var c = polygon[2];
                    var v = b - a;
                    normal = v.CrossProduct(c - a);
                    dist = normal.DotProduct(a);
                    break;
                }
                case 4:
                {
                    // more efficient code for 4-sided quadrilateral polygons

                    var a = polygon[0];
                    var b = polygon[1];
                    var c = polygon[2];
                    var d = polygon[3];

                    //normal = new XYZ(
                    //  ( c.Y - a.Y ) * ( d.Z - b.Z ) + ( c.Z - a.Z ) * ( b.Y - d.Y ),
                    //  ( c.Z - a.Z ) * ( d.X - b.X ) + ( c.X - a.X ) * ( b.Z - d.Z ),
                    //  ( c.X - a.X ) * ( d.Y - b.Y ) + ( c.Y - a.Y ) * ( b.X - d.X ) );

                    normal = (a - c).CrossProduct(b - d);

                    dist = 0.25 *
                           (normal.X * (a.X + b.X + c.X + d.X)
                            + normal.Y * (a.Y + b.Y + c.Y + d.Y)
                            + normal.Z * (a.Z + b.Z + c.Z + d.Z));
                    break;
                }
                case > 4:
                {
                    // general case for n-sided polygons

                    XYZ a;
                    var b = polygon[n - 2];
                    var c = polygon[n - 1];
                    var s = XYZ.Zero;

                    for (var i = 0; i < n; ++i)
                    {
                        a = b;
                        b = c;
                        c = polygon[i];

                        normal = new XYZ(
                            normal.X + b.Y * (c.Z - a.Z),
                            normal.Y + b.Z * (c.X - a.X),
                            normal.Z + b.X * (c.Y - a.Y));

                        s += c;
                    }

                    dist = s.DotProduct(normal) / n;
                    break;
                }
            }

            if (rc)
            {
                // the polygon area is half of the length
                // of the non-normalized normal vector of the plane:

                var length = normal.GetLength();
                rc = !Util.IsZero(length);
                Debug.Assert(rc);

                if (rc)
                {
                    normal /= length;
                    dist /= length;
                    area = 0.5 * length;
                }
            }

            return rc;
        }

        private double[] GetPolygonAreas(List<List<XYZ>> polygons)
        {
            int i = 0, n = polygons.Count;
            var areas = new double[n];
            double dist, area;
            XYZ normal;
            foreach (var polygon in polygons)
                if (GetPolygonPlane(polygon, out normal, out dist, out area))
                    areas[i++] = area;
            return areas;
        }

        #endregion // Three-dimensional polygon area

        #region Transform 3D plane to horizontal

        private Transform GetTransformToZ(XYZ v)
        {
            Transform t;

            var a = XYZ.BasisZ.AngleTo(v);

            if (Util.IsZero(a))
            {
                t = Transform.Identity;
            }
            else
            {
                var axis = Util.IsEqual(a, Math.PI)
                    ? XYZ.BasisX
                    : v.CrossProduct(XYZ.BasisZ);

                //t = Transform.get_Rotation( XYZ.Zero, axis, a ); // 2013
                t = Transform.CreateRotation(axis, a); // 2014
            }

            return t;
        }

        private List<XYZ> ApplyTransform(
            List<XYZ> polygon,
            Transform t)
        {
            var n = polygon.Count;

            var polygonTransformed
                = new List<XYZ>(n);

            foreach (var p in polygon) polygonTransformed.Add(t.OfPoint(p));
            return polygonTransformed;
        }

        #endregion // Transform 3D plane to horizontal
    }
}