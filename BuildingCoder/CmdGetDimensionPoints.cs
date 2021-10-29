#region Header

//
// CmdGetDimensionPoints.cs - determine dimension segment start and end points
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-a-dimension-s-segment-geometry/m-p/7145688
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    public class CmdGetDimensionPoints : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            var sel = uidoc.Selection;

            ISelectionFilter f
                = new JtElementsOfClassSelectionFilter<Dimension>();

            var elemRef = sel.PickObject(
                ObjectType.Element, f, "Pick a dimension");

            var dim = doc.GetElement(elemRef) as Dimension;

            var p = GetDimensionStartPoint(dim);
            var pts = GetDimensionPoints(dim, p);

            var n = pts.Count;

            Debug.Print("Dimension origin at {0} followed "
                        + "by {1} further point{2}{3} {4}",
                Util.PointString(p), n,
                Util.PluralSuffix(n), Util.DotOrColon(n),
                string.Join(", ", pts.Select(
                    q => Util.PointString(q))));

            var d = new List<double>(n);
            var q0 = p;
            foreach (var q in pts)
            {
                d.Add(q.X - q0.X);
                q0 = q;
            }

            Debug.Print(
                $"Horizontal distances in metres: {string.Join(", ", d.Select(x => Util.RealString(Util.FootToMetre(x))))}");

            using var tx = new Transaction(doc);
            tx.Start("Draw Point Markers");

            var sketchPlane = dim.View.SketchPlane;

            var size = 0.3;
            DrawMarker(p, size, sketchPlane);
            pts.ForEach(q => DrawMarker(q, size, sketchPlane));

            tx.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return dimension origin, i.e., the midpoint
        ///     of the dimension or of its first segment.
        /// </summary>
        private XYZ GetDimensionStartPoint(
            Dimension dim)
        {
            XYZ p = null;

            try
            {
                p = dim.Origin;
            }
            catch (ApplicationException ex)
            {
                Debug.Assert(ex.Message.Equals("Cannot access this method if this dimension has more than one segment."));

                foreach (DimensionSegment seg in dim.Segments)
                {
                    p = seg.Origin;
                    break;
                }
            }

            return p;
        }

        /// <summary>
        ///     Retrieve the start and end points of
        ///     each dimension segment, based on the
        ///     dimension origin determined above.
        /// </summary>
        private List<XYZ> GetDimensionPoints(
            Dimension dim,
            XYZ pStart)
        {
            var dimLine = dim.Curve as Line;
            if (dimLine == null) return null;
            var pts = new List<XYZ>();

            dimLine.MakeBound(0, 1);
            var pt1 = dimLine.GetEndPoint(0);
            var pt2 = dimLine.GetEndPoint(1);
            var direction = pt2.Subtract(pt1).Normalize();

            if (0 == dim.Segments.Size)
            {
                var v = 0.5 * (double) dim.Value * direction;
                pts.Add(pStart - v);
                pts.Add(pStart + v);
            }
            else
            {
                var p = pStart;
                foreach (DimensionSegment seg in dim.Segments)
                {
                    var v = (double) seg.Value * direction;
                    if (0 == pts.Count) pts.Add(p = pStart - 0.5 * v);
                    pts.Add(p = p.Add(v));
                }
            }

            return pts;
        }

        /// <summary>
        ///     Graphical debugging helper using model lines
        ///     to draw an X at the given position.
        /// </summary>
        private void DrawMarker(
            XYZ p,
            double size,
            SketchPlane sketchPlane)
        {
            size *= 0.5;
            var v = new XYZ(size, size, 0);
            var doc = sketchPlane.Document;
            doc.Create.NewModelCurve(Line.CreateBound(
                p - v, p + v), sketchPlane);
            v = new XYZ(size, -size, 0);
            doc.Create.NewModelCurve(Line.CreateBound(
                p - v, p + v), sketchPlane);
        }

        /// <summary>
        ///     Return a reference built directly from grid
        /// </summary>
        private Reference GetGridRef(Document doc)
        {
            var idGrid = new ElementId(397028);
            var eGrid = doc.GetElement(idGrid);
            return new Reference(eGrid);
        }

        #region Obsolete initial attempts

        private List<XYZ> GetDimensionPointsObsoleteFirstAttempt(
            Dimension dim)
        {
            var dimLine = dim.Curve as Line;
            if (dimLine == null) return null;
            var pts = new List<XYZ>();

            dimLine.MakeBound(0, 1);
            var pt1 = dimLine.GetEndPoint(0);
            var pt2 = dimLine.GetEndPoint(1);
            var direction = pt2.Subtract(pt1).Normalize();
            pts.Add(pt1);
            if (dim.Segments.Size == 0)
            {
                pt2 = pt1.Add(direction.Multiply((double) dim.Value));
                pts.Add(pt2);
            }
            else
            {
                var segmentPt0 = pt1;
                foreach (DimensionSegment seg in dim.Segments)
                {
                    var segmentPt1 = segmentPt0.Add(direction.Multiply((double) seg.Value));
                    Debug.Print("pt  {0},  value  {1}", segmentPt1, (double) seg.Value);
                    pts.Add(segmentPt1);
                    segmentPt0 = segmentPt1;
                }
            }

            return pts;
        }

        private XYZ GetDimensionStartPointFirstAttempt(
            Dimension dim)
        {
            var doc = dim.Document;

            var dimLine = dim.Curve as Line;
            if (dimLine == null) return null;
            dimLine.MakeBound(0, 1);

            XYZ dimStartPoint = null;
            var pt1 = dimLine.GetEndPoint(0);

            // dim.Origin throws "Cannot access this method
            // if this dimension has more than one segment."
            //Debug.Assert( Util.IsEqual( pt1, dim.Origin ),
            //  "expected equal points" );

            foreach (Reference ref1 in dim.References)
            {
                XYZ refPoint = null;
                var el = doc.GetElement(ref1.ElementId);
                var obj = el.GetGeometryObjectFromReference(
                    ref1);

                if (obj == null)
                {
                    switch (el)
                    {
                        // element is Grid or ReferencePlane or ??
                        case ReferencePlane refPl:
                            refPoint = refPl.GetPlane().Origin;
                            break;
                        case Grid grid:
                            refPoint = grid.Curve.GetEndPoint(0);
                            break;
                    }
                }
                else
                {
                    // reference to Line, Plane or Point?
                    var l = obj as Line;
                    if (l != null) refPoint = l.GetEndPoint(0);
                    var f = obj as PlanarFace;
                    if (f != null) refPoint = f.Origin;
                }

                if (refPoint != null)
                {
                    //View v = doc.ActiveView;
                    var v = dim.View;
                    var WorkPlane = v.SketchPlane.GetPlane();
                    var normal = WorkPlane.Normal.Normalize();

                    // Project the "globalpoint" of the reference onto the sketchplane

                    var refPtonPlane = refPoint.Subtract(
                        normal.Multiply(normal.DotProduct(
                            refPoint - WorkPlane.Origin)));

                    var lineNormal = normal.CrossProduct(
                        dimLine.Direction).Normalize();

                    // Project the result onto the dimensionLine

                    dimStartPoint = refPtonPlane.Subtract(
                        lineNormal.Multiply(lineNormal.DotProduct(
                            refPtonPlane - pt1)));
                }

                break;
            }

            return dimStartPoint;
        }

        #endregion // Obsolete initial attempts
    }
}