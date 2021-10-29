#region Header

//
// CmdProcessVisibleDwg.cs - extract geometry from visible import instance layers in the current view
//
// Copyright (C) 2018-2020 by Ryan Goertzen, Goertzen Enterprises, and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdProcessVisibleDwg : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;

            ProcessVisible(uidoc);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Pick a DWG import instance, extract polylines
        ///     from it visible in the current view and create
        ///     filled regions from them.
        /// </summary>
        public void ProcessVisible(UIDocument uidoc)
        {
            var doc = uidoc.Document;
            var active_view = doc.ActiveView;

            var visible_dwg_geo
                = new List<GeometryObject>();

            // Pick Import Instance

            var r = uidoc.Selection.PickObject(
                ObjectType.Element,
                new JtElementsOfClassSelectionFilter<ImportInstance>());

            var import = doc.GetElement(r) as ImportInstance;

            // Get Geometry

            var ge = import.get_Geometry(new Options());

            foreach (var go in ge)
                if (go is GeometryInstance gi)
                {
                    var ge2 = gi.GetInstanceGeometry();

                    if (ge2 != null)
                        foreach (var obj in ge2)
                            // Only work on PolyLines

                            if (obj is PolyLine)
                            {
                                // Use the GraphicsStyle to get the 
                                // DWG layer linked to the Category 
                                // for visibility.

                                var gStyle = doc.GetElement(
                                    obj.GraphicsStyleId) as GraphicsStyle;

                                // Check if the layer is visible in the view.

                                if (!active_view.GetCategoryHidden(
                                    gStyle.GraphicsStyleCategory.Id))
                                    visible_dwg_geo.Add(obj);
                            }
                }

            // Do something with the info

            if (visible_dwg_geo.Count > 0)
            {
                // Retrieve first filled region type

                var filledType = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfClass(typeof(FilledRegionType))
                    .OfType<FilledRegionType>()
                    .First();

                using var t = new Transaction(doc);
                t.Start("ProcessDWG");

                foreach (var obj in visible_dwg_geo)
                {
                    var poly = obj as PolyLine;

                    // Draw a filled region for each polyline

                    if (null != poly)
                    {
                        // Create loops for detail region

                        var curveLoop = new CurveLoop();

                        var points = poly.GetCoordinates();

                        for (var i = 0; i < points.Count - 1; ++i)
                            curveLoop.Append(Line.CreateBound(
                                points[i], points[i + 1]));

                        FilledRegion.Create(doc,
                            filledType.Id, active_view.Id,
                            new List<CurveLoop> {curveLoop});
                    }
                }

                t.Commit();
            }
        }
    }
}