#region Header

//
// CmdEllipticalArc.cs - create an elliptical arc geometry object
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdEllipticalArc : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application.Application;

            var c = CreateEllipse(app);

            return Result.Failed;
        }

        /// <summary>
        ///     Create and return a new elliptical arc geometry object
        ///     with a given start and end angle.
        /// </summary>
        private Curve CreateEllipse(Application app)
        {
            var center = XYZ.Zero;

            double radX = 30;
            double radY = 50;

            var xVec = XYZ.BasisX;
            var yVec = XYZ.BasisY;

            var param0 = 0.0;
            var param1 = 2 * Math.PI;

            //Ellipse e = app.Create.NewEllipse( center, radX, radY, xVec, yVec, param0, param1 ); // 2013
            //Ellipse e = Ellipse.Create( center, radX, radY, xVec, yVec, param0, param1 ); // 2014

            var c = Ellipse.CreateCurve(center, radX, radY, xVec, yVec, param0, param1); // 2018

            // Create a line from ellipse center in
            // direction of target angle:

            var targetAngle = Math.PI / 3.0;

            var direction = new XYZ(
                Math.Cos(targetAngle),
                Math.Sin(targetAngle),
                0);

            //Line line = app.Create.NewLineUnbound( center, direction ); // 2013

            var line = Line.CreateUnbound(center, direction); // 2014

            // Find intersection between line and ellipse:

            IntersectionResultArray results;
            c.Intersect(line, out results);

            // Find the shortest intersection segment:

            foreach (IntersectionResult result in results)
            {
                var p = result.UVPoint.U;
                if (p < param1) param1 = p;
            }

            // Apply parameter to the ellipse:

            c.MakeBound(param0, param1);

            return c;
        }
    }
}