#region Header

//
// CmdMidCurve.cs - create a series of model line segments between two curve elements
//
// Copyright (C) 2013-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
    internal class CmdMidCurve : IExternalCommand
    {
        /// <summary>
        ///     Number of approximation segments to generate.
        /// </summary>
        private const int _nSegments = 64;

        private const string _prompt
            = "Please run this in a model containing "
              + "exactly two curve elements, and they will be "
              + "automatically selected. Alternatively, pre-"
              + "select two curve elements before launching "
              + "this command, or post-select them when "
              + "prompted.";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // Select all model curves in the entire model.

            var curves = new List<CurveElement>(
                new FilteredElementCollector(doc)
                    .OfClass(typeof(CurveElement))
                    .ToElements()
                    .Cast<CurveElement>());

            var n = curves.Count;

            // If there are less than two, 
            // there is nothing we can do.

            if (2 > n)
            {
                message = _prompt;
                return Result.Failed;
            }

            // If there are exactly two, pick those.

            if (2 < n)
            {
                // Else, check for a pre-selection.

                curves.Clear();

                var sel = uidoc.Selection;
                var ids = sel.GetElementIds();
                n = ids.Count;


                Debug.Print("{0} pre-selected elements.",
                    n);

                // If two or more model curves were pre-
                // selected, use the first two encountered.

                if (1 < n)
                    foreach (var id in ids)
                        if (doc.GetElement(id) is CurveElement c)
                        {
                            curves.Add(c);

                            if (2 == curves.Count)
                            {
                                Debug.Print("Found two model curves, "
                                            + "ignoring everything else.");

                                break;
                            }
                        }

                // Else, prompt for an 
                // interactive post-selection.

                if (2 != curves.Count)
                {
                    curves.Clear();

                    ISelectionFilter f
                        = new JtElementsOfClassSelectionFilter<CurveElement>();

                    try
                    {
                        var r = sel.PickObject(
                            ObjectType.Element, f,
                            "Please pick first model curve.");

                        curves.Add(doc.GetElement(r.ElementId)
                            as CurveElement);
                    }
                    catch (OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }

                    try
                    {
                        var r = sel.PickObject(
                            ObjectType.Element, f,
                            "Please pick second model curve.");

                        curves.Add(doc.GetElement(r.ElementId)
                            as CurveElement);
                    }
                    catch (OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }
            }

            // Extract data from the two selected curves.

            var c0 = curves[0].GeometryCurve;
            var c1 = curves[1].GeometryCurve;

            var sp0 = c0.GetEndParameter(0);
            var ep0 = c0.GetEndParameter(1);
            var step0 = (ep0 - sp0) / _nSegments;

            var sp1 = c1.GetEndParameter(0);
            var ep1 = c1.GetEndParameter(1);
            var step1 = (ep1 - sp1) / _nSegments;

            Debug.Print("Two curves' step size [start, end]:"
                        + " {0} [{1},{2}] -- {3} [{4},{5}]",
                Util.RealString(step0),
                Util.RealString(sp0),
                Util.RealString(ep0),
                Util.RealString(step1),
                Util.RealString(sp1),
                Util.RealString(ep1));

            // Modify document within a transaction.

            using var tx = new Transaction(doc);
            var creator = new Creator(doc);

            tx.Start("MidCurve");

            // Current segment start points.

            var t0 = sp0;
            var t1 = sp1;

            var p0 = c0.GetEndPoint(0);
            var p1 = c1.GetEndPoint(0);
            var p = Util.Midpoint(p0, p1);

            Debug.Assert(
                p0.IsAlmostEqualTo(c0.Evaluate(t0, false)),
                "expected equal start points");

            Debug.Assert(
                p1.IsAlmostEqualTo(c1.Evaluate(t1, false)),
                "expected equal start points");

            // Current segment end points.

            t0 += step0;
            t1 += step1;

            XYZ q0, q1, q;
            Line line;

            for (var i = 0; i < _nSegments; ++i, t0 += step0, t1 += step1)
            {
                q0 = c0.Evaluate(t0, false);
                q1 = c1.Evaluate(t1, false);
                q = Util.Midpoint(q0, q1);

                Debug.Print(
                    "{0} {1} {2} {3}-{4} {5}-{6} {7}-{8}",
                    i,
                    Util.RealString(t0),
                    Util.RealString(t1),
                    Util.PointString(p0),
                    Util.PointString(q0),
                    Util.PointString(p1),
                    Util.PointString(q1),
                    Util.PointString(p),
                    Util.PointString(q));

                // Create approximating curve segment.

                line = Line.CreateBound(p, q);
                creator.CreateModelCurve(line);

                p0 = q0;
                p1 = q1;
                p = q;
            }

            tx.Commit();

            return Result.Succeeded;
        }
    }
}