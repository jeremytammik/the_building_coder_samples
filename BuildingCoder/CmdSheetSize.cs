#region Header

//
// CmdSheetSize.cs - list title block element types and title block and view sheet instances and sizes
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    internal class CmdSheetSize : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            FilteredElementCollector a;
            Parameter p;
            int n;

            #region Using the obsolete TitleBlocks property

#if BEFORE_REVIT_2015
      // The TitleBlocks property was declared deprecated
      // in the Revit 2014 API, and removed in Revit 2015.

      // Using the obsolete deprecated TitleBlocks property

      FamilySymbolSet titleBlocks = doc.TitleBlocks;

      n = titleBlocks.Size;

      Debug.Print(
        "{0} title block element type{1} listed "
        + "in doc.TitleBlocks collection{2}",
        n,
        ( 1 == n ? "" : "s" ),
        ( 0 == n ? "." : ":" ) );

      string s;

      foreach( FamilySymbol tb in titleBlocks )
      {
        // these are the family symbols,
        // i.e. the title block element types,
        // i.e. not instances, i.e. not sheets,
        // and they obviously do not have any sheet
        // number, width or height, so 's' ends up empty:

        s = GetParameterValueString( tb, BuiltInParameter.SHEET_NUMBER )
          + GetParameterValueString( tb, BuiltInParameter.SHEET_WIDTH )
          + GetParameterValueString( tb, BuiltInParameter.SHEET_HEIGHT );

        Debug.Print(
          "Title block element type {0} {1}" + s,
          tb.Name, tb.Id.IntegerValue );
      }
#endif // BEFORE_REVIT_2015

            #endregion // Using the obsolete TitleBlocks property

            // Using this filter returns the same elements
            // as the doc.TitleBlocks collection:

            a = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol));

            n = a.ToElementIds().Count;

            Debug.Print("{0} title block element type{1} "
                        + "retrieved by filtered element collector{2}",
                n,
                1 == n ? "" : "s",
                0 == n ? "." : ":");

            foreach (FamilySymbol symbol in a)
                Debug.Print(
                    "Title block element type {0} {1}",
                    symbol.Name, symbol.Id.IntegerValue);

            // Retrieve the title block instances:

            a = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilyInstance));

            Debug.Print("Title block instances:");

            foreach (FamilyInstance e in a)
            {
                p = e.get_Parameter(
                    BuiltInParameter.SHEET_NUMBER);

                Debug.Assert(null != p,
                    "expected valid sheet number");

                var sheet_number = p.AsString();

                p = e.get_Parameter(
                    BuiltInParameter.SHEET_WIDTH);

                Debug.Assert(null != p,
                    "expected valid sheet width");

                var swidth = p.AsValueString();
                var width = p.AsDouble();

                p = e.get_Parameter(
                    BuiltInParameter.SHEET_HEIGHT);

                Debug.Assert(null != p,
                    "expected valid sheet height");

                var sheight = p.AsValueString();
                var height = p.AsDouble();

                var typeId = e.GetTypeId();
                var type = doc.GetElement(typeId);

                Debug.Print(
                    "Sheet number {0} size is {1} x {2} "
                    + "({3} x {4}), id {5}, type {6} {7}",
                    sheet_number, swidth, sheight,
                    Util.RealString(width),
                    Util.RealString(height),
                    e.Id.IntegerValue,
                    type.Name, typeId.IntegerValue);
            }

            // Retrieve the view sheet instances:

            a = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet));

            Debug.Print("View sheet instances:");

            foreach (ViewSheet vs in a)
            {
                var number = vs.SheetNumber;
                Debug.Print(
                    "View sheet name {0} number {1} id {2}",
                    vs.Name, vs.SheetNumber,
                    vs.Id.IntegerValue);
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Read the title block parameters to retrieve the
        ///     label parameters Sheet Number, Author and Client
        ///     Name
        /// </summary>
        private static void ReadTitleBlockLabelParameters(
            Document doc)
        {
            var title_block_instances
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .OfClass(typeof(FamilyInstance));

            Parameter p;

            Debug.Print("Title block instances:");

            foreach (FamilyInstance tb in title_block_instances)
            {
                var typeId = tb.GetTypeId();
                var type = doc.GetElement(typeId);

                p = tb.get_Parameter(
                    BuiltInParameter.SHEET_NUMBER);

                Debug.Assert(null != p,
                    "expected valid sheet number");

                var s_sheet_number = p.AsString();

                p = tb.get_Parameter(
                    BuiltInParameter.PROJECT_AUTHOR);

                Debug.Assert(null != p,
                    "expected valid project author");

                var s_project_author = p.AsValueString();

                p = tb.get_Parameter(
                    BuiltInParameter.CLIENT_NAME);

                Debug.Assert(null != p,
                    "expected valid client name");

                var s_client_name = p.AsValueString();

                Debug.Print(
                    "Title block {0} <{1}> of type {2} <{3}>: "
                    + "{4} project author {5} for client {6}",
                    tb.Name, tb.Id.IntegerValue,
                    type.Name, typeId.IntegerValue,
                    s_sheet_number, s_project_author,
                    s_client_name);
            }
        }

        /// Return a string value for the specified
        /// built-in parameter if it is available on
        /// the given element, else an empty string.
        private string GetParameterValueString(
            Element e,
            BuiltInParameter bip)
        {
            var p = e.get_Parameter(bip);

            var s = string.Empty;

            if (null != p)
            {
                switch (p.StorageType)
                {
                    case StorageType.Integer:
                        s = p.AsInteger().ToString();
                        break;

                    case StorageType.ElementId:
                        s = p.AsElementId().IntegerValue.ToString();
                        break;

                    case StorageType.Double:
                        s = Util.RealString(p.AsDouble());
                        break;

                    case StorageType.String:
                        s = $"{p.AsValueString()} ({Util.RealString(p.AsDouble())})";
                        break;

                    default:
                        s = "";
                        break;
                }

                s = $", {bip}={s}";
            }

            return s;
        }
    }
}