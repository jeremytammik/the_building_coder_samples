#region Header

//
// CmdWallTopFaces.cs - retrieve top faces of selected or all wall
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#define CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
#if CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES
    [Transaction(TransactionMode.Manual)]
#else
  [Transaction( TransactionMode.ReadOnly )]
#endif // CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

    internal class CmdWallTopFaces : IExternalCommand
    {
        /// <summary>
        ///     Super-simple test whether a face is planar
        ///     and its normal vector points upwards.
        /// </summary>
        private static bool IsTopPlanarFace(Face f)
        {
            return f is PlanarFace face
                   && Util.PointsUpwards(face.FaceNormal);
        }

        /// <summary>
        ///     Simple test whether a given face normal vector
        ///     points upwards in the middle of the face.
        /// </summary>
        private static bool IsTopFace(Face f)
        {
            var b = f.GetBoundingBox();
            var p = b.Min;
            var q = b.Max;
            var midpoint = p + 0.5 * (q - p);
            var normal = f.ComputeNormal(midpoint);
            return Util.PointsUpwards(normal);
        }

        /// <summary>
        ///     Define equality between XYZ objects, ensuring
        ///     that almost equal points compare equal. Cf.
        ///     CmdNestedInstanceGeo.XyzEqualityComparer,
        ///     which uses the native Revit API XYZ comparison
        ///     member method IsAlmostEqualTo. We cannot use
        ///     it here, because the tolerance built into that
        ///     method is too fine and does not recognise
        ///     points that we need to identify as equal.
        /// </summary>
        public class XyzEqualityComparer : IEqualityComparer<XYZ>
        {
            private readonly double _eps;

            public XyzEqualityComparer(double eps)
            {
                Debug.Assert(0 < eps,
                    "expected a positive tolerance");

                _eps = eps;
            }

            public bool Equals(XYZ p, XYZ q)
            {
                return _eps > p.DistanceTo(q);
            }

            public int GetHashCode(XYZ p)
            {
                return Util.PointString(p).GetHashCode();
            }
        }

#if CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

        /// <summary>
        ///     Offset at which to create a model curve copy
        ///     of all top face edges for debugging purposes.
        /// </summary>
        private static readonly XYZ _offset = XYZ.BasisZ / 12;

        /// <summary>
        ///     Translation transformation to apply to create
        ///     model curve copies of top face edges.
        /// </summary>
        private static readonly Transform _t =
            // Transform.get_Translation( _offset ); // 2013
            Transform.CreateTranslation(_offset); // 2014

#endif // CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            var opt = app.Create.NewGeometryOptions();

            var comparer
                = new XyzEqualityComparer(1e-6);

#if CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

            var creator = new Creator(doc);

            var t = new Transaction(doc);

            t.Start("Create model curve copies of top face edges");

#endif // CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

            IList<Face> topFaces = new List<Face>();

            int n;
            var nWalls = 0;

            //foreach( Element e in uidoc.Selection.Elements ) // 2014

            foreach (var id in uidoc.Selection.GetElementIds()) // 2015
            {
                var e = doc.GetElement(id);

                var wall = e as Wall;

                if (null == wall)
                {
                    Debug.Print($"Skipped {Util.ElementDescription(e)}");
                    continue;
                }

                // Get the side faces

                var sideFaces
                    = HostObjectUtils.GetSideFaces(wall,
                        ShellLayerType.Exterior);

                // Access the first side face

                var e2 = doc.GetElement(sideFaces[0]);

                Debug.Assert(e2.Id.Equals(e.Id),
                    "expected side face element to be the wall itself");

                var face = e2.GetGeometryObjectFromReference(
                    sideFaces[0]) as Face;

                if (null == face)
                {
                    Debug.Print($"No side face found for {Util.ElementDescription(e)}");
                    continue;
                }

                // When there are opening such as doors or
                // windows in the wall, we need to find the
                // outer loop.
                // For one possible approach to extract the
                // outermost loop, please refer to
                // http://thebuildingcoder.typepad.com/blog/2008/12/2d-polygon-areas-and-outer-loop.html

                // Determine the outer loop of the side face
                // by finding the polygon with the largest area

                XYZ normal;
                double area, dist, maxArea = 0;
                EdgeArray outerLoop = null;

                foreach (EdgeArray ea in face.EdgeLoops)
                    if (CmdWallProfileArea.GetPolygonPlane(
                            ea.GetPolygon(), out normal, out dist, out area)
                        && Math.Abs(area) > Math.Abs(maxArea))
                    {
                        maxArea = area;
                        outerLoop = ea;
                    }

                n = 0;

#if GET_FACES_FROM_OUTER_LOOP
        // With the outermost loop, calculate the top faces

        foreach( Edge edge in outerLoop )
        {
          // For each edge, get the neighbouring
          // face and check its normal

          for( int i = 0; i < 2; ++i )
          {
            PlanarFace pf = edge.get_Face( i )
              as PlanarFace;

            if( null == pf )
            {
              Debug.Print( "Skipped non-planar face on "
                + Util.ElementDescription( e ) );
              continue;
            }

            if( Util.PointsUpwards( pf.Normal, 0.9 ) )
            {
              if( topFaces.Contains( pf ) )
              {
                Debug.Print( "Duplicate face on "
                  + Util.ElementDescription( e ) );
              }
              else
              {
                topFaces.Add( pf );
                ++n;
              }
            }
          }
        }
#endif // GET_FACES_FROM_OUTER_LOOP

                var sideVertices = outerLoop.GetPolygon();

                // Go over all the faces of the wall and
                // determine which ones fulfill the following
                // two criteria: (i) planar face pointing
                // upwards, and (ii) neighbour of the side
                // face outer loop.

                var solid = wall.get_Geometry(opt)
                    .OfType<Solid>()
                    .First(sol => null != sol);

                foreach (Face f in solid.Faces)
                    if (IsTopFace(f))
                    {
                        var faceVertices
                            = f.Triangulate().Vertices;

                        //if( sideVertices.Exists( v
                        //  => faceVertices.Contains<XYZ>( v, comparer ) ) )
                        //{
                        //  topFaces.Add( f );
                        //  ++n;
                        //}

                        foreach (var v in faceVertices)
                            if (sideVertices.Contains(
                                v, comparer))
                            {
                                topFaces.Add(f);
                                ++n;

#if CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

                                // Display face for debugging purposes

                                foreach (EdgeArray ea in f.EdgeLoops)
                                {
                                    var curves
                                        = ea.Cast<Edge>()
                                            .Select(
                                                x => x.AsCurve());

                                    foreach (var curve in curves)
                                        //creator.CreateModelCurve( curve.get_Transformed( _t ) ); // 2013
                                        creator.CreateModelCurve(curve.CreateTransformed(_t)); // 2014
                                }

#endif // CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

                                break;
                            }
                    }

                Debug.Print(string.Format(
                        "{0} top face{1} found on {2} ({3})",
                        n, Util.PluralSuffix(n),
                        Util.ElementDescription(e)),
                    nWalls++);
            }

#if CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES
            t.Commit();
#endif // CREATE_MODEL_CURVES_FOR_TOP_FACE_EDGES

            var s = $"{nWalls} wall{Util.PluralSuffix(nWalls)} successfully processed";

            n = topFaces.Count;

            TaskDialog.Show("Wall Top Faces",
                $"{s} with {n} top face{Util.PluralSuffix(n)}.");

            return Result.Succeeded;
        }
    }
}

// C:\tmp\rac_basic_sample_project_walls.rvt
// C:\Program Files\Autodesk\Revit Architecture 2012\Program\Samples\rac_basic_sample_project.rvt
// C:\a\doc\revit\blog\rvt\two_walls_top_face.rvt