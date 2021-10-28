#region Header

//
// CmdBoundingBox.cs - eplore element bounding box
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

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdBoundingBox : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var e = Util.SelectSingleElement(
                uidoc, "an element");

            if (null == e)
            {
                message = "No element selected";
                return Result.Failed;
            }

            // Trying to call this property returns the
            // compile time error: Property, indexer, or
            // event 'BoundingBox' is not supported by
            // the language; try directly calling
            // accessor method 'get_BoundingBox( View )'

            //BoundingBoxXYZ b = e.BoundingBox[null];

            View v = null;

            var b = e.get_BoundingBox(v);

            if (null == b)
            {
                v = commandData.View;
                b = e.get_BoundingBox(v);
            }

            if (null == b)
            {
                Util.InfoMsg(
                    $"{Util.ElementDescription(e)} has no bounding box.");
            }
            else
            {
                using var tx = new Transaction(doc);
                tx.Start("Draw Model Line Bounding Box Outline");

                Debug.Assert(b.Transform.IsIdentity,
                    "expected identity bounding box transform");

                var in_view = null == v
                    ? "model space"
                    : $"view {v.Name}";

                Util.InfoMsg(string.Format(
                    "Element bounding box of {0} in "
                    + "{1} extends from {2} to {3}.",
                    Util.ElementDescription(e),
                    in_view,
                    Util.PointString(b.Min),
                    Util.PointString(b.Max)));

                var creator = new Creator(doc);

                creator.DrawPolygon(new List<XYZ>(
                    Util.GetBottomCorners(b)));

                var rotation = Transform.CreateRotation(
                    XYZ.BasisZ, 60 * Math.PI / 180.0);

                b = RotateBoundingBox(b, rotation);

                Util.InfoMsg(string.Format(
                    "Bounding box rotated by 60 degrees "
                    + "extends from {0} to {1}.",
                    Util.PointString(b.Min),
                    Util.PointString(b.Max)));

                creator.DrawPolygon(new List<XYZ>(
                    Util.GetBottomCorners(b)));

                tx.Commit();
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return a rotated bounding box around
        ///     the origin in the XY plane.
        ///     We cannot just rotate the min and max points,
        ///     because the rotated max point may easily end
        ///     up being 'smaller' in some coordinate than the
        ///     min. To work around that, we extract all four
        ///     bounding box corners, rotate each of them and
        ///     determine new min and max values from those.
        /// </summary>
        private static BoundingBoxXYZ RotateBoundingBox(
            BoundingBoxXYZ b,
            Transform t)
        {
            var height = b.Max.Z - b.Min.Z;

            // Four corners: lower left, lower right, 
            // upper right, upper left:

            var corners = Util.GetBottomCorners(b);

            var cornersTransformed
                = corners.Select(
                        p => new XyzComparable(t.OfPoint(p)))
                    .ToArray();

            b.Min = cornersTransformed.Min();
            b.Max = cornersTransformed.Max();
            b.Max += height * XYZ.BasisZ;

            return b;
        }

        /// <summary>
        ///     XYZ wrapper class implementing IComparable.
        /// </summary>
        private class XyzComparable : XYZ, IComparable<XYZ>
        {
            public XyzComparable(XYZ a)
                : base(a.X, a.Y, a.Z)
            {
            }

            int IComparable<XYZ>.CompareTo(XYZ a)
            {
                return Util.Compare(this, a);
            }
        }
    }
}