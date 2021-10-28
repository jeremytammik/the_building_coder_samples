#region Header

//
// CmdListRailings.cs - list all railing types,
// in response to queries from Berria at
// http://thebuildingcoder.typepad.com/blog/2009/02/inserting-a-column.html#comments
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

//using Autodesk.Revit.Enums;
//using Autodesk.Revit.Symbols;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListRailingTypes : IExternalCommand
    {
        /*
        public Result Execute2(
          ExternalCommandData commandData,
          ref string messages,
          ElementSet elements )
        {
          UIApplication app = commandData.Application;
          Document doc = app.ActiveUIDocument.Document;
          CreationFilter cf = app.Create.Filter;
    
          Filter f1 = cf.NewParameterFilter(
            BuiltInParameter.DESIGN_OPTION_PARAM,
            CriteriaFilterType.Equal, "Main Model" );
    
          Filter f2 = cf.NewTypeFilter( typeof( Wall ) );
          Filter f = cf.NewLogicAndFilter( f1, f2 );
    
          List<Element> a = new List<Element>();
    
          doc.get_Elements( f, a );
    
          Util.InfoMsg( "There are "
            + a.Count.ToString()
            + " main model wall elements" );
    
          return Result.Succeeded;
        }
        */

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            // this returns zero symbols:
            //BuiltInCategory bic = BuiltInCategory.OST_StairsRailing;
            //Filter f1 = cf.NewCategoryFilter( bic );
            //Filter f2 = cf.NewTypeFilter( typeof( FamilySymbol ) );
            //Filter f = cf.NewLogicAndFilter( f1, f2 );

            // this returns zero families:
            // we already know why, because family does not implement the Category property, c.f.
            // http://thebuildingcoder.typepad.com/blog/2009/01/family-category-and-filtering.html
            //Filter f1 = cf.NewCategoryFilter( bic );
            //Filter f2 = cf.NewTypeFilter( typeof( Family ) );
            //Filter f = cf.NewLogicAndFilter( f1, f2 );

            var bic
                = BuiltInCategory.OST_StairsRailingBaluster;

            var symbols = Util.GetElementsOfType(
                doc, typeof(FamilySymbol), bic).ToElements();

            var n = symbols.Count;

            Debug.Print("\n{0}"
                        + " OST_StairsRailingBaluster"
                        + " family symbol{1}:",
                n, Util.PluralSuffix(n));

            foreach (FamilySymbol s in symbols)
                Debug.Print(
                    "Family name={0}, symbol name={1}",
                    s.Family.Name, s.Name);

            bic = BuiltInCategory.OST_StairsRailing;

            symbols = Util.GetElementsOfType(
                doc, typeof(ElementType), bic).ToElements();

            n = symbols.Count;

            Debug.Print("\n{0}"
                        + " OST_StairsRailing symbol{1}:",
                n, Util.PluralSuffix(n));

            foreach (ElementType s in symbols)
            {
                var fs = s as FamilySymbol;

                Debug.Print(
                    "Family name={0}, symbol name={1}",
                    null == fs ? "<none>" : fs.Family.Name,
                    s.Name);
            }

            return Result.Failed;
        }
    }
}