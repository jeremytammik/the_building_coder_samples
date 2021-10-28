#region Header

//
// CmdSortCurveLoops.cs - Retrieve and sort outer and inner face curve loops
//
// Get the CurveLoops of the selected face and sort each 
// outer loop together with its inner loops.
// The SortCurveLoops function defined here is similar to
// ExporterIFCUtils.SortCurveLoops with two exceptions:
// - it takes a Face in input (instead of a List<CurveLoop>)
// - it manages loops on curved faces (instead of co-planar
//   loops only)
//
// This command asks the user to pick a face, gets its edge loops, 
// sorts all the inner loops together with their outer loop, 
// then creates a text note label on the first edge of each loop.
//
// Copyright (C) 2021 by stenci and Jeremy Tammik,
// https://github.com/stenci and Autodesk Inc. 
// All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections;
using System.Collections.Generic;
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
    public class CmdSortCurveLoops : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            // Ask the user to select a Face
            Face face;
            try
            {
                var pickedObject = uidoc.Selection.PickObject(ObjectType.Face, "Select a face");
                var element = doc.GetElement(pickedObject);
                face = element.GetGeometryObjectFromReference(pickedObject) as Face;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }

            using var tx = new Transaction(doc);
            tx.Start("Sort and Mark Face Curve Loops");

            // Sort the loops on the selected face
            var lists = SortCurveLoops(face);

            // Create a label on each loop.
            // Outer loops are counterclockwise.
            // The label is at U=0.33, closer to the beginning of the
            // edge, which gives an idea of the loop orientation. 
            for (var i = 0; i < lists.Count; i++)
            for (var j = 0; j < lists[i].Count; j++)
            {
                var loop = lists[i][j];
                Creator.CreateTextNote($"[{i}][{j}]", loop.First().Evaluate(0.33, true), doc);
            }

            tx.Commit();

            return Result.Succeeded;
        }

        private static List<List<CurveLoop>> SortCurveLoops(Face face)
        {
            var allLoops = face.GetEdgesAsCurveLoops().Select(loop => new CurveLoopUV(loop, face)).ToList();

            var outerLoops = allLoops.Where(loop => loop.IsCounterclockwise).ToList();
            var innerLoops = allLoops.Where(loop => !outerLoops.Contains(loop)).ToList();

            // sort outerLoops putting last the ones that are outside all the preceding loops
            bool somethingHasChanged;
            do
            {
                somethingHasChanged = false;
                for (var i = 1; i < outerLoops.Count(); i++)
                {
                    var point = outerLoops[i].StartPointUV;
                    var loop = outerLoops[i - 1];
                    if (loop.IsPointInside(point) is CurveLoopUV.PointLocation.Inside)
                    {
                        var tmp = outerLoops[i];
                        outerLoops[i] = outerLoops[i - 1];
                        outerLoops[i - 1] = tmp;

                        somethingHasChanged = true;
                    }
                }
            } while (somethingHasChanged);

            var result = new List<List<CurveLoop>>();
            foreach (var outerLoop in outerLoops)
            {
                var list = new List<CurveLoop> {outerLoop.Loop3d};

                for (var i = innerLoops.Count - 1; i >= 0; i--)
                {
                    var innerLoop = innerLoops[i];
                    if (outerLoops.Count == 1 // skip testing when the inner loop is inside the outer loop
                        || outerLoop.IsPointInside(innerLoop.StartPointUV) == CurveLoopUV.PointLocation.Inside)
                    {
                        list.Add(innerLoop.Loop3d);
                        innerLoops.RemoveAt(i);
                    }
                }

                result.Add(list);
            }

            return result;
        }
    }

    internal class CurveLoopUV : IEnumerable<Curve>
    {
        public enum PointLocation
        {
            Outside,
            OnTheEdge,
            Inside
        }

        private const double Epsilon = 0.000001;
        private readonly CurveLoop _loop2d;

        public readonly double MinX, MaxX, MinY, MaxY;

        public CurveLoopUV(CurveLoop curveLoop, Face face)
        {
            Loop3d = curveLoop;
            _loop2d = new CurveLoop();

            var points3d = Loop3d.SelectMany(curve => curve.Tessellate().Skip(1));
            var pointsUv = points3d.Select(point3d => face.Project(point3d).UVPoint);
            var points2d = pointsUv.Select(pointUv => new XYZ(pointUv.U, pointUv.V, 0)).ToList();

            MinX = MinY = 1.0e100;
            MaxX = MaxY = -1.0e100;
            var nPoints = points2d.Count;
            for (var i = 0; i < nPoints; i++)
            {
                var p1 = points2d[i];
                var p2 = points2d[(i + 1) % nPoints];
                _loop2d.Append(Line.CreateBound(p1, p2));
                if (p1.X < MinX)
                    MinX = p1.X;
                if (p1.Y < MinY)
                    MinY = p1.Y;
                if (p1.X > MaxX)
                    MaxX = p1.X;
                if (p1.Y > MaxY)
                    MaxY = p1.Y;
            }
        }

        public CurveLoop Loop3d { get; }

        public bool IsCounterclockwise => _loop2d.IsCounterclockwise(XYZ.BasisZ);

        public XYZ StartPointUV => _loop2d.First().GetEndPoint(0);

        public IEnumerator<Curve> GetEnumerator()
        {
            return _loop2d.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PointLocation IsPointInside(XYZ point)
        {
            // Check if the point is outside of the loop bounding box
            if (point.X - Epsilon < MinX
                || point.X + Epsilon > MaxX
                || point.Y - Epsilon < MinY
                || point.Y + Epsilon > MaxY)
                return PointLocation.Outside;

            // Check if the point is on the loop
            if (_loop2d.Any(curve => curve.Distance(point) < Epsilon))
                return PointLocation.OnTheEdge;

            // Create a Line that starts from point and ends outside of the loop. Adding non-integer
            // values decreases the chances of special cases, where line passes through loop
            // endpoints. These cases can still happen and are managed by the function, but using a
            // different offset costs nothing and may help staying out of trouble. (The trouble
            // could show up when a point doesn't really lay on a line, or two points are not exactly
            // the identical. Using Epsilon helps a little, but, again, when the distance between two
            // points is exactly Epsilon, here comes the trouble.) 
            var line = Line.CreateBound(point, new XYZ(MaxX + 0.1234, MaxY + 0.3456, 0));

            // Count the number of intersections between the line just created and the loop.
            // If the number of intersection is odd, then point is inside the loop.
            // Discard the solutions where the intersection is the edge start point, because these
            // intersections have already been counted when intersecting the end point of the
            // previous segments.
            var nIntersections = _loop2d
                .Where(edge => edge.Intersect(line) == SetComparisonResult.Overlap)
                .Count(edge => line.Distance(edge.GetEndPoint(0)) > Epsilon);

            return nIntersections % 2 == 1 ? PointLocation.Inside : PointLocation.Outside;
        }
    }
}