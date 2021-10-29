#region Header

//
// Creator.cs - model line creator helper class
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
using System.Linq;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Document = Autodesk.Revit.Creation.Document;

#endregion // Namespaces

namespace BuildingCoder
{
    internal class Creator
    {
        private readonly Document _credoc;

        private readonly Autodesk.Revit.DB.Document _doc;

        // these are
        // Autodesk.Revit.Creation
        // objects!
        private Application _creapp;

        public Creator(Autodesk.Revit.DB.Document doc)
        {
            _doc = doc;
            _credoc = doc.Create;
            _creapp = doc.Application.Create;
        }

        /// <summary>
        ///     Determine the plane that a given curve resides in and return its normal vector.
        ///     Ask the curve for its start and end points and some point in the middle.
        ///     The latter can be obtained by asking the curve for its parameter range and
        ///     evaluating it in the middle, or by tessellation. In case of tessellation,
        ///     you could iterate through the tessellation points and use each one together
        ///     with the start and end points to try and determine a valid plane.
        ///     Once one is found, you can add debug assertions to ensure that the other
        ///     tessellation points (if there are any more) are in the same plane.
        ///     In the case of the line, the tessellation only returns two points.
        ///     I once heard that that is the only element that can do that, all
        ///     non-linear curves return at least three. So you could use this property
        ///     to determine that a line is a line (and add an assertion as well, if you like).
        ///     Update, later: please note that the Revit API provides an overload of the
        ///     NewPlane method taking a CurveArray argument.
        /// </summary>
        private XYZ GetCurveNormal(Curve curve)
        {
            var pts = curve.Tessellate();
            var n = pts.Count;

            Debug.Assert(1 < n,
                "expected at least two points "
                + "from curve tessellation");

            var p = pts[0];
            var q = pts[n - 1];
            var v = q - p;
            XYZ w, normal = null;

            if (2 == n)
            {
                Debug.Assert(curve is Line,
                    "expected non-line element to have "
                    + "more than two tessellation points");

                // For non-vertical lines, use Z axis to
                // span the plane, otherwise Y axis:

                var dxy = Math.Abs(v.X) + Math.Abs(v.Y);

                w = dxy > Util.TolPointOnPlane
                    ? XYZ.BasisZ
                    : XYZ.BasisY;

                normal = v.CrossProduct(w).Normalize();
            }
            else
            {
                var i = 0;
                while (++i < n - 1)
                {
                    w = pts[i] - p;
                    normal = v.CrossProduct(w);
                    if (!normal.IsZeroLength())
                    {
                        normal = normal.Normalize();
                        break;
                    }
                }

#if DEBUG
                {
                    XYZ normal2;
                    while (++i < n - 1)
                    {
                        w = pts[i] - p;
                        normal2 = v.CrossProduct(w);
                        Debug.Assert(normal2.IsZeroLength()
                                     || Util.IsZero(normal2.AngleTo(normal)),
                            "expected all points of curve to "
                            + "lie in same plane");
                    }
                }
#endif // DEBUG
            }

            return normal;
        }

        /// <summary>
        ///     Create a model line between the two given points.
        ///     Internally, it creates an arbitrary sketch
        ///     plane given the model line end points.
        /// </summary>
        public static ModelLine CreateModelLine(
            Autodesk.Revit.DB.Document doc,
            XYZ p,
            XYZ q)
        {
            if (p.DistanceTo(q) < Util.MinLineLength) return null;

            // Create sketch plane; for non-vertical lines,
            // use Z-axis to span the plane, otherwise Y-axis:

            var v = q - p;

            var dxy = Math.Abs(v.X) + Math.Abs(v.Y);

            var w = dxy > Util.TolPointOnPlane
                ? XYZ.BasisZ
                : XYZ.BasisY;

            var norm = v.CrossProduct(w).Normalize();

            //Autodesk.Revit.Creation.Application creApp
            //  = doc.Application.Create;

            //Plane plane = creApp.NewPlane( norm, p ); // 2014
            //Plane plane = new Plane( norm, p ); // 2015, 2016
            var plane = Plane.CreateByNormalAndOrigin(norm, p); // 2017

            //SketchPlane sketchPlane = creDoc.NewSketchPlane( plane ); // 2013
            var sketchPlane = SketchPlane.Create(doc, plane); // 2014

            //Line line = creApp.NewLine( p, q, true ); // 2013
            var line = Line.CreateBound(p, q); // 2014

            // The following line is only valid in a project 
            // document. In a family, it will throw an exception 
            // saying "Document.Create can only be used with 
            // project documents. Use Document.FamilyCreate 
            // in the Family Editor."

            //Autodesk.Revit.Creation.Document creDoc
            //  = doc.Create;

            //return creDoc.NewModelCurve(
            //  //creApp.NewLine( p, q, true ), // 2013
            //  Line.CreateBound( p, q ), // 2014
            //  sketchPlane ) as ModelLine;

            var curve = doc.IsFamilyDocument
                ? doc.FamilyCreate.NewModelCurve(line, sketchPlane)
                : doc.Create.NewModelCurve(line, sketchPlane);

            return curve as ModelLine;
        }

        private SketchPlane NewSketchPlanePassLine(
            Line line)
        {
            var p = line.GetEndPoint(0);
            var q = line.GetEndPoint(1);
            XYZ norm;
            if (p.X == q.X)
                norm = XYZ.BasisX;
            else if (p.Y == q.Y)
                norm = XYZ.BasisY;
            else
                norm = XYZ.BasisZ;
            //Plane plane = _creapp.NewPlane( norm, p ); // 2016
            var plane = Plane.CreateByNormalAndOrigin(norm, p); // 2017

            //return _credoc.NewSketchPlane( plane ); // 2013

            return SketchPlane.Create(_doc, plane); // 2014
        }

        //public void CreateModelLine( XYZ p, XYZ q )
        //{
        //  if( p.IsAlmostEqualTo( q ) )
        //  {
        //    throw new ArgumentException(
        //      "Expected two different points." );
        //  }
        //  Line line = Line.CreateBound( p, q );
        //  if( null == line )
        //  {
        //    throw new Exception(
        //      "Geometry line creation failed." );
        //  }
        //  _credoc.NewModelCurve( line,
        //    NewSketchPlanePassLine( line ) );
        //}

        /// <summary>
        ///     Return a new sketch plane containing the given curve.
        ///     Update, later: please note that the Revit API provides
        ///     an overload of the NewPlane method taking a CurveArray
        ///     argument, which could presumably be used instead.
        /// </summary>
        private SketchPlane NewSketchPlaneContainCurve(
            Curve curve)
        {
            var p = curve.GetEndPoint(0);
            var normal = GetCurveNormal(curve);

            //Plane plane = _creapp.NewPlane( normal, p ); // 2016
            var plane = Plane.CreateByNormalAndOrigin(normal, p); // 2017

#if DEBUG
            if (!(curve is Line))
            {
                //CurveArray a = _creapp.NewCurveArray();
                //a.Append( curve );
                //Plane plane2 = _creapp.NewPlane( a ); // 2016

                var a = new List<Curve>(1);
                a.Add(curve);
                var b = CurveLoop.Create(a);
                var plane2 = b.GetPlane(); // 2017


                Debug.Assert(Util.IsParallel(plane2.Normal,
                    plane.Normal), "expected equal planes");

                Debug.Assert(Util.IsZero(plane2.SignedDistanceTo(
                    plane.Origin)), "expected equal planes");
            }
#endif // DEBUG

            //return _credoc.NewSketchPlane( plane ); // 2013

            return SketchPlane.Create(_doc, plane); // 2014
        }

        public ModelCurve CreateModelCurve(Curve curve)
        {
            return _credoc.NewModelCurve(curve,
                NewSketchPlaneContainCurve(curve));
        }

        private ModelCurve CreateModelCurve(
            Curve curve,
            XYZ origin,
            XYZ normal)
        {
            //Plane plane = _creapp.NewPlane( normal, origin ); // 2016
            var plane = Plane.CreateByNormalAndOrigin(
                normal, origin); // 2017

            var sketchPlane = SketchPlane.Create(
                _doc, plane);

            return _credoc.NewModelCurve(
                curve, sketchPlane);
        }

        public ModelCurveArray CreateModelCurves(
            Curve curve)
        {
            var array = new ModelCurveArray();

            var line = curve as Line;
            if (line != null)
            {
                array.Append(CreateModelLine(_doc,
                    curve.GetEndPoint(0),
                    curve.GetEndPoint(1)));

                return array;
            }

            var arc = curve as Arc;
            if (arc != null)
            {
                var origin = arc.Center;
                var normal = arc.Normal;

                array.Append(CreateModelCurve(
                    arc, origin, normal));

                return array;
            }

            var ellipse = curve as Ellipse;
            if (ellipse != null)
            {
                var origin = ellipse.Center;
                var normal = ellipse.Normal;

                array.Append(CreateModelCurve(
                    ellipse, origin, normal));

                return array;
            }

            var points = curve.Tessellate();
            var p = points.First();

            foreach (var q in points.Skip(1))
            {
                array.Append(CreateModelLine(_doc, p, q));
                p = q;
            }

            return array;
        }

        public void DrawPolygon(
            List<XYZ> loop)
        {
            var p1 = XYZ.Zero;
            var q = XYZ.Zero;
            var first = true;
            foreach (var p in loop)
            {
                if (first)
                {
                    p1 = p;
                    first = false;
                }
                else
                {
                    CreateModelLine(_doc, p, q);
                }

                q = p;
            }

            CreateModelLine(_doc, q, p1);
        }

        public void DrawPolygons(
            List<List<XYZ>> loops)
        {
            foreach (var loop in loops) DrawPolygon(loop);
        }

        public void DrawFaceTriangleNormals(Face f)
        {
            var mesh = f.Triangulate();
            var n = mesh.NumTriangles;

            var s = "{0} face triangulation returns "
                    + "mesh triangle{1} and normal vector{1}:";

            Debug.Print(
                s, n, Util.PluralSuffix(n));

            for (var i = 0; i < n; ++i)
            {
                var t = mesh.get_Triangle(i);

                var p = (t.get_Vertex(0)
                         + t.get_Vertex(1)
                         + t.get_Vertex(2)) / 3;

                var v = t.get_Vertex(1)
                        - t.get_Vertex(0);

                var w = t.get_Vertex(2)
                        - t.get_Vertex(0);

                var normal = v.CrossProduct(w).Normalize();

                Debug.Print(
                    "{0} {1} --> {2}", i,
                    Util.PointString(p),
                    Util.PointString(normal));

                CreateModelLine(_doc, p, p + normal);
            }
        }

        /// <summary>
        ///     Create a TextNote on the specified XYZ.
        ///     This function is useful during debugging to attach
        ///     a label to points in space
        /// </summary>
        public static TextNote CreateTextNote(string text, XYZ origin, Autodesk.Revit.DB.Document doc)
        {
            var options = new TextNoteOptions
            {
                HorizontalAlignment = HorizontalTextAlignment.Center,
                VerticalAlignment = VerticalTextAlignment.Middle,
                TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType)
            };

            return TextNote.Create(doc, doc.ActiveView.Id, origin, text, options);
        }
    }
}