#region Header

//
// CmdSetGridEndpoint.cs - move selected grid endpoints in Y direction using SetCurveInView
//
// Copyright (C) 2018-2020 by Ryuji Ogasawara and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// Written by Ryuji Ogasawara.
//

#endregion // Header

#region Namespaces

using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    public class CmdSetGridEndpoint : IExternalCommand
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
            var view = doc.ActiveView;

            ISelectionFilter f
                = new JtElementsOfClassSelectionFilter<Grid>();

            var elemRef = sel.PickObject(
                ObjectType.Element, f, "Pick a grid");

            var grid = doc.GetElement(elemRef) as Grid;

            var gridCurves = grid.GetCurvesInView(
                DatumExtentType.Model, view);

            using var tx = new Transaction(doc);
            tx.Start("Modify Grid Endpoints");

            foreach (var c in gridCurves)
            {
                var start = c.GetEndPoint(0);
                var end = c.GetEndPoint(1);

                var newStart = start + 10 * XYZ.BasisY;
                var newEnd = end - 10 * XYZ.BasisY;

                var newLine = Line.CreateBound(newStart, newEnd);

                grid.SetCurveInView(
                    DatumExtentType.Model, view, newLine);
            }

            tx.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Align the given grid horizontally or vertically
        ///     if it is very slightly off axis, by Fair59 in
        ///     https://forums.autodesk.com/t5/revit-api-forum/grids-off-axis/m-p/7129065
        /// </summary>
        private void AlignOffAxisGrid(
            Grid grid)
        {
            //Grid grid = doc.GetElement( 
            //  sel.GetElementIds().FirstOrDefault() ) as Grid;

            var doc = grid.Document;

            var direction = grid.Curve
                .GetEndPoint(1)
                .Subtract(grid.Curve.GetEndPoint(0))
                .Normalize();

            var distance2hor = direction.DotProduct(XYZ.BasisY);
            var distance2vert = direction.DotProduct(XYZ.BasisX);
            double angle = 0;

            // Maybe use another criterium then <0.0001

            var max_distance = 0.0001;

            if (Math.Abs(distance2hor) < max_distance)
            {
                var vector = direction.X < 0
                    ? direction.Negate()
                    : direction;

                angle = Math.Asin(-vector.Y);
            }

            if (Math.Abs(distance2vert) < max_distance)
            {
                var vector = direction.Y < 0
                    ? direction.Negate()
                    : direction;

                angle = Math.Asin(vector.X);
            }

            if (angle.CompareTo(0) != 0)
            {
                using var t = new Transaction(doc);
                t.Start("correctGrid");

                ElementTransformUtils.RotateElement(doc,
                    grid.Id,
                    Line.CreateBound(grid.Curve.GetEndPoint(0),
                        grid.Curve.GetEndPoint(0).Add(XYZ.BasisZ)),
                    angle);

                t.Commit();
            }
        }
    }
}