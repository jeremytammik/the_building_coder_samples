#region Header

//
// CmdRebarCurves.cs - Retrieve and duplicate all rebar centreline curves with model curves
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
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
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdRebarCurves : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            // Retrieve all rebar curves

            var curves = GetRebarCurves(doc);

            // Create a simplified 3D shape to represent them
            //
            // A sweep would be nice and easy; however, 
            // NewSweep only works in the family environment.
            // Next idea: for straight line segments, it is 
            // trivial to create an extruded regular polygon.
            // For curves ones not quite so easy. However, we
            // could do the following: 
            // From the curve, generate a certain number of 
            // offset curves parallel to the original.
            // Tesselate each of them -- no, that will be too fine.
            // Calculate a certain number of points along them.
            // Generate triangles between the points to define 
            // a DirectShape approximating the rebar.

            foreach (var c in curves)
            {
                //CreateTubeAroundCurve( doc, c );
            }

            return Result.Succeeded;
        }

        private static IList<Curve> GetRebarCurves(Document doc)
        {
            // I’m using a filter which gives me all rebar 
            // elements. For each rebar, we calculate for each 
            // bar in set it’s curves using Rebar.GetCenterlineCurves.
            // In case of shape driven, we also move the curves for
            // the bar at position `i` to their real position.

            IList<Curve> curves = new List<Curve>();

            var rebars
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Rebar));

            int n, nElements = 0, nCurves = 0;

            foreach (Rebar rebar in rebars)
            {
                ++nElements;

                n = rebar.NumberOfBarPositions;

                nCurves += n;

                for (var i = 0; i < n; ++i)
                {
                    // Retrieve the curves of i'th bar in the set.
                    // In case of shape driven rebar, they will be 
                    // positioned at the location of the first bar 
                    // in set.

                    var centerlineCurves
                        = rebar.GetCenterlineCurves(
                            true, false, false,
                            MultiplanarOption.IncludeAllMultiplanarCurves,
                            i);

                    // Move the curves to their position.

                    if (rebar.IsRebarShapeDriven())
                    {
                        var accessor
                            = rebar.GetShapeDrivenAccessor();

                        var trf = accessor
                            .GetBarPositionTransform(i);

                        foreach (var c in centerlineCurves) curves.Add(c.CreateTransformed(trf));
                    }
                    else
                    {
                        // This is a Free Form Rebar

                        foreach (var c in centerlineCurves)
                            curves.Add(c);
                    }
                }
            }

            n = curves.Count;

            Debug.Print("Processed {0} rebar element{1} "
                        + "with {2} bar position{3}, extracted {4} "
                        + "curve{5}",
                nElements, Util.PluralSuffix(nElements),
                nCurves, Util.PluralSuffix(nCurves),
                n, Util.PluralSuffix(n));

            return curves;
        }
    }
}