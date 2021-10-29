#region Header

//
// CmdListViews.cs - determine all the view
// ports of a drawing sheet and vice versa
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListViews : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var run_ViewSheetSet_Views_benchmark = true;

            if (run_ViewSheetSet_Views_benchmark)
            {
                var s = GetViewSheetSetViewsBenchmark(doc);
                var td = new TaskDialog(
                    "ViewSheetSet.Views Benchmark");
                td.MainContent = s;
                td.Show();
                return Result.Succeeded;
            }

            var sheets
                = new FilteredElementCollector(doc);

            sheets.OfClass(typeof(ViewSheet));

            // map with key = sheet element id and
            // value = list of viewport element ids:

            var
                mapSheetToViewport =
                    new Dictionary<ElementId, List<ElementId>>();

            // map with key = viewport element id and
            // value = sheet element id:

            var mapViewportToSheet
                = new Dictionary<ElementId, ElementId>();

            foreach (ViewSheet sheet in sheets)
            {
                //int n = sheet.Views.Size; // 2014

                var viewIds = sheet.GetAllPlacedViews(); // 2015

                var n = viewIds.Count;

                Debug.Print(
                    "Sheet {0} contains {1} view{2}: ",
                    Util.ElementDescription(sheet),
                    n, Util.PluralSuffix(n));

                var idSheet = sheet.Id;

                var i = 0;

                foreach (var id in viewIds)
                {
                    var v = doc.GetElement(id) as View;

                    BoundingBoxXYZ bb;

                    bb = v.get_BoundingBox(doc.ActiveView);

                    Debug.Assert(null == bb,
                        "expected null view bounding box");

                    bb = v.get_BoundingBox(sheet);

                    Debug.Assert(null == bb,
                        "expected null view bounding box");

                    var viewport = GetViewport(sheet, v);

                    // null if not in active view:

                    bb = viewport.get_BoundingBox(doc.ActiveView);

                    var outline = v.Outline;

                    Debug.WriteLine("  {0} {1} bb {2} outline {3}", ++i, Util.ElementDescription(v), null == bb ? "<null>" : Util.BoundingBoxString(bb),
                        Util.BoundingBoxString(outline));

                    if (!mapSheetToViewport.ContainsKey(idSheet))
                        mapSheetToViewport.Add(idSheet,
                            new List<ElementId>());
                    mapSheetToViewport[idSheet].Add(v.Id);

                    Debug.Assert(
                        !mapViewportToSheet.ContainsKey(v.Id),
                        "expected viewport to be contained"
                        + " in only one single sheet");

                    mapViewportToSheet.Add(v.Id, idSheet);
                }
            }

            return Result.Cancelled;
        }

        /// <summary>
        ///     Return the viewport on the given
        ///     sheet displaying the given view.
        /// </summary>
        private Element GetViewport(ViewSheet sheet, View view)
        {
            var doc = sheet.Document;

            // filter for view name:

            var bip
                = BuiltInParameter.VIEW_NAME;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterStringRuleEvaluator evaluator
                = new FilterStringEquals();

            //FilterRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, view.Name, true );

            FilterRule rule = new FilterStringRule( // 2022
                provider, evaluator, view.Name);

            var name_filter
                = new ElementParameterFilter(rule);

            var bic
                = BuiltInCategory.OST_Viewports;

            // retrieve the specific named viewport:

            //Element viewport
            //  = new FilteredElementCollector( doc )
            //    .OfCategory( bic )
            //    .WherePasses( name_filter )
            //    .FirstElement();
            //return viewport;

            // unfortunately, there are not just one,
            // but two candidate elements. apparently,
            // we can distibuish them using the
            // owner view id property:

            var viewports
                = new List<Element>(
                    new FilteredElementCollector(doc)
                        .OfCategory(bic)
                        .WherePasses(name_filter)
                        .ToElements());

            Debug.Assert(viewports[0].OwnerViewId.Equals(ElementId.InvalidElementId),
                "expected the first viewport to have an invalid owner view id");

            Debug.Assert(!viewports[1].OwnerViewId.Equals(ElementId.InvalidElementId),
                "expected the second viewport to have a valid owner view id");

            var i = 1;

            return viewports[i];
        }

        private string GetViewSheetSetViewsBenchmark(Document doc)
        {
            var sheetSets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheetSet));

            var n = sheetSets.GetElementCount();

            var result = $"Total of {n} sheet sets in this project.\n\n";

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (ViewSheetSet set in sheetSets)
            {
                result += set.Name;

                // getting the Views property takes around 
                // 1.5 seconds on the given sample.rvt file.

                var views = set.Views;

                result += $" has {views.Size} views.\n";
            }

            stopWatch.Stop();

            double ms = stopWatch.ElapsedMilliseconds;

            result += $"\nOperation completed in {Math.Round(ms / 1000.0, 3)} seconds.\nAverage of {ms / n} ms per loop iteration.";

            return result;
        }
    }
}

// C:\a\j\adn\case\bsd\1266302\attach\rst_basic_sample_project_reinf.rvt