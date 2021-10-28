#region Header

//
// CmdWallFooting.cs - determine wall footing from wall.
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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdWallFooting : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            if (Util.SelectSingleElementOfType(uidoc, typeof(Wall), "a wall", false) is not Wall wall)
            {
                message = "Please select a single wall element.";

                return Result.Failed;
            }

            ICollection<ElementId> delIds = null;

            using (var t = new Transaction(doc))
            {
                try
                {
                    t.Start("Temporary Wall Deletion");

                    delIds = doc.Delete(wall.Id);

                    t.RollBack();
                }
                catch (Exception ex)
                {
                    message = $"Deletion failed: {ex.Message}";
                    t.RollBack();
                }
            }

            WallFoundation footing = null;

            foreach (var id in delIds)
            {
                footing = doc.GetElement(id) as WallFoundation;

                if (null != footing) break;
            }

            var s = Util.ElementDescription(wall);

            Util.InfoMsg(null == footing
                ? $"No footing found for {s}."
                : $"{s} has {Util.ElementDescription(footing)}.");

            return Result.Succeeded;
        }
    }
}