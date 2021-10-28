#region Header

//
// CmdPurgeLineStyles.cs - purge specific line styles
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    internal class CmdPurgeLineStyles : IExternalCommand
    {
        private const string _line_style_name = "_Solid-Red-1";

        /// <summary>
        ///     External command Execute method.
        /// </summary>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var doc = uiapp.ActiveUIDocument.Document;

            PurgeGraphicStyles(doc, _line_style_name);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Purge all graphic styles whose name contains
        ///     the given substring. Watch out what you do!
        ///     If your substring is empty, this might delete
        ///     all graphic styles in the entire project!
        /// </summary>
        private void PurgeGraphicStyles(
            Document doc,
            string name_substring)
        {
            var graphic_styles
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(GraphicsStyle));

            var n1 = graphic_styles.Count();

            var red_line_styles
                = graphic_styles.Where(e
                    => e.Name.Contains(name_substring));

            var n2 = red_line_styles.Count();

            if (0 < n2)
            {
                using var tx = new Transaction(doc);
                tx.Start("Delete Line Styles");

                doc.Delete(red_line_styles
                    .Select(e => e.Id)
                    .ToArray());

                tx.Commit();

                TaskDialog.Show("Purge line styles",
                    $"Deleted {n2} graphic style{(1 == n2 ? "" : "s")} named '*{name_substring}*' from {n1} total graohic styles.");
            }
        }

        /// <summary>
        ///     Revit macro mainline.
        ///     Uncomment the line referencing 'this'.
        /// </summary>
        public void PurgeLineStyles_macro_mainline()
        {
            Document doc = null; // in a macro, use this.Document
            var name = "_Solid-Red-1";
            PurgeGraphicStyles(doc, name);
        }
    }
}