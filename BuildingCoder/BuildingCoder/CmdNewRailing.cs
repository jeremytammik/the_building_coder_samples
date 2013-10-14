#region Header
//
// CmdNewRailing.cs - insert a new railing instance,
// in response to queries from Berria at
// http://thebuildingcoder.typepad.com/blog/2009/02/list-railing-types.html#comments
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Currently, it is not possible to create a new railing instance:
  /// http://thebuildingcoder.typepad.com/blog/2009/02/list-railing-types.html#comments
  /// SPR #134260 [API - New Element Creation: Railing]
  /// </summary>
  [Transaction( TransactionMode.Automatic )]
  class CmdNewRailing : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector levels = Util.GetElementsOfType(
        doc, typeof( Level ), BuiltInCategory.OST_Levels );

      Level level = levels.FirstElement() as Level;

      if( null == level )
      {
        message = "No level found.";
        return Result.Failed;
      }

      // get symbol to use:

      BuiltInCategory bic;
      Type t;

      // this retrieves the railing baluster symbols
      // but they cannot be used to create a railing:

      bic = BuiltInCategory.OST_StairsRailingBaluster;
      t = typeof( FamilySymbol );

      // this retrieves all railing symbols,
      // but they are just Symbol instances,
      // not FamilySymbol ones:

      bic = BuiltInCategory.OST_StairsRailing;
      t = typeof( ElementType );

      FilteredElementCollector symbols
        = Util.GetElementsOfType( doc, t, bic );

      FamilySymbol sym = null;

      foreach( ElementType s in symbols )
      {
        FamilySymbol fs = s as FamilySymbol;

        Debug.Print(
          "Family name={0}, symbol name={1},"
          + " category={2}",
          null == fs ? "<none>" : fs.Family.Name,
          s.Name,
          s.Category.Name );

        if( null == sym && s is ElementType )
        {
          // this does not work, of course:
          sym = s as FamilySymbol;
        }
      }
      if( null == sym )
      {
        message = "No railing family symbols found.";
        return Result.Failed;
      }
      XYZ p1 = new XYZ( 17, 0, 0 );
      XYZ p2 = new XYZ( 33, 0, 0 );
      Line line = Line.CreateBound( p1, p2 );
      // we need a FamilySymbol instance here, but only have a Symbol:

      FamilyInstance Railing1 
        = doc.Create.NewFamilyInstance( 
          line, sym, level, StructuralType.NonStructural );

      return Result.Succeeded;
    }
  }
}
