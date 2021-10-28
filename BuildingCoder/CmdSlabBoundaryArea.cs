#region Header

//
// CmdSlabBoundaryArea.cs - determine
// slab boundary polygon loops and areas
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
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSlabBoundaryArea : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var floors = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                floors, uidoc, typeof(Floor)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some floor elements."
                    : "No floor elements found.";
                return Result.Failed;
            }

            var opt = app.Application.Create.NewGeometryOptions();

            var polygons
                = CmdSlabBoundary.GetFloorBoundaryPolygons(
                    floors, opt);

            var flat_polygons
                = Flatten(polygons);

            int i = 0, n = flat_polygons.Count;
            var areas = new double[n];
            double a, maxArea = 0.0;
            foreach (var polygon in flat_polygons)
            {
                a = GetSignedPolygonArea(polygon);
                if (Math.Abs(maxArea) < Math.Abs(a)) maxArea = a;
                areas[i++] = a;
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
                        ? ", outer loop of largest floor slab"
                        : "");

            using var t = new Transaction(doc);
            t.Start("Draw Polygons");

            var creator = new Creator(doc);
            creator.DrawPolygons(polygons);

            t.Commit();

            return Result.Succeeded;
        }

        #region Two-dimensional polygon area

        /// <summary>
        ///     Use the formula
        ///     area = sign * 0.5 * sum( xi * ( yi+1 - yi-1 ) )
        ///     to determine the winding direction (clockwise
        ///     or counter) and area of a 2D polygon.
        ///     Cf. also GetPolygonPlane.
        /// </summary>
        public static double GetSignedPolygonArea(List<UV> p)
        {
            var n = p.Count;
            var sum = p[0].U * (p[1].V - p[n - 1].V); // loop at beginning
            for (var i = 1; i < n - 1; ++i) sum += p[i].U * (p[i + 1].V - p[i - 1].V);
            sum += p[n - 1].U * (p[0].V - p[n - 2].V); // loop at end
            return 0.5 * sum;
        }

        #endregion // Two-dimensional polygon area

        #region Rpthomas108 improved solution

        // In Revit API discussion forum thread
        // https://forums.autodesk.com/t5/revit-api-forum/outer-loops-of-planar-face-with-separate-parts/m-p/7461348

        public Result GetPlanarFaceOuterLoops(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var IntApp = commandData.Application;
            var IntUIDoc = IntApp.ActiveUIDocument;
            if (IntUIDoc == null)
                return Result.Failed;
            var IntDoc = IntUIDoc.Document;

            Reference R = null;
            try
            {
                R = IntUIDoc.Selection.PickObject(ObjectType.Face);
            }
            catch
            {
            }

            if (R == null)
                return Result.Cancelled;

            var F_El = IntDoc.GetElement(R.ElementId);
            if (F_El == null)
                return Result.Failed;

            var F = F_El.GetGeometryObjectFromReference(R)
                as PlanarFace;

            if (F == null)
                return Result.Failed;

            //Create individual CurveLoops to compare from 
            // the orginal CurveLoopArray
            //If floor has separate parts these will now be 
            // separated out into individual faces rather 
            // than one face with multiple loops.
            var CLoop
                = new List<Tuple<PlanarFace, CurveLoop, int>>();

            var Ix = 0;
            foreach (var item in F.GetEdgesAsCurveLoops())
            {
                var CLL = new List<CurveLoop>();
                CLL.Add(item);
                //Create a solid extrusion for each CurveLoop 
                // ( we want to get the planarFace from this 
                // to use built in functionality (.PlanarFace.IsInside).
                //Would be nice if you could skip this step and 
                // create PlanarFaces directly from CuveLoops? 
                // Does not appear to be possible, I only looked 
                // in GeometryCreationUtilities.
                //Below creates geometry in memory rather than 
                // actual geometry in the document, therefore 
                // no transaction required.
                var S = GeometryCreationUtilities
                    .CreateExtrusionGeometry(CLL, F.FaceNormal, 1);

                foreach (Face Fx in S.Faces)
                {
                    var PFx = Fx as PlanarFace;
                    if (PFx == null)
                        continue;
                    if (PFx.FaceNormal.IsAlmostEqualTo(
                        F.FaceNormal))
                    {
                        Ix += 1;
                        CLoop.Add(new Tuple<PlanarFace,
                            CurveLoop, int>(PFx, item, Ix));
                    }
                }
            }

            var OuterLoops = new List<CurveLoop>();
            //If there is more than one outerloop we know the 
            // original face has separate parts.
            //We could therefore stop the creation of floors 
            // with separate parts via posting failures etc. 
            // or more passively create a geometry checking
            // utility to identify them.
            var InnerLoops = new List<CurveLoop>();
            foreach (var item in CLoop)
            {
                //To identify an inner loop we just need to see 
                // if any of it's points are inside another face.
                //The exception to this is a loop compared to the
                // face it was taken from. This will also be 
                // considered inside as the points are on the boundary.
                //Therefore give each item an integer ID to ensure
                // it isn't self comparing. An alternative would
                // be to look for J=1 instead of J=0 below (perhaps).

                var J = CLoop.ToList().FindAll(z
                    => FirstPointIsInsideFace(item.Item2, z.Item1) && z.Item3 != item.Item3).Count;

                if (J == 0)
                    OuterLoops.Add(item.Item2);
                else
                    InnerLoops.Add(item.Item2);
            }

            using var Tx = new Transaction(IntDoc,
                "Outer loops");
            if (Tx.Start() == TransactionStatus.Started)
            {
                var SKP = SketchPlane.Create(IntDoc,
                    Plane.CreateByThreePoints(F.Origin,
                        F.Origin + F.XVector, F.Origin + F.YVector));

                foreach (var Crv in OuterLoops)
                foreach (var C in Crv)
                    IntDoc.Create.NewModelCurve(C, SKP);
                Tx.Commit();
            }

            return Result.Succeeded;
        }

        public bool FirstPointIsInsideFace(
            CurveLoop CL,
            PlanarFace PFace)
        {
            var Trans = PFace.ComputeDerivatives(
                new UV(0, 0));
            if (CL.Count() == 0)
                return false;
            var Pt = Trans.Inverse.OfPoint(
                CL.ToList()[0].GetEndPoint(0));
            IntersectionResult Res = null;
            var outval = PFace.IsInside(
                new UV(Pt.X, Pt.Y), out Res);
            return outval;
        }

        #endregion // Rpthomas108 improved solution

        #region Rpthomas108 first solution searching for minimum point

        // In Revit API discussion forum thread
        // https://forums.autodesk.com/t5/revit-api-forum/is-the-first-edgeloop-still-the-outer-loop/m-p/7225379

        public static double MinU(Curve C, Face F)
        {
            return C.Tessellate()
                .Select(p => F.Project(p))
                .Min(ir => ir.UVPoint.U);
        }

        public static double MinX(Curve C, Transform Tinv)
        {
            return C.Tessellate()
                .Select(p => Tinv.OfPoint(p))
                .Min(p => p.X);
        }

        public static EdgeArray OuterLoop(Face F)
        {
            EdgeArray eaMin = null;
            var loops = F.EdgeLoops;
            var uMin = double.MaxValue;
            foreach (EdgeArray a in loops)
            {
                var uMin2 = double.MaxValue;
                foreach (Edge e in a)
                {
                    var min = MinU(e.AsCurve(), F);
                    if (min < uMin2) uMin2 = min;
                }

                if (uMin2 < uMin)
                {
                    uMin = uMin2;
                    eaMin = a;
                }
            }

            return eaMin;
        }

        public static EdgeArray PlanarFaceOuterLoop(Face F)
        {
            var face = F as PlanarFace;
            if (face == null) return null;
            var T = Transform.Identity;
            T.BasisZ = face.FaceNormal;
            T.BasisX = face.XVector;
            T.BasisY = face.YVector;
            T.Origin = face.Origin;
            var Tinv = T.Inverse;

            EdgeArray eaMin = null;
            var loops = F.EdgeLoops;
            var uMin = double.MaxValue;
            foreach (EdgeArray a in loops)
            {
                var uMin2 = double.MaxValue;
                foreach (Edge e in a)
                {
                    var min = MinX(e.AsCurve(), Tinv);
                    if (min < uMin2) uMin2 = min;
                }

                if (uMin2 < uMin)
                {
                    uMin = uMin2;
                    eaMin = a;
                }
            }

            return eaMin;
        }

        #endregion // Rpthomas108 first solution searching for minimum point

        #region Flatten, i.e. project from 3D to 2D by dropping the Z coordinate

        /// <summary>
        ///     Eliminate the Z coordinate.
        /// </summary>
        private static UV Flatten(XYZ point)
        {
            return new UV(point.X, point.Y);
        }

        /// <summary>
        ///     Eliminate the Z coordinate.
        /// </summary>
        public static List<UV> Flatten(List<XYZ> polygon)
        {
            var z = polygon[0].Z;
            var a = new List<UV>(polygon.Count);
            foreach (var p in polygon)
            {
                Debug.Assert(Util.IsEqual(p.Z, z),
                    "expected horizontal polygon");
                a.Add(Flatten(p));
            }

            return a;
        }

        /// <summary>
        ///     Eliminate the Z coordinate.
        /// </summary>
        private static List<List<UV>> Flatten(List<List<XYZ>> polygons)
        {
            var z = polygons[0][0].Z;
            var a = new List<List<UV>>(polygons.Count);
            foreach (var polygon in polygons)
            {
                Debug.Assert(Util.IsEqual(polygon[0].Z, z),
                    "expected horizontal polygons");
                a.Add(Flatten(polygon));
            }

            return a;
        }

        #endregion // Flatten, i.e. project from 3D to 2D by dropping the Z coordinate
    }
}