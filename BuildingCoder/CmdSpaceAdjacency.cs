#region Header

//
// CmdSpaceAdjacency.cs - determine space adjacencies.
//
// Copyright (C) 2009-2020 by Martin Schmid and Jeremy Tammik,
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
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

// todo: report and resolve this, this should not be required: 'RE: ambiguous BoundarySegmentArrayArray'
//using BoundarySegmentArrayArray = Autodesk.Revit.DB.Mechanical.BoundarySegmentArrayArray; // 2011
//using BoundarySegmentArray = Autodesk.Revit.DB.Mechanical.BoundarySegmentArray; // 2011
//using BoundarySegment = Autodesk.Revit.DB.Mechanical.BoundarySegment; // 2011

// 2012

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdSpaceAdjacency : IExternalCommand
    {
        private const double D2mm = 2.0 / 25.4 / 12; // 2 mm in ft units
        private const double MaxWallThickness = 14 / 12;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var spaces = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                spaces, uidoc, typeof(Space)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some space elements."
                    : "No space elements found.";
                return Result.Failed;
            }

            var segments = new List<Segment>();

            foreach (Space space in spaces) GetBoundaries(segments, space);

            var segmentPairs
                = new Dictionary<Segment, Segment>();

            FindClosestSegments(segmentPairs, segments);

            var spaceAdjacencies
                = new Dictionary<Space, List<Space>>();

            DetermineAdjacencies(
                spaceAdjacencies, segmentPairs);

            ReportAdjacencies(spaceAdjacencies);

            return Result.Failed;
        }

        private void GetBoundaries(
            List<Segment> segments,
            Space space)
        {
            //BoundarySegmentArrayArray boundaries = space.Boundary; // 2011

            var boundaries // 2012
                = space.GetBoundarySegments( // 2012
                    new SpatialElementBoundaryOptions()); // 2012

            //foreach( BoundarySegmentArray b in boundaries ) // 2011
            foreach (var b in boundaries) // 2012
            foreach (var s in b)
            {
                //Curve curve = s.Curve; // 2015
                var curve = s.GetCurve(); // 2016
                var a = curve.Tessellate();
                for (var i = 1; i < a.Count; ++i)
                {
                    var segment = new Segment(
                        a[i - 1], a[i], space);

                    segments.Add(segment);
                }
            }
        }

        private void FindClosestSegments(
            Dictionary<Segment, Segment> segmentPairs,
            List<Segment> segments)
        {
            foreach (var segOuter in segments)
            {
                var first = true;
                double dist = 0;
                Segment closest = null;

                foreach (var segInner in segments)
                {
                    if (segOuter == segInner)
                        continue;

                    if (segInner.Space == segOuter.Space)
                        continue;

                    var d = segOuter.Distance(
                        segInner);

                    if (first || d < dist)
                    {
                        dist = d;
                        first = false;
                        closest = segInner;
                    }
                }

                segmentPairs.Add(segOuter, closest);
            }
        }

        private void DetermineAdjacencies(
            Dictionary<Space, List<Space>> a,
            Dictionary<Segment, Segment> segmentPairs)
        {
            foreach (var s in segmentPairs.Keys)
            {
                // Analyse the relationship between the two
                // closest segments s and t. If their distance
                // exceeds the maximum wall thickness, the
                // spaces are not considered adjacent.
                // Otherwise, calculate a test point 2 mm
                // away from s in the direction of t and
                // use the Space.IsPointInSpace method:

                var t = segmentPairs[s];
                var d = s.Distance(t);
                if (d < MaxWallThickness)
                {
                    var direction = s.DirectionTo(t);
                    var startPt = t.MidPoint;
                    var testPoint = startPt + direction * D2mm;
                    if (t.Space.IsPointInSpace(testPoint))
                    {
                        if (!a.ContainsKey(s.Space)) a.Add(s.Space, new List<Space>());
                        if (!a[s.Space].Contains(t.Space)) a[s.Space].Add(t.Space);
                    }
                }
            }
        }

        private void PrintSpaceInfo(
            string indent,
            Space space)
        {
            Debug.Print("{0}{1} {2}", indent,
                space.Name, space.Number);
        }

        private void ReportAdjacencies(
            Dictionary<Space, List<Space>> spaceAdjacencies)
        {
            Debug.WriteLine("\nReport Space Adjacencies:");
            foreach (var space in spaceAdjacencies.Keys)
            {
                PrintSpaceInfo("", space);
                foreach (var adj in spaceAdjacencies[space]) PrintSpaceInfo("  ", adj);
            }
        }

        #region Segment Class

        private class Segment
        {
            public Segment(XYZ sp, XYZ ep, Space space)
            {
                StartPoint = sp;
                EndPoint = ep;
                Space = space;
            }

            public XYZ StartPoint { get; }

            public XYZ EndPoint { get; }

            public Space Space { get; }

            public double Slope
            {
                get
                {
                    var deltaX = StartPoint.X - EndPoint.X;
                    var deltaY = StartPoint.Y - EndPoint.Y;
                    if (deltaX != 0) return deltaY / deltaX;
                    return 0;
                }
            }

            public bool IsHorizontal => StartPoint.Y == EndPoint.Y;

            public bool IsVertical => StartPoint.X == EndPoint.X;

            public XYZ MidPoint => Util.Midpoint(StartPoint, EndPoint);

            public new string ToString()
            {
                return $"{Util.PointString(StartPoint)} {Util.PointString(EndPoint)}";
            }

            public XYZ DirectionTo(Segment a)
            {
                var v = a.MidPoint - MidPoint;
                return v.IsZeroLength() ? v : v.Normalize();
            }

            public double Distance(Segment a)
            {
                return MidPoint.DistanceTo(a.MidPoint);
            }

            public bool Parallel(Segment a)
            {
                return IsVertical && a.IsVertical
                       || IsHorizontal && a.IsHorizontal
                       || Util.IsEqual(Slope, a.Slope);
            }
        }

        #endregion // Segment Class
    }
}