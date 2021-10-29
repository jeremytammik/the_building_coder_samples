#region Header

//
// CmdSlabBoundary.cs - determine polygonal slab boundary loops
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSlabBoundary : IExternalCommand
    {
        /// <summary>
        ///     Offset the generated boundary polygon loop
        ///     model lines downwards to separate them from
        ///     the slab edge.
        /// </summary>
        private const double _offset = 0.1;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            // retrieve selected floors, or all floors, if nothing is selected:

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
                = GetFloorBoundaryPolygons(floors, opt);

            var n = polygons.Count;

            Debug.Print(
                "{0} boundary loop{1} found.",
                n, Util.PluralSuffix(n));

            var creator = new Creator(doc);

            using var t = new Transaction(doc);
            t.Start("Draw Slab Boundaries");

            creator.DrawPolygons(polygons);

            t.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Determine the boundary polygons of the lowest
        ///     horizontal planar face of the given solid.
        /// </summary>
        /// <param name="polygons">
        ///     Return polygonal boundary
        ///     loops of lowest horizontal face, i.e. profile of
        ///     circumference and holes
        /// </param>
        /// <param name="solid">Input solid</param>
        /// <returns>
        ///     False if no horizontal planar face was
        ///     found, else true
        /// </returns>
        private static bool GetBoundary(
            List<List<XYZ>> polygons,
            Solid solid)
        {
            PlanarFace lowest = null;
            var faces = solid.Faces;
            foreach (Face f in faces)
            {
                var pf = f as PlanarFace;
                if (null != pf && Util.IsHorizontal(pf))
                    if (null == lowest
                        || pf.Origin.Z < lowest.Origin.Z)
                        lowest = pf;
            }

            if (null != lowest)
            {
                XYZ p, q = XYZ.Zero;
                bool first;
                int i, n;
                var loops = lowest.EdgeLoops;
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
                            var v = points[i];
                            v -= _offset * XYZ.BasisZ;
                            vertices.Add(v);
                        }
                    }

                    q -= _offset * XYZ.BasisZ;
                    Debug.Assert(q.IsAlmostEqualTo(vertices[0]),
                        "expected last end point to equal"
                        + " first start point");
                    polygons.Add(vertices);
                }
            }

            return null != lowest;
        }

        /// <summary>
        ///     Return all floor slab boundary loop polygons
        ///     for the given floors, offset downwards from the
        ///     bottom floor faces by a certain amount.
        /// </summary>
        public static List<List<XYZ>> GetFloorBoundaryPolygons(
            List<Element> floors,
            Options opt)
        {
            var polygons = new List<List<XYZ>>();

            foreach (Floor floor in floors)
            {
                var geo = floor.get_Geometry(opt);

                //GeometryObjectArray objects = geo.Objects; // 2012
                //foreach( GeometryObject obj in objects ) // 2012

                foreach (var obj in geo) // 2013
                {
                    var solid = obj as Solid;
                    if (solid != null) GetBoundary(polygons, solid);
                }
            }

            return polygons;
        }
    }
}