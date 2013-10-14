#region Header
//
// CmdNewLightingFixture.cs - insert new lighting fixture family instance
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewLightingFixture : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // get a lighting fixture family symbol:

      FilteredElementCollector symbols
        = Util.GetElementsOfType( doc,
          typeof( FamilySymbol ),
          BuiltInCategory.OST_LightingFixtures );

      FamilySymbol sym = symbols.FirstElement()
        as FamilySymbol;

      if( null == sym )
      {
        message = "No lighting fixture symbol found.";
        return Result.Failed;
      }

      // pick the ceiling:


#if _2010
      uidoc.Selection.StatusbarTip
        = "Please select ceiling to host lighting fixture";

      uidoc.Selection.PickOne();

      Element ceiling = null;

      foreach( Element elem in uidoc.Selection.Elements )
      {
        ceiling = elem as Element;
        break;
      }
#endif // _2010

      Reference r = uidoc.Selection.PickObject( ObjectType.Element,
        "Please select ceiling to host lighting fixture" );

      if( null == r )
      {
        message = "Nothing selected.";
        return Result.Failed;
      }

      // 'Autodesk.Revit.DB.Reference.Element' is
      // obsolete: Property will be removed. Use
      // Document.GetElement(Reference) instead.
      //Element ceiling = r.Element; // 2011

      Element ceiling = doc.GetElement( r ) as Wall; // 2012

      // get the level 1:

      Level level = Util.GetFirstElementOfTypeNamed(
        doc, typeof( Level ), "Level 1" ) as Level;

      if( null == level )
      {
        message = "Level 1 not found.";
        return Result.Failed;
      }

      // create the family instance:

      XYZ p = app.Create.NewXYZ( -43, 28, 0 );

      FamilyInstance instLight
        = doc.Create.NewFamilyInstance(
          p, sym, ceiling, level,
          StructuralType.NonStructural );

      return Result.Succeeded;
    }
  }
}
