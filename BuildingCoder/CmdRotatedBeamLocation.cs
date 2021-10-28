#region Header

//
// CmdRotatedBeamLocation.cs - determine location of rotated beam
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

// C:\a\doc\revit\blog\img\three_beams.png
// C:\a\doc\revit\blog\img\rotated_beam.jpg

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdRotatedBeamLocation : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var beam = Util.SelectSingleElementOfType(
                uidoc, typeof(FamilyInstance), "a beam", false) as FamilyInstance;

            var bic
                = BuiltInCategory.OST_StructuralFraming;

            if (null == beam
                || null == beam.Category
                || !beam.Category.Id.IntegerValue.Equals((int) bic))
            {
                message = "Please select a single beam element.";
            }
            else
            {
                if (beam.Location is not LocationCurve curve)
                {
                    message = "No curve available";
                    return Result.Failed;
                }

                var p = curve.Curve.GetEndPoint(0);
                var q = curve.Curve.GetEndPoint(1);
                var v = 0.1 * (q - p);
                p = p - v;
                q = q + v;

                //Creator creator = new Creator( doc );
                //creator.CreateModelLine( p, q );

                using var t = new Transaction(doc);
                t.Start("Create Model Line");

                Creator.CreateModelLine(doc, p, q);

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}