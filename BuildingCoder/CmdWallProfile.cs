#region Header

//
// CmdWallProfile.cs - determine wall
// elevation profile boundary loop polygons
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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdWallProfile : IExternalCommand
    {
        /// <summary>
        ///     Offset the generated boundary polygon loop
        ///     model lines outwards to separate them from
        ///     the wall edge, measured in feet.
        /// </summary>
        private const double _offset = 1.0;

        public Result Execute(
            ExternalCommandData cd,
            ref string msg,
            ElementSet els)
        {
            // Choose which implementation to use.

            var use_execute_nr = 3;

            switch (use_execute_nr)
            {
                case 1: return Execute1(cd, ref msg, els);
                case 2: return Execute2(cd, ref msg, els);
                case 3: return Execute3(cd, ref msg, els);
            }

            return Result.Failed;
        }

        /// <summary>
        ///     Determine the elevation boundary profile
        ///     polygons of the exterior vertical planar
        ///     face of the given wall solid.
        /// </summary>
        /// <param name="polygons">
        ///     Return polygonal boundary
        ///     loops of exterior vertical planar face, i.e.
        ///     profile of wall elevation incl. holes
        /// </param>
        /// <param name="solid">Input solid</param>
        /// <param name="v">
        ///     Vector pointing along
        ///     wall centre line
        /// </param>
        /// <param name="w">
        ///     Vector pointing towards
        ///     exterior wall face
        /// </param>
        /// <returns>
        ///     False if no exterior vertical
        ///     planar face was found, else true
        /// </returns>
        private static bool GetProfile(
            List<List<XYZ>> polygons,
            Solid solid,
            XYZ v,
            XYZ w)
        {
            double d, dmax = 0;
            PlanarFace outermost = null;
            var faces = solid.Faces;
            foreach (Face f in faces)
            {
                var pf = f as PlanarFace;
                if (null != pf
                    && Util.IsVertical(pf)
                    && Util.IsZero(v.DotProduct(pf.FaceNormal)))
                {
                    d = pf.Origin.DotProduct(w);
                    if (null == outermost
                        || dmax < d)
                    {
                        outermost = pf;
                        dmax = d;
                    }
                }
            }

            if (null != outermost)
            {
                var voffset = _offset * w;
                XYZ p, q = XYZ.Zero;
                bool first;
                int i, n;
                var loops = outermost.EdgeLoops;
                foreach (EdgeArray loop in loops)
                {
                    var vertices = new List<XYZ>();
                    first = true;
                    foreach (Edge e in loop)
                    {
                        var points = e.Tessellate();
                        p = points[0];
                        if (!first)
                            Debug.Assert(p.IsAlmostEqualTo(q),
                                "expected subsequent start point"
                                + " to equal previous end point");
                        n = points.Count;
                        q = points[n - 1];
                        for (i = 0; i < n - 1; ++i)
                        {
                            var a = points[i];
                            a += voffset;
                            vertices.Add(a);
                        }
                    }

                    q += voffset;
                    Debug.Assert(q.IsAlmostEqualTo(vertices[0]),
                        "expected last end point to equal"
                        + " first start point");
                    polygons.Add(vertices);
                }
            }

            return null != outermost;
        }

        /// <summary>
        ///     Return all wall profile boundary loop polygons
        ///     for the given walls, offset out from the outer
        ///     face of the wall by a certain amount.
        /// </summary>
        public static List<List<XYZ>> GetWallProfilePolygons(
            //Application app,
            List<Element> walls,
            Options opt)
        {
            XYZ p, q, v, w;
            //Options opt = app.Create.NewGeometryOptions();
            var polygons = new List<List<XYZ>>();

            foreach (Wall wall in walls)
            {
                var desc = Util.ElementDescription(wall);

                if (wall.Location is not LocationCurve curve)
                    throw new Exception($"{desc}: No wall curve found.");
                p = curve.Curve.GetEndPoint(0);
                q = curve.Curve.GetEndPoint(1);
                v = q - p;
                v = v.Normalize();
                w = XYZ.BasisZ.CrossProduct(v).Normalize();
                if (wall.Flipped) w = -w;

                var geo = wall.get_Geometry(opt);

                //GeometryObjectArray objects = geo.Objects; // 2012
                //foreach( GeometryObject obj in objects ) // 2012

                foreach (var obj in geo) // 2013
                {
                    var solid = obj as Solid;
                    if (solid != null) GetProfile(polygons, solid, v, w);
                }
            }

            return polygons;
        }

        /// <summary>
        ///     Original implementation published November 17, 2008:
        ///     http://thebuildingcoder.typepad.com/blog/2008/11/wall-elevation-profile.html
        /// </summary>
        public Result Execute1(
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
                = GetWallProfilePolygons(walls, opt);

            var n = polygons.Count;

            Debug.Print(
                "{0} boundary loop{1} found.",
                n, Util.PluralSuffix(n));

            var creator = new Creator(doc);

            using var tx = new Transaction(doc);
            tx.Start("Draw Wall Elevation Profile Model Lines");
            creator.DrawPolygons(polygons);
            tx.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Alternative implementation published January 23, 2015:
        ///     http://thebuildingcoder.typepad.com/blog/2015/01/getting-the-wall-elevation-profile.html
        /// </summary>
        public Result Execute2(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            var creapp
                = app.Create;

            var credoc
                = doc.Create;

            var r = uidoc.Selection.PickObject(
                ObjectType.Element, "Select a wall");

            var e = uidoc.Document.GetElement(r);

            var wall = e as Wall;

            using var tx = new Transaction(doc);
            tx.Start("Wall Profile");

            // Get the external wall face for the profile

            var sideFaces
                = HostObjectUtils.GetSideFaces(wall,
                    ShellLayerType.Exterior);

            var e2 = doc.GetElement(sideFaces[0]);

            var face = e2.GetGeometryObjectFromReference(
                sideFaces[0]) as Face;

            // The normal of the wall external face.

            var normal = face.ComputeNormal(new UV(0, 0));

            // Offset curve copies for visibility.

            var offset = Transform.CreateTranslation(
                5 * normal);

            // If the curve loop direction is counter-
            // clockwise, change its color to RED.

            var colorRed = new Color(255, 0, 0);

            // Get edge loops as curve loops.

            var curveLoops
                = face.GetEdgesAsCurveLoops();

            // ExporterIFCUtils class can also be used for 
            // non-IFC purposes. The SortCurveLoops method 
            // sorts curve loops (edge loops) so that the 
            // outer loops come first.

            var curveLoopLoop
                = ExporterIFCUtils.SortCurveLoops(
                    curveLoops);

            foreach (var curveLoops2
                in curveLoopLoop)
            foreach (var curveLoop2 in curveLoops2)
            {
                // Check if curve loop is counter-clockwise.

                var isCCW = curveLoop2.IsCounterclockwise(
                    normal);

                var curves = creapp.NewCurveArray();

                foreach (var curve in curveLoop2) curves.Append(curve.CreateTransformed(offset));

                // Create model lines for an curve loop.

                //Plane plane = creapp.NewPlane( curves ); // 2016

                var plane = curveLoop2.GetPlane(); // 2017

                var sketchPlane
                    = SketchPlane.Create(doc, plane);

                var curveElements
                    = credoc.NewModelCurveArray(curves,
                        sketchPlane);

                if (isCCW)
                    foreach (ModelCurve mcurve in curveElements)
                    {
                        var overrides
                            = view.GetElementOverrides(
                                mcurve.Id);

                        overrides.SetProjectionLineColor(
                            colorRed);

                        view.SetElementOverrides(
                            mcurve.Id, overrides);
                    }
            }

            tx.Commit();

            return Result.Succeeded;
        }

        private void SetModelCurvesColor(
            ModelCurveArray modelCurves,
            View view,
            Color color)
        {
            foreach (var curve in modelCurves
                .Cast<ModelCurve>())
            {
                var overrides = view.GetElementOverrides(
                    curve.Id);

                overrides.SetProjectionLineColor(color);

                view.SetElementOverrides(curve.Id, overrides);
            }
        }

        /// <summary>
        ///     Improved implementation by Alexander Ignatovich
        ///     supporting curved wall with curved window,
        ///     second attempt, published April 10, 2015:
        /// </summary>
        public Result Execute3(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            var creapp
                = app.Create;

            var credoc
                = doc.Create;

            var r = uidoc.Selection.PickObject(
                ObjectType.Element, "Select a wall");

            var e = uidoc.Document.GetElement(r);

            var creator = new Creator(doc);

            if (e is not Wall wall) return Result.Cancelled;

            using var tx = new Transaction(doc);
            tx.Start("Wall Profile");

            // Get the external wall face for the profile
            // a little bit simpler than in the last 
            // implementation in Execute2.

            var sideFaceReference
                = HostObjectUtils.GetSideFaces(
                        wall, ShellLayerType.Exterior)
                    .First();

            var face = wall.GetGeometryObjectFromReference(
                sideFaceReference) as Face;

            // The plane and normal of the wall external face.

            var normal = wall.Orientation.Normalize();
            var ftx = face.ComputeDerivatives(UV.Zero);
            var forigin = ftx.Origin;
            var fnormal = ftx.BasisZ;

            Debug.Print(
                "wall orientation {0}, face origin {1}, face normal {2}",
                Util.PointString(normal),
                Util.PointString(forigin),
                Util.PointString(fnormal));

            // Offset distance.

            double d = 5;

            // Offset curve copies for visibility.

            var voffset = d * normal;
            var offset = Transform.CreateTranslation(
                voffset);

            // If the curve loop direction is counter-
            // clockwise, change its color to RED.

            var colorRed = new Color(255, 0, 0);

            // Get edge loops as curve loops.

            var curveLoops
                = face.GetEdgesAsCurveLoops();

            foreach (var curveLoop in curveLoops)
            {
                //CurveLoop curveLoopOffset = CurveLoop.CreateViaOffset(
                //  curveLoop, d, normal );

                var curves = creapp.NewCurveArray();

                foreach (var curve in curveLoop)
                    curves.Append(curve.CreateTransformed(
                        offset));

                var isCounterClockwize = curveLoop
                    .IsCounterclockwise(normal);

                // Create model lines for an curve loop if it is made 

                var wallCurve = ((LocationCurve) wall.Location).Curve;

                if (wallCurve is Line)
                {
                    //Plane plane = creapp.NewPlane( curves ); // 2016

                    //Plane plane = curveLoopOffset.GetPlane(); // 2017

                    var plane = Plane.CreateByNormalAndOrigin( // 2019
                        normal, forigin + voffset);

                    Debug.Print(
                        "plane origin {0}, plane normal {1}",
                        Util.PointString(plane.Origin),
                        Util.PointString(plane.Normal));

                    var sketchPlane
                        = SketchPlane.Create(doc, plane);

                    var curveElements = credoc
                        .NewModelCurveArray(curves, sketchPlane);

                    if (isCounterClockwize)
                        SetModelCurvesColor(curveElements,
                            view, colorRed);
                }
                else
                {
                    foreach (var curve in curves.Cast<Curve>())
                    {
                        var curveElements = creator.CreateModelCurves(curve);
                        if (isCounterClockwize) SetModelCurvesColor(curveElements, view, colorRed);
                    }
                }
            }

            tx.Commit();

            return Result.Succeeded;
        }
    }
}