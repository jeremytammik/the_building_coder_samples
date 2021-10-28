#region Header

//
// CmdPostRequestInstancePlacement.cs - Exercise the PostRequestForElementTypePlacement method
//
// Copyright (C) 2015-2021 by Jeremy Tammik,
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

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdPostRequestInstancePlacement : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var elementType
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Walls) // .OST_Columns
                    .OfClass(typeof(ElementType))
                    .FirstElement() as ElementType;

            uidoc.PostRequestForElementTypePlacement(
                elementType);

            return Result.Succeeded;
        }
    }
}