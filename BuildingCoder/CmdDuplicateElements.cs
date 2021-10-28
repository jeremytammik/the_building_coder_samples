#region Header

//
// CmdDuplicateElement.cs - duplicate selected elements
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdDuplicateElements : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            using var tx = new Transaction(doc);
            tx.Start("Duplicate Elements");

            //Group group = doc.Create.NewGroup( // 2012
            //  uidoc.Selection.Elements );

            var group = doc.Create.NewGroup( // 2013
                uidoc.Selection.GetElementIds());

            var groupType = group.GroupType;

            var location = group.Location
                as LocationPoint;

            var p = location.Point;
            var newPoint = new XYZ(p.X, p.Y + 10, p.Z);

            var newGroup = doc.Create.PlaceGroup(
                newPoint, group.GroupType);

            //group.Ungroup(); // 2012
            group.UngroupMembers(); // 2013

            //ElementSet eSet = newGroup.Ungroup(); // 2012

            var eIds
                = newGroup.UngroupMembers(); // 2013

            doc.Delete2(groupType);

            // change the property or parameter values
            // of the member elements as required...

            tx.Commit();

            return Result.Succeeded;
        }

        #region Which target view

        /// <summary>
        ///     Create a new group of the specified elements
        ///     in the current active view at the given offset.
        /// </summary>
        private void CreateGroup(
            Document doc,
            ICollection<ElementId> ids,
            XYZ offset)
        {
            var group = doc.Create.NewGroup(ids);

            var location = group.Location
                as LocationPoint;

            var p = location.Point + offset;

            var newGroup = doc.Create.PlaceGroup(
                p, group.GroupType);

            group.UngroupMembers();
        }

        #endregion // Which target view
    }
}