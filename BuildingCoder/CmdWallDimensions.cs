#region Header

//
// CmdWallDimensions.cs - determine wall dimensions
// by iterating over wall geometry faces
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
using NormalAndOrigins
    = System.Collections.Generic.KeyValuePair<
        Autodesk.Revit.DB.XYZ, System.Collections.Generic.List<Autodesk.Revit.DB.XYZ>>;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     List dimensions for a quadrilateral wall with
    ///     openings. In this algorithm, we collect all
    ///     the faces with parallel normal vectors and
    ///     calculate the maximal distance between any
    ///     two pairs of them. This is the wall dimension
    ///     in that direction.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdWallDimensions : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var msg = string.Empty;

            //Selection sel = uidoc.Selection; // 2014
            //foreach( Element e in sel.Elements ) // 2014

            var walls = new List<Element>();

            if (Util.GetSelectedElementsOrAll(walls, uidoc, typeof(Wall)))
                foreach (Wall wall in walls)
                    msg += ProcessWall(wall);

            if (0 == msg.Length) msg = "Please select some walls.";

            Util.InfoMsg(msg);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Retrieve the planar face normal and origin
        ///     from all of the solid's planar faces and
        ///     insert them into the map mapping face normals
        ///     to a list of all origins of different faces
        ///     sharing this normal.
        /// </summary>
        /// <param name="naos">
        ///     Map mapping each normal vector
        ///     to a list of the origins of all planar faces
        ///     sharing this normal direction
        /// </param>
        /// <param name="solid">Input solid</param>
        private void getFaceNaos(
            Dictionary<XYZ, List<XYZ>> naos,
            Solid solid)
        {
            foreach (Face face in solid.Faces)
            {
                var planarFace = face as PlanarFace;
                if (null != planarFace)
                {
                    var normal = planarFace.FaceNormal;
                    var origin = planarFace.Origin;
                    var normals = new List<XYZ>(naos.Keys);
                    var i = normals.FindIndex(
                        delegate(XYZ v) { return XyzParallel(v, normal); });

                    if (-1 == i)
                    {
                        Debug.Print(
                            "Face at {0} has new normal {1}",
                            Util.PointString(origin),
                            Util.PointString(normal));

                        naos.Add(normal, new List<XYZ>());
                        naos[normal].Add(origin);
                    }
                    else
                    {
                        Debug.Print(
                            "Face at {0} normal {1} matches {2}",
                            Util.PointString(origin),
                            Util.PointString(normal),
                            Util.PointString(normals[i]));

                        naos[normals[i]].Add(origin);
                    }
                }
            }
        }

        /// <summary>
        ///     Calculate the maximum distance between
        ///     the given set of points in the given
        ///     normal direction.
        /// </summary>
        /// <param name="pts">Points to compare</param>
        /// <param name="normal">Normal direction</param>
        /// <returns>Max distance along normal</returns>
        private double getMaxDistanceAlongNormal(
            List<XYZ> pts,
            XYZ normal)
        {
            int i, j;
            var n = pts.Count;
            double dmax = 0;

            for (i = 0; i < n - 1; ++i)
            for (j = i + 1; j < n; ++j)
            {
                var v = pts[i].Subtract(pts[j]);
                var d = v.DotProduct(normal);
                if (d > dmax) dmax = d;
            }

            return dmax;
        }

        /// <summary>
        ///     Create a string listing the
        ///     dimensions from a dictionary
        ///     of normal vectors with associated
        ///     face origins.
        /// </summary>
        /// <param name="naos">Normals and origins</param>
        /// <returns>Formatted string of dimensions</returns>
        private string getDimensions(
            Dictionary<XYZ, List<XYZ>> naos)
        {
            string s, ret = string.Empty;

            foreach (var pair in naos)
            {
                var normal = pair.Key.Normalize();
                var pts = pair.Value;

                if (1 == pts.Count)
                {
                    s = string.Format(
                        "Only one wall face in "
                        + "direction {0} found.",
                        Util.PointString(normal));
                }
                else
                {
                    var dmax = getMaxDistanceAlongNormal(
                        pts, normal);

                    s = string.Format(
                        "Max wall dimension in "
                        + "direction {0} is {1} feet.",
                        Util.PointString(normal),
                        Util.RealString(dmax));
                }

                Debug.WriteLine(s);
                ret += $"\n{s}";
            }

            return ret;
        }

        private string ProcessWall(Wall wall)
        {
            var msg = $"Wall <{wall.Name} {wall.Id.IntegerValue}>:";

            Debug.WriteLine(msg);

            var o = wall.Document.Application.Create.NewGeometryOptions();
            var ge = wall.get_Geometry(o);

            //GeometryObjectArray objs = ge.Objects; // 2012

            IEnumerable<GeometryObject> objs = ge; // 2013

            // face normals and origins:
            var naos
                = new Dictionary<XYZ, List<XYZ>>();

            foreach (var obj in objs)
            {
                var solid = obj as Solid;
                if (null != solid) getFaceNaos(naos, solid);
            }

            return $"{msg}{getDimensions(naos)}\n";
        }

        #region Geometry

        private const double _eps = 1.0e-9;

        /// <summary>
        ///     Check whether two real numbers are equal
        /// </summary>
        private static bool DoubleEqual(double a, double b)
        {
            return Math.Abs(a - b) < _eps;
        }

        /// <summary>
        ///     Check whether two vectors are parallel
        /// </summary>
        private static bool XyzParallel(XYZ a, XYZ b)
        {
            var angle = a.AngleTo(b);
            return _eps > angle
                   || DoubleEqual(angle, Math.PI);
        }

        #endregion // Geometry
    }
}