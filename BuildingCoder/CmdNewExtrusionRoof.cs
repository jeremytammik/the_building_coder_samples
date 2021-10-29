#region Header

//
// CmdNewExtrusionRoof.cs - create a strangely stair shaped new extrusion roof
//
// Copyright (C) 2014-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewExtrusionRoof : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            using var tx = new Transaction(doc);
            tx.Start("NewExtrusionRoof");

            var fs
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .FirstOrDefault(a => null != a);

            var lvl
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(a => null != a);

            double x = 1;

            var origin = new XYZ(x, 0, 0);
            var vx = XYZ.BasisY;
            var vy = XYZ.BasisZ;

            var sp = SketchPlane.Create(doc,
                //new Autodesk.Revit.DB.Plane( vx, vy, origin ) // 2016
                Plane.CreateByOriginAndBasis(origin, vx, vy)); // 2017
            //Plane.CreateByOriginAndBasis( origin, vy, vx ) ); // 2019

            var ca = new CurveArray();

            // This stair shape causes NewExtrusionRoof to
            // throw an exception in Revit 2019.1.

            var pts = new[]
            {
                new(x, 1, 0),
                new XYZ(x, 1, 1),
                new XYZ(x, 2, 1),
                new XYZ(x, 2, 2),
                new XYZ(x, 3, 2),
                new XYZ(x, 3, 3),
                new XYZ(x, 4, 3),
                new XYZ(x, 4, 4)
            };

            // Try a simple and closed rectangular shape.
            // This throws an invalid operation exception 
            // saying "Invalid profile."

            pts = new[]
            {
                new(x, 1, 0),
                new XYZ(x, 1, 1),
                new XYZ(x, 2, 1),
                new XYZ(x, 2, 0)
            };

            var n = pts.Length;

            for (var i = 1; i < n; ++i)
                ca.Append(Line.CreateBound(
                    pts[i - 1], pts[i]));
            ca.Append(Line.CreateBound(
                pts[n - 1], pts[0]));

            doc.Create.NewModelCurveArray(ca, sp);

            var v = doc.ActiveView;

            var rp
                = doc.Create.NewReferencePlane2(
                    origin, origin + vx, origin + vy, v);

            rp.Name = "MyRoofPlane";

            var er
                = doc.Create.NewExtrusionRoof(
                    ca, rp, lvl, fs, 0, 3);

            Debug.Print($"Extrusion roof element id: {er.Id}");

            tx.Commit();

            return Result.Succeeded;
        }

        #region Revit Online Help sample code from the section on Roofs

        private void f(Document doc)
        {
            // Before invoking this sample, select some walls 
            // to add a roof over. Make sure there is a level 
            // named "Roof" in the document.

            var level
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Where(e =>
                        !string.IsNullOrEmpty(e.Name)
                        && e.Name.Equals("Roof"))
                    .FirstOrDefault() as Level;

            var roofType
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .FirstOrDefault() as RoofType;

            // Get the handle of the application
            var application = doc.Application;

            // Define the footprint for the roof based on user selection
            var footprint = application.Create
                .NewCurveArray();

            var uidoc = new UIDocument(doc);

            var selectedIds
                = uidoc.Selection.GetElementIds();

            if (selectedIds.Count != 0)
                foreach (var id in selectedIds)
                {
                    var element = doc.GetElement(id);
                    switch (element)
                    {
                        case Wall wall:
                        {
                            var wallCurve = wall.Location as LocationCurve;
                            footprint.Append(wallCurve.Curve);
                            continue;
                        }
                        case ModelCurve modelCurve:
                            footprint.Append(modelCurve.GeometryCurve);
                            break;
                    }
                }
            else
                throw new Exception(
                    "Please select a curve loop, wall loop or "
                    + "combination of walls and curves to "
                    + "create a footprint roof.");

            var footPrintToModelCurveMapping
                = new ModelCurveArray();

            var footprintRoof
                = doc.Create.NewFootPrintRoof(
                    footprint, level, roofType,
                    out footPrintToModelCurveMapping);

            var iterator
                = footPrintToModelCurveMapping.ForwardIterator();

            iterator.Reset();
            while (iterator.MoveNext())
            {
                var modelCurve = iterator.Current as ModelCurve;
                footprintRoof.set_DefinesSlope(modelCurve, true);
                footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            }
        }

        #endregion // Revit Online Help sample code from the section on Roofs
    }
}