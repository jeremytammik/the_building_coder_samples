#region Header
//
// CmdCreateLineStyle.cs - create a new line style using NewSubcategory
//
// Copyright (C) 2016-2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdCreateLineStyle : IExternalCommand
  {
    /// <summary>
    /// Create a new line style using NewSubcategory
    /// </summary>
    void CreateLineStyle( Document doc )
    {
      // Use this to access the current document in a macro.
      //
      //Document doc = this.ActiveUIDocument.Document;

      // Find existing linestyle.  Can also opt to
      // create one with LinePatternElement.Create()

      FilteredElementCollector fec
        = new FilteredElementCollector( doc )
          .OfClass( typeof( LinePatternElement ) );

      LinePatternElement linePatternElem = fec
        .Cast<LinePatternElement>()
        .First<LinePatternElement>( linePattern
          => linePattern.Name == "Long Dash" );

      // The new linestyle will be a subcategory 
      // of the Lines category        

      Categories categories = doc.Settings.Categories;

      Category lineCat = categories.get_Item(
        BuiltInCategory.OST_Lines );

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Create LineStyle" );

        // Add the new linestyle 

        Category newLineStyleCat = categories
          .NewSubcategory( lineCat, "New LineStyle" );

        doc.Regenerate();

        // Set the linestyle properties 
        // (weight, color, pattern).

        newLineStyleCat.SetLineWeight( 8,
          GraphicsStyleType.Projection );

        newLineStyleCat.LineColor = new Color(
          0xFF, 0x00, 0x00 );

        newLineStyleCat.SetLinePatternId(
          linePatternElem.Id,
          GraphicsStyleType.Projection );

        t.Commit();
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      CreateLineStyle( doc );

      return Result.Succeeded;
    }
  }
}
