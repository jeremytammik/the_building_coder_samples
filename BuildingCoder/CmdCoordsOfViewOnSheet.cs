#region Header

//
// CmdCoordsOfViewOnSheet.cs - retrieve coordinates of view on sheet
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
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
    internal class CmdCoordsOfViewOnSheet : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var currentSheet
                = doc.ActiveView as ViewSheet;

            //foreach( View v in currentSheet.Views ) // 2014 warning	'Autodesk.Revit.DB.ViewSheet.Views' is obsolete.  Use GetAllPlacedViews() instead.

            foreach (var id in currentSheet.GetAllPlacedViews()) // 2015
            {
                var v = doc.GetElement(id) as View;

                // the values returned here do not seem to
                // accurately reflect the positions of the
                // views on the sheet:

                var loc = v.Outline;

                Debug.Print(
                    "Coordinates of {0} view '{1}': {2}",
                    v.ViewType, v.Name,
                    Util.PointString(loc.Min));
            }

            return Result.Failed;
        }
    }
}