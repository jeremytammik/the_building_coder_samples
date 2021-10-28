#region Header

//
// CmdNewArea.cs - create a new area element
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewArea : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var rc = Result.Failed;

            if (commandData.View is not ViewPlan {ViewType: ViewType.AreaPlan} view)
            {
                message = "Please run this command in an area plan view.";
                return rc;
            }

            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var room = Util.GetSingleSelectedElement(uidoc);

            if (room is not Room) room = Util.SelectSingleElement(uidoc, "a room");

            if (room is not Room)
            {
                message = "Please select a single room element.";
            }
            else
            {
                using var t = new Transaction(doc);
                t.Start("Create New Area");

                var loc = room.Location;
                var lp = loc as LocationPoint;
                var p = lp.Point;
                var q = new UV(p.X, p.Y);
                var area = doc.Create.NewArea(view, q);
                rc = Result.Succeeded;
                t.Commit();
            }

            return rc;
        }
    }
}