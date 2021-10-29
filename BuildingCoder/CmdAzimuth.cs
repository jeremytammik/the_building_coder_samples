#region Header

//
// CmdAzimuth.cs - determine direction
// of a line with regard to the north
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdAzimuth : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Util.ListForgeTypeIds();

            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var e = Util.SelectSingleElement(
                uidoc, "a line or wall");

            LocationCurve curve = null;

            if (null == e)
                message = "No element selected";
            else
                curve = e.Location as LocationCurve;

            if (null == curve)
            {
                message = "No curve available";
            }
            else
            {
                var p = curve.Curve.GetEndPoint(0);
                var q = curve.Curve.GetEndPoint(1);

                Debug.WriteLine($"Start point {Util.PointString(p)}");

                Debug.WriteLine($"End point {Util.PointString(q)}");

                // the angle between the vectors from the project origin
                // to the start and end points of the wall is pretty irrelevant:

                var a = p.AngleTo(q);
                Debug.WriteLine($"Angle between start and end point vectors = {Util.AngleString(a)}");

                var v = q - p;
                var vx = XYZ.BasisX;
                a = vx.AngleTo(v);
                Debug.WriteLine($"Angle between points measured from X axis = {Util.AngleString(a)}");

                var z = XYZ.BasisZ;
                a = vx.AngleOnPlaneTo(v, z);
                Debug.WriteLine(
                    $"Angle around measured from X axis = {Util.AngleString(a)}");

                if (e is Wall wall)
                {
                    var w = z.CrossProduct(v).Normalize();
                    if (wall.Flipped) w = -w;
                    a = vx.AngleOnPlaneTo(w, z);
                    Debug.WriteLine(
                        $"Angle pointing out of wall = {Util.AngleString(a)}");
                }
            }

            foreach (ProjectLocation location in doc.ProjectLocations)
            {
                //ProjectPosition projectPosition
                //  = location.get_ProjectPosition( XYZ.Zero ); // 2017

                var projectPosition = location.GetProjectPosition(XYZ.Zero); // 2018

                var pna = projectPosition.Angle;
                Debug.WriteLine($"Angle between project north and true north {Util.AngleString(pna)}");
            }

            return Result.Failed;
        }
    }
}