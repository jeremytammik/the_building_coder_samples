#region Header

//
// CmdNewSprinkler.cs - insert a new sprinkler family instance
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewSprinkler : IExternalCommand
    {
        private const string _path = "C:/Documents and Settings/All Users/Application Data/Autodesk/RME 2010/Metric Library/Fire Protection/Sprinklers/";
        private const string _name = "M_Sprinkler - Pendent - Hosted";
        private const string _ext = ".rfa";

        private const string _filename = _path + _name + _ext;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;
            var rc = Result.Failed;

            using var t = new Transaction(doc);
            t.Start("Place a New Sprinkler Instance");


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

            if (Util.GetFirstElementOfTypeNamed(
                doc, typeof(Family), _name) is not Family family)
                if (!doc.LoadFamily(_filename, out family))
                {
                    message = $"Unable to load '{_filename}'.";
                    return rc;
                }

            FamilySymbol sprinklerSymbol = null;

            //foreach( FamilySymbol fs in family.Symbols ) // 2014

            foreach (var id in
                family.GetFamilySymbolIds()) // 2015
            {
                sprinklerSymbol = doc.GetElement(id)
                    as FamilySymbol;

                break;
            }

            Debug.Assert(null != sprinklerSymbol,
                "expected at least one sprinkler symbol"
                + " to be defined in family");

            // pick the host ceiling:

            var ceiling = Util.SelectSingleElement(
                uidoc, "ceiling to host sprinkler");

            if (null == ceiling
                || !ceiling.Category.Id.IntegerValue.Equals(
                    (int) BuiltInCategory.OST_Ceilings))
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

            var ceilingBottom
                = GetLargestHorizontalFace(ceiling);

            if (null != ceilingBottom)
            {
                var p = PointOnFace(ceilingBottom);

                // Create the sprinkler family instance:

                var fi = doc.Create.NewFamilyInstance(
                    ceilingBottom, p, XYZ.BasisX, sprinklerSymbol);

                rc = Result.Succeeded;
            }

            t.Commit();

            return rc;
        }

        /// <summary>
        ///     Return the largest horizontal face of the given
        ///     element e, either top or bottom, optionally
        ///     computing references.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="computReferences">Compute references?</param>
        /// <param name="bottomFace">Top or bottom?</param>
        private PlanarFace GetLargestHorizontalFace(
            Element e,
            bool computReferences = true,
            bool bottomFace = true)
        {
            //Options opt = app.Application.Create.NewGeometryOptions();

            var opt = new Options();
            opt.ComputeReferences = computReferences;

            var geo = e.get_Geometry(opt);

            PlanarFace largest_face = null;

            foreach (var obj in geo)
            {
                var solid = obj as Solid;

                if (null != solid)
                    foreach (Face face in solid.Faces)
                    {
                        var pf = face as PlanarFace;

                        if (null != pf)
                        {
                            var normal = pf.FaceNormal.Normalize();

                            if (Util.IsVertical(normal)
                                && (bottomFace ? 0.0 > normal.Z : 0.0 < normal.Z)
                                && (null == largest_face || largest_face.Area < pf.Area))
                            {
                                largest_face = pf;
                                break;
                            }
                        }
                    }
            }

            return largest_face;
        }

        /// <summary>
        ///     Return the median point of a triangle by
        ///     taking the average of its three vertices.
        /// </summary>
        private XYZ MedianPoint(MeshTriangle triangle)
        {
            var p = XYZ.Zero;
            p += triangle.get_Vertex(0);
            p += triangle.get_Vertex(1);
            p += triangle.get_Vertex(2);
            p *= 0.3333333333333333;
            return p;
        }

        /// <summary>
        ///     Return the area of a triangle as half of
        ///     its height multiplied with its base length.
        /// </summary>
        private double TriangleArea(MeshTriangle triangle)
        {
            var a = triangle.get_Vertex(0);
            var b = triangle.get_Vertex(1);
            var c = triangle.get_Vertex(2);

            var l = Line.CreateBound(a, b);

            var h = l.Project(c).Distance;

            var area = 0.5 * l.Length * h;

            return area;
        }

        /// <summary>
        ///     Return an arbitrary point on a planar face,
        ///     namely the midpoint of the first mesh triangle.
        /// </summary>
        private XYZ PointOnFace(PlanarFace face)
        {
            var mesh = face.Triangulate();

            return 0 < mesh.NumTriangles
                ? MedianPoint(mesh.get_Triangle(0))
                : XYZ.Zero;
        }

        /// <summary>
        ///     Return a 'good' point on a planar face, namely
        ///     the median point of its largest mesh triangle.
        /// </summary>
        private XYZ PointOnFace2(PlanarFace face)
        {
            var mesh = face.Triangulate();
            double max_area = 0;
            var selected = 0;

            for (var i = 0; i < mesh.NumTriangles; i++)
            {
                var area = TriangleArea(
                    mesh.get_Triangle(i));

                if (max_area < area)
                {
                    max_area = area;
                    selected = i;
                }
            }

            return MedianPoint(mesh.get_Triangle(selected));
        }
    }
}