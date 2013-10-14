#region Header
//
// CmdPlaceFamilyInstance.cs - call PromptForFamilyInstancePlacement
// to place family instances and use the DocumentChanged event to
// capture the newly added element ids
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
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
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdPlaceFamilyInstance : IExternalCommand
  {
    List<ElementId> _added_element_ids = new List<ElementId>();

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfCategory( BuiltInCategory.OST_Doors );
      collector.OfClass( typeof( FamilySymbol ) );

      FamilySymbol symbol = collector.FirstElement() as FamilySymbol;

      _added_element_ids.Clear();

      app.DocumentChanged
        += new EventHandler<DocumentChangedEventArgs>(
          OnDocumentChanged );

      uidoc.PromptForFamilyInstancePlacement( symbol );

      app.DocumentChanged
        -= new EventHandler<DocumentChangedEventArgs>(
          OnDocumentChanged );

      int n = _added_element_ids.Count;

      TaskDialog.Show(
        "Place Family Instance",
        string.Format(
          "{0} element{1} added.", n,
          ( ( 1 == n ) ? "" : "s" ) ) );

      return Result.Succeeded;
    }

    void OnDocumentChanged(
      object sender,
      DocumentChangedEventArgs e )
    {
      // this does not work, because the handler will
      // be called each time a new instance is added,
      // overwriting the previous ones recorded:

      //_added_element_ids = e.GetAddedElementIds();

      _added_element_ids.AddRange( e.GetAddedElementIds() );
    }
  }
}
