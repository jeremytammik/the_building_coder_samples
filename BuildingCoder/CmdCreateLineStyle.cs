#region Header

//
// CmdCreateLineStyle.cs - create a new line style using NewSubcategory
//
// Copyright (C) 2016-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdCreateLineStyle : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var doc = uiapp.ActiveUIDocument.Document;

            CreateLineStyle(doc);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Create a new line style using NewSubcategory
        /// </summary>
        private void CreateLineStyle(Document doc)
        {
            // Use this to access the current document in a macro.
            //
            //Document doc = this.ActiveUIDocument.Document;

            // Find existing linestyle.  Can also opt to
            // create one with LinePatternElement.Create()

            var fec
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(LinePatternElement));

            var linePatternElem = fec
                .Cast<LinePatternElement>()
                .First(linePattern
                    => linePattern.Name == "Long Dash");

            // The new linestyle will be a subcategory 
            // of the Lines category        

            var categories = doc.Settings.Categories;

            var lineCat = categories.get_Item(
                BuiltInCategory.OST_Lines);

            using var t = new Transaction(doc);
            t.Start("Create LineStyle");

            // Add the new linestyle 

            var newLineStyleCat = categories
                .NewSubcategory(lineCat, "New LineStyle");

            doc.Regenerate();

            // Set the linestyle properties 
            // (weight, color, pattern).

            newLineStyleCat.SetLineWeight(8,
                GraphicsStyleType.Projection);

            newLineStyleCat.LineColor = new Color(
                0xFF, 0x00, 0x00);

            newLineStyleCat.SetLinePatternId(
                linePatternElem.Id,
                GraphicsStyleType.Projection);

            t.Commit();
        }
    }
}