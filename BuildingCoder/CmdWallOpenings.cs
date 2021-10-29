#region Header

//
// CmdWallOpenings.cs - determine wall opening side faces and report their start and end points along location line
//
// Copyright (C) 2015-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    /// <summary>
    ///     A simple class with two coordinates
    ///     and some other basic info.
    /// </summary>
    internal class WallOpening2d
    {
        //public ElementId Id { get; set; }
        public XYZ Start { get; set; }
        public XYZ End { get; set; }

        public override string ToString()
        {
            return $"({Util.PointString(Start)}-{Util.PointString(End)})";
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class CmdWallOpenings : IExternalCommand
    {
        /// <summary>
        ///     Move out of wall and up from floor a bit
        /// </summary>
        private const double _offset = 0.1; // feet

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            if (null == doc)
            {
                message = "Please run this command in a valid document.";
                return Result.Failed;
            }

            if (doc.ActiveView is not View3D view)
            {
                message = "Please run this command in a 3D view.";
                return Result.Failed;
            }

            var e = Util.SelectSingleElementOfType(
                uidoc, typeof(Wall), "wall", true);

            var openings = GetWallOpenings(
                e as Wall, view);

            var n = openings.Count;

            var msg = $"{n} opening{Util.PluralSuffix(n)} found{Util.DotOrColon(n)}";

            Util.InfoMsg2(msg, string.Join(
                "\r\n", openings));

            return Result.Succeeded;
        }

        /// <summary>
        /// A small number
        /// </summary>
        //const double _eps = .1e-5;

        /// <summary>
        ///     Predicate: is the given number even?
        /// </summary>
        private static bool IsEven(int i)
        {
            return 0 == i % 2;
        }

        /// <summary>
        ///     Predicate: does the given reference refer to a surface?
        /// </summary>
        private static bool IsSurface(Reference r)
        {
            return ElementReferenceType.REFERENCE_TYPE_SURFACE
                   == r.ElementReferenceType;
        }

        /// <summary>
        ///     Retrieve all wall openings,
        ///     including at start and end of wall.
        /// </summary>
        private List<WallOpening2d> GetWallOpenings(
            Wall wall,
            View3D view)
        {
            var doc = wall.Document;
            var level = doc.GetElement(wall.LevelId) as Level;
            var elevation = level.Elevation;
            var c = (wall.Location as LocationCurve).Curve;
            var wallOrigin = c.GetEndPoint(0);
            var wallEndPoint = c.GetEndPoint(1);
            var wallDirection = wallEndPoint - wallOrigin;
            var wallLength = wallDirection.GetLength();
            wallDirection = wallDirection.Normalize();
            var offsetOut = _offset * new UV(wallDirection.X, wallDirection.Y);

            var rayStart = new XYZ(wallOrigin.X - offsetOut.U,
                wallOrigin.Y - offsetOut.V, elevation + _offset);

            var intersector
                = new ReferenceIntersector(wall.Id,
                    FindReferenceTarget.Face, view);

            var refs
                = intersector.Find(rayStart, wallDirection);

            // Extract the intersection points:
            // - only surfaces
            // - within wall length plus offset at each end
            // - sorted by proximity
            // - eliminating duplicates

            var pointList = new List<XYZ>(refs
                .Where(r => IsSurface(
                    r.GetReference()))
                .Where(r => r.Proximity
                            < wallLength + _offset + _offset)
                .OrderBy(
                    r => r.Proximity)
                .Select(r
                    => r.GetReference().GlobalPoint)
                .Distinct(new XyzEqualityComparer()));

            // Check if first point is at the wall start.
            // If so, the wall does not begin with an opening,
            // so that point can be removed. Else, add it.

            var q = wallOrigin + _offset * XYZ.BasisZ;

            var wallHasFaceAtStart = Util.IsEqual(
                pointList[0], q);

            if (wallHasFaceAtStart)
                pointList.RemoveAll(p
                    //=> _eps > p.DistanceTo( q ) );
                    => Util.IsEqual(p, q));
            else
                pointList.Insert(0, wallOrigin);

            // Check if last point is at the wall end.
            // If so, the wall does not end with an opening, 
            // so that point can be removed. Else, add it.

            q = wallEndPoint + _offset * XYZ.BasisZ;

            var wallHasFaceAtEnd = Util.IsEqual(
                pointList.Last(), q);

            if (wallHasFaceAtEnd)
                pointList.RemoveAll(p
                    //=> _eps > p.DistanceTo( q ) );
                    => Util.IsEqual(p, q));
            else
                pointList.Add(wallEndPoint);

            var n = pointList.Count;

            Debug.Assert(IsEven(n),
                "expected an even number of opening sides");

            var wallOpenings = new List<WallOpening2d>(
                n / 2);

            for (var i = 0; i < n; i += 2)
                wallOpenings.Add(new WallOpening2d
                {
                    Start = pointList[i],
                    End = pointList[i + 1]
                });
            return wallOpenings;
        }

        private class XyzProximityComparerNotUsed : IComparer<XYZ>
        {
            private readonly XYZ _p;

            public XyzProximityComparerNotUsed(XYZ p)
            {
                _p = p;
            }

            public int Compare(XYZ x, XYZ y)
            {
                var dx = x.DistanceTo(_p);
                var dy = y.DistanceTo(_p);
                return Util.IsEqual(dx, dy) ? 0
                    : dx < dy ? -1 : 1;
            }
        }

        private class XyzEqualityComparer : IEqualityComparer<XYZ>
        {
            public bool Equals(XYZ a, XYZ b)
            {
                return Util.IsEqual(a, b);
            }

            public int GetHashCode(XYZ a)
            {
                return Util.PointString(a).GetHashCode();
            }
        }

        #region Determine walls in linked file intersecting pipe

        /// <summary>
        ///     Determine walls in linked file intersecting pipe
        /// </summary>
        public void GetWalls(UIDocument uidoc)
        {
            var doc = uidoc.Document;

            var pipeRef = uidoc.Selection.PickObject(
                ObjectType.Element);

            var pipeElem = doc.GetElement(pipeRef);

            var lc = pipeElem.Location as LocationCurve;
            var curve = lc.Curve;

            var reference1 = new ReferenceComparer();

            ElementFilter filter = new ElementCategoryFilter(
                BuiltInCategory.OST_Walls);

            var collector
                = new FilteredElementCollector(doc);

            Func<View3D, bool> isNotTemplate = v3 => !v3.IsTemplate;
            var view3D = collector
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .First(isNotTemplate);

            var refIntersector
                = new ReferenceIntersector(
                    filter, FindReferenceTarget.Element, view3D);

            refIntersector.FindReferencesInRevitLinks = true;
            var referenceWithContext
                = refIntersector.Find(
                    curve.GetEndPoint(0),
                    (curve as Line).Direction);

            IList<Reference> references
                = referenceWithContext
                    .Select(p => p.GetReference())
                    .Distinct(reference1)
                    .Where(p => p.GlobalPoint.DistanceTo(
                        curve.GetEndPoint(0)) < curve.Length)
                    .ToList();

            IList<Element> walls = new List<Element>();
            foreach (var reference in references)
            {
                var instance = doc.GetElement(reference)
                    as RevitLinkInstance;
                var linkDoc = instance.GetLinkDocument();
                var element = linkDoc.GetElement(reference.LinkedElementId);
                walls.Add(element);
            }

            TaskDialog.Show("Count of wall", walls.Count.ToString());
        }

        /// <summary>
        ///     Compare references with linked file support.
        /// </summary>
        public class ReferenceComparer : IEqualityComparer<Reference>
        {
            public bool Equals(Reference x, Reference y)
            {
                if (x.ElementId == y.ElementId)
                {
                    if (x.LinkedElementId == y.LinkedElementId) return true;
                    return false;
                }

                return false;
            }

            public int GetHashCode(Reference obj)
            {
                var hashName = obj.ElementId.GetHashCode();
                var hashId = obj.LinkedElementId.GetHashCode();
                return hashId ^ hashId;
            }
        }

        /// <summary>
        ///     Return a `StableRepresentation` for a linked wall's exterior face.
        /// </summary>
        public string GetFaceRefRepresentation(
            Wall wall,
            Document doc,
            RevitLinkInstance instance)
        {
            var faceRef = HostObjectUtils.GetSideFaces(
                wall, ShellLayerType.Exterior).FirstOrDefault();
            var stRef = faceRef.CreateLinkReference(instance);
            var stable = stRef.ConvertToStableRepresentation(doc);
            return stable;
        }

        #endregion // Determine walls in linked file intersecting pipe

        #region Find Beams and Slabs intersecting Columns

        // https://forums.autodesk.com/t5/revit-api-forum/ray-projection-not-picking-up-beams/m-p/10388868
        private void AdjustColumnHeightsUsingBoundingBox(
            Document doc,
            IList<ElementId> ids)
        {
            var view = doc.ActiveView;

            var allColumns = 0;
            var successColumns = 0;

            if (view is View3D)
            {
                using (var tx = new Transaction(doc))
                {
                    tx.Start("Adjust Column Heights");

                    foreach (var elemId in ids)
                    {
                        var elem = doc.GetElement(elemId);

                        // Check if element is column

                        if ((BuiltInCategory) elem.Category.Id.IntegerValue
                            == BuiltInCategory.OST_StructuralColumns)
                        {
                            allColumns++;

                            var column = elem as FamilyInstance;

                            // Collect beams and slabs within bounding box

                            var builtInCats = new List<BuiltInCategory>();
                            builtInCats.Add(BuiltInCategory.OST_Floors);
                            builtInCats.Add(BuiltInCategory.OST_StructuralFraming);
                            var beamSlabFilter
                                = new ElementMulticategoryFilter(builtInCats);

                            var bb = elem.get_BoundingBox(view);
                            var myOutLn = new Outline(bb.Min, bb.Max + 100 * XYZ.BasisZ);
                            var bbFilter
                                = new BoundingBoxIntersectsFilter(myOutLn);

                            var collector
                                = new FilteredElementCollector(doc)
                                    .WherePasses(beamSlabFilter)
                                    .WherePasses(bbFilter);

                            var intersectingBeams = new List<Element>();
                            var intersectingSlabs = new List<Element>();

                            if (ColumnAttachment.GetColumnAttachment(
                                column, 1) != null)
                            {
                                // Change color of columns to green

                                var color = new Color(0, 255, 0);
                                var ogs = new OverrideGraphicSettings();
                                ogs.SetProjectionLineColor(color);
                                view.SetElementOverrides(elem.Id, ogs);
                            }
                            else
                            {
                                foreach (var e in collector)
                                    switch (e.Category.Name)
                                    {
                                        case "Structural Framing":
                                            intersectingBeams.Add(e);
                                            break;
                                        case "Floors":
                                            intersectingSlabs.Add(e);
                                            break;
                                    }

                                if (intersectingBeams.Any())
                                {
                                    var lowestBottomElem = intersectingBeams.First();
                                    foreach (var beam in intersectingBeams)
                                    {
                                        var thisBeamBB = beam.get_BoundingBox(view);
                                        var currentLowestBB = lowestBottomElem.get_BoundingBox(view);
                                        if (thisBeamBB.Min.Z < currentLowestBB.Min.Z) lowestBottomElem = beam;
                                    }

                                    ColumnAttachment.AddColumnAttachment(
                                        doc, column, lowestBottomElem, 1,
                                        ColumnAttachmentCutStyle.None,
                                        ColumnAttachmentJustification.Minimum,
                                        0);
                                    successColumns++;
                                }
                                else if (intersectingSlabs.Any())
                                {
                                    var lowestBottomElem = intersectingSlabs.First();
                                    foreach (var slab in intersectingSlabs)
                                    {
                                        var thisSlabBB = slab.get_BoundingBox(view);
                                        var currentLowestBB = lowestBottomElem.get_BoundingBox(view);
                                        if (thisSlabBB.Min.Z < currentLowestBB.Min.Z) lowestBottomElem = slab;
                                    }

                                    ColumnAttachment.AddColumnAttachment(
                                        doc, column, lowestBottomElem, 1,
                                        ColumnAttachmentCutStyle.None,
                                        ColumnAttachmentJustification.Minimum,
                                        0);
                                    successColumns++;
                                }
                                else
                                {
                                    // Change color of columns to red

                                    var color = new Color(255, 0, 0);
                                    var ogs = new OverrideGraphicSettings();
                                    ogs.SetProjectionLineColor(color);
                                    view.SetElementOverrides(elem.Id, ogs);
                                }
                            }
                        }
                    }

                    tx.Commit();
                }

                TaskDialog.Show("Columns Changed",
                    $"{successColumns} of {allColumns} Columns Changed");
            }
            else
            {
                TaskDialog.Show("Revit", "Run Script in 3D View.");
            }
        }

        private void AdjustColumnHeightsUsingReferenceIntersector(
            Document doc,
            IList<ElementId> ids)
        {
            if (doc.ActiveView is not View3D view)
                throw new Exception(
                    "Please run this command in a 3D view.");

            var allColumns = 0;
            var successColumns = 0;

            using var tx = new Transaction(doc);
            tx.Start("Attach Columns Tops");

            foreach (var elemId in ids)
            {
                var elem = doc.GetElement(elemId);

                if ((BuiltInCategory) elem.Category.Id.IntegerValue
                    == BuiltInCategory.OST_StructuralColumns)
                {
                    allColumns++;

                    var column = elem as FamilyInstance;

                    // Collect beams and slabs

                    var builtInCats = new List<BuiltInCategory>();
                    builtInCats.Add(BuiltInCategory.OST_Floors);
                    builtInCats.Add(BuiltInCategory.OST_StructuralFraming);
                    var filter
                        = new ElementMulticategoryFilter(builtInCats);

                    // Remove old column attachement

                    if (ColumnAttachment.GetColumnAttachment(column, 1) != null) ColumnAttachment.RemoveColumnAttachment(column, 1);

                    var elemBB = elem.get_BoundingBox(view);

                    var elemLoc = (elem.Location as LocationPoint).Point;
                    var elemCenter = new XYZ(elemLoc.X, elemLoc.Y, elemLoc.Z + 0.1);
                    var b1 = new XYZ(elemBB.Min.X, elemBB.Min.Y, elemBB.Min.Z + 0.1);
                    var b2 = new XYZ(elemBB.Max.X, elemBB.Max.Y, elemBB.Min.Z + 0.1);
                    var b3 = new XYZ(elemBB.Min.X, elemBB.Max.Y, elemBB.Min.Z + 0.1);
                    var b4 = new XYZ(elemBB.Max.X, elemBB.Min.Y, elemBB.Min.Z + 0.1);

                    var points = new List<XYZ>(5);
                    points.Add(b1);
                    points.Add(b2);
                    points.Add(b3);
                    points.Add(b4);
                    points.Add(elemCenter);

                    var refI = new ReferenceIntersector(
                        filter, FindReferenceTarget.All, view);

                    var rayd = XYZ.BasisZ;
                    ReferenceWithContext refC = null;
                    foreach (var pt in points)
                    {
                        refC = refI.FindNearest(pt, rayd);
                        if (refC != null) break;
                    }

                    if (refC != null)
                    {
                        var reference = refC.GetReference();
                        var id = reference.ElementId;
                        var e = doc.GetElement(id);

                        ColumnAttachment.AddColumnAttachment(
                            doc, column, e, 1,
                            ColumnAttachmentCutStyle.None,
                            ColumnAttachmentJustification.Minimum,
                            0);

                        successColumns++;
                    }
                    else
                    {
                        // Change color of columns to red

                        var color = new Color(255, 0, 0);
                        var ogs = new OverrideGraphicSettings();
                        ogs.SetProjectionLineColor(color);
                        view.SetElementOverrides(elem.Id, ogs);
                    }
                }
            }

            tx.Commit();
        }

        #endregion // Find Beams and Slabs intersecting Columns
    }
}