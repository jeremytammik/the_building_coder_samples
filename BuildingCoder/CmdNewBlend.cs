#region Header

//
// CmdNewBlend.cs - create a new blend element using the NewBlend method
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewBlend : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            if (doc.IsFamilyDocument)
            {
                using var t = new Transaction(doc);
                t.Start("New Blend");

                var blend = CreateBlend(doc);

                t.Commit();

                return Result.Succeeded;
            }

            message = "Please run this command "
                      + "in a family document.";

            return Result.Failed;
        }

        private static Blend CreateBlend(Document doc)
        {
            Debug.Assert(doc.IsFamilyDocument,
                "this method will only work in a family document");

            var app = doc.Application;

            var creApp
                = app.Create;

            var factory
                = doc.FamilyCreate;

            double startAngle = 0;
            var midAngle = Math.PI;
            var endAngle = 2 * Math.PI;

            var xAxis = XYZ.BasisX;
            var yAxis = XYZ.BasisY;

            var center = XYZ.Zero;
            var normal = -XYZ.BasisZ;
            var radius = 0.7579;

            //Arc arc1 = creApp.NewArc( center, radius, startAngle, midAngle, xAxis, yAxis ); // 2013
            //Arc arc2 = creApp.NewArc( center, radius, midAngle, endAngle, xAxis, yAxis ); // 2013

            var arc1 = Arc.Create(center, radius, startAngle, midAngle, xAxis, yAxis); // 2014
            var arc2 = Arc.Create(center, radius, midAngle, endAngle, xAxis, yAxis); // 2014

            var baseProfile = new CurveArray();

            baseProfile.Append(arc1);
            baseProfile.Append(arc2);

            // create top profile:

            var topProfile = new CurveArray();

            var circular_top = false;

            if (circular_top)
            {
                // create a circular top profile:

                var center2 = new XYZ(0, 0, 1.27);

                //Arc arc3 = creApp.NewArc( center2, radius, startAngle, midAngle, xAxis, yAxis ); // 2013
                //Arc arc4 = creApp.NewArc( center2, radius, midAngle, endAngle, xAxis, yAxis ); // 2013

                var arc3 = Arc.Create(center2, radius, startAngle, midAngle, xAxis, yAxis); // 2014
                var arc4 = Arc.Create(center2, radius, midAngle, endAngle, xAxis, yAxis); // 2014

                topProfile.Append(arc3);
                topProfile.Append(arc4);
            }
            else
            {
                // create a skewed rectangle top profile:

                var pts = new[]
                {
                    new(0, 0, 3),
                    new XYZ(2, 0, 3),
                    new XYZ(3, 2, 3),
                    new XYZ(0, 4, 3)
                };

                for (var i = 0; i < 4; ++i)
                    //topProfile.Append( creApp.NewLineBound( // 2013

                    topProfile.Append(Line.CreateBound( // 2014
                        pts[0 == i ? 3 : i - 1], pts[i]));
            }

            //Plane basePlane = creApp.NewPlane( normal, center ); // 2016
            var basePlane = Plane.CreateByNormalAndOrigin(normal, center); // 2017

            //SketchPlane sketch = factory.NewSketchPlane( basePlane ); // 2013
            var sketch = SketchPlane.Create(doc, basePlane); // 2014

            var blend = factory.NewBlend(true,
                topProfile, baseProfile, sketch);

            return blend;
        }
    }
}