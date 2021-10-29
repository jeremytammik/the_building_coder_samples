#region Header

//
// CmdNewLineLoad.cs - create a new structural line load element
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
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion // Namespaces

// 1251154 [NewLineLoad]

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewLineLoad : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Debug.Assert(false, "This has not been tested since Revit 2010!");

            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var ca
                = app.Application.Create;

            var cd
                = doc.Create;

            // determine line load symbol to use:

            var symbols
                = new FilteredElementCollector(doc);

            symbols.OfClass(typeof(LineLoadType));

            var loadSymbol
                = symbols.FirstElement() as LineLoadType;

            // sketch plane and arrays of forces and moments:

            //Plane plane = ca.NewPlane( XYZ.BasisZ, XYZ.Zero ); // 2016
            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero); // 2017

            using var t = new Transaction(doc);
            t.Start("Create New Line Load");

            //SketchPlane skplane = cd.NewSketchPlane( plane ); // 2013

            var skplane = SketchPlane.Create(doc, plane); // 2014

            var forceA = new XYZ(0, 0, 5);
            var forceB = new XYZ(0, 0, 10);
            var forces = new List<XYZ>();
            forces.Add(forceA);
            forces.Add(forceB);

            var momentA = new XYZ(0, 0, 0);
            var momentB = new XYZ(0, 0, 0);
            var moments = new List<XYZ>();
            moments.Add(momentA);
            moments.Add(momentB);

            var bic
                = BuiltInCategory.OST_StructuralFraming;

            var beams = Util.GetElementsOfType(
                doc, typeof(FamilyInstance), bic);

            var p1 = new XYZ(0, 0, 0);
            var p2 = new XYZ(3, 0, 0);
            //List<XYZ> points = new List<XYZ>();
            //points.Add( p1 );
            //points.Add( p2 );

            // create a new unhosted line load on points:

            //LineLoad lineLoadNoHost = cd.NewLineLoad(
            //  points, forces, moments,
            //  false, false, false,
            //  loadSymbol, skplane ); // 2015

            var lineLoadNoHost = LineLoad.Create(doc,
                p1, p2, forces[0], moments[0],
                loadSymbol, skplane); // 2016

            Debug.Print("Unhosted line load works.");

            // create new line loads on beam:

            foreach (var e in beams)
            {
                try
                {
                    //LineLoad lineLoad = cd.NewLineLoad(
                    //  e, forces, moments,
                    //  false, false, false,
                    //  loadSymbol, skplane ); // 2015

                    var amsurf = e.GetAnalyticalModel()
                        as AnalyticalModelSurface;

                    var lineLoad = LineLoad.Create(doc,
                        amsurf, 0, forces[0], moments[0], loadSymbol); // 2016

                    Debug.Print("Hosted line load on beam works.");
                }
                catch (Exception ex)
                {
                    Debug.Print($"Hosted line load on beam fails: {ex.Message}");
                }

                var i = e as FamilyInstance;

                var am = i.GetAnalyticalModel();

                foreach (var curve in
                    am.GetCurves(AnalyticalCurveType.ActiveCurves))
                    try
                    {
                        //LineLoad lineLoad = cd.NewLineLoad(
                        //  curve.Reference, forces, moments,
                        //  false, false, false,
                        //  loadSymbol, skplane ); // 2015

                        var amstick = e.GetAnalyticalModel()
                            as AnalyticalModelStick;

                        var lineLoad = LineLoad.Create(doc,
                            amstick, forces[0], moments[0], loadSymbol); // 2016

                        Debug.Print("Hosted line load on "
                                    + "AnalyticalModelFrame curve works.");
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"Hosted line load on AnalyticalModelFrame curve fails: {ex.Message}");
                    }
            }

            t.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Create a point load on all
        ///     analytical column end points.
        /// </summary>
        private void CreatePointLoadOnColumnEnd(Document doc)
        {
            // Find all AM column instances in the document

            var columns
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_ColumnAnalytical)
                    .WhereElementIsNotElementType();

            foreach (AnalyticalModel am in columns)
            {
                var curve = am.GetCurve();

                var selector
                    = new AnalyticalModelSelector(curve);

                selector.CurveSelector
                    = AnalyticalCurveSelector.EndPoint;

                var endPointRef
                    = am.GetReference(selector);

                using var tx = new Transaction(doc);
                tx.Start("NewPointBoundaryConditions");

                var newPointBC
                    = doc.Create.NewPointBoundaryConditions(
                        endPointRef,
                        TranslationRotationValue.Fixed, 0,
                        TranslationRotationValue.Spring, 1.0,
                        TranslationRotationValue.Fixed, 0,
                        TranslationRotationValue.Fixed, 0,
                        TranslationRotationValue.Fixed, 0,
                        TranslationRotationValue.Fixed, 0);

                newPointBC.SetOrientTo(
                    BoundaryConditionsOrientTo
                        .HostLocalCoordinateSystem);

                tx.Commit();
            }
        }
    }
}