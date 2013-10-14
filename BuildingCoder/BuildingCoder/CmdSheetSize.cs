#region Header
//
// CmdSheetSize.cs - list title block element types and title block and view sheet instances and sizes
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdSheetSize : IExternalCommand
  {
    ///
    /// Return a string value for the specified
    /// built-in parameter if it is available on
    /// the given element, else an empty string.
    ///
    string GetParameterValueString(
      Element e,
      BuiltInParameter bip )
    {
      Parameter p = e.get_Parameter( bip );

      string s = string.Empty;

      if( null != p )
      {
        switch( p.StorageType )
        {
          case StorageType.Integer:
            s = p.AsInteger().ToString();
            break;

          case StorageType.ElementId:
            s = p.AsElementId().IntegerValue.ToString();
            break;

          case StorageType.Double:
            s = Util.RealString( p.AsDouble() );
            break;

          case StorageType.String:
            s = string.Format( "{0} ({1})",
              p.AsValueString(),
              Util.RealString( p.AsDouble() ) );
            break;

          default: s = "";
            break;
        }
        s = ", " + bip.ToString() + "=" + s;
      }
      return s;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref String message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector a;
      Parameter p;
      string s;

      // Using the obsolete deprecated TitleBlocks property

      FamilySymbolSet titleBlocks = doc.TitleBlocks;

      int n = titleBlocks.Size;

      Debug.Print(
        "{0} title block element type{1} listed "
        + "in doc.TitleBlocks collection{2}",
        n,
        ( 1 == n ? "" : "s" ),
        ( 0 == n ? "." : ":" ) );

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

      // Using this filter returns the same elements
      // as the doc.TitleBlocks collection:

      a = new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_TitleBlocks )
        .OfClass( typeof( FamilySymbol ) );

      Debug.Print( "{0} title block element type{1} "
        + "retrieved by filtered element collector{2}",
        n,
        ( 1 == n ? "" : "s" ),
        ( 0 == n ? "." : ":" ) );

      foreach( FamilySymbol symbol in a )
      {
        Debug.Print(
          "Title block element type {0} {1}",
          symbol.Name, symbol.Id.IntegerValue );
      }

      // Retrieve the title block instances:

      a = new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_TitleBlocks )
        .OfClass( typeof( FamilyInstance ) );

      Debug.Print( "Title block instances:" );

      foreach( FamilyInstance e in a )
      {
        p = e.get_Parameter(
          BuiltInParameter.SHEET_NUMBER );

        Debug.Assert( null != p,
          "expected valid sheet number" );

        string sheet_number = p.AsString();

        p = e.get_Parameter(
          BuiltInParameter.SHEET_WIDTH );

        Debug.Assert( null != p,
          "expected valid sheet width" );

        string swidth = p.AsValueString();
        double width = p.AsDouble();

        p = e.get_Parameter(
          BuiltInParameter.SHEET_HEIGHT );

        Debug.Assert( null != p,
          "expected valid sheet height" );

        string sheight = p.AsValueString();
        double height = p.AsDouble();

        ElementId typeId = e.GetTypeId();
        Element type = doc.GetElement( typeId );

        Debug.Print(
          "Sheet number {0} size is {1} x {2} "
          + "({3} x {4}), id {5}, type {6} {7}",
          sheet_number, swidth, sheight,
          Util.RealString( width ),
          Util.RealString( height ),
          e.Id.IntegerValue,
          type.Name, typeId.IntegerValue );
      }

      // Retrieve the view sheet instances:

      a = new FilteredElementCollector( doc )
        .OfClass( typeof( ViewSheet ) );

      Debug.Print( "View sheet instances:" );

      foreach( ViewSheet vs in a )
      {
        string number = vs.SheetNumber;
        Debug.Print(
          "View sheet name {0} number {1} id {2}",
          vs.Name, vs.SheetNumber,
          vs.Id.IntegerValue );
      }
      return Result.Succeeded;
    }
  }
}
