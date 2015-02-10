#region Header
//
// CmdPlaceFamilyInstance.cs - call PromptForFamilyInstancePlacement
// to place family instances and use the DocumentChanged event to
// capture the newly added element ids
//
// Copyright (C) 2010-2015 by Jeremy Tammik,
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
using ComponentManager = Autodesk.Windows.ComponentManager;
using IWin32Window = System.Windows.Forms.IWin32Window;
using Keys = System.Windows.Forms.Keys;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdPlaceFamilyInstance : IExternalCommand
  {
    /// <summary>
    /// Set this flag to true to abort after 
    /// placing the first instance.
    /// </summary>
    static bool _place_one_single_instance_then_abort
      = true;

    /// <summary>
    /// Send messages to main Revit application window.
    /// </summary>
    IWin32Window _revit_window;

    List<ElementId> _added_element_ids
      = new List<ElementId>();

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      _revit_window
        = new JtWindowHandle(
          ComponentManager.ApplicationWindow );

      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfCategory( BuiltInCategory.OST_Doors );
      collector.OfClass( typeof( FamilySymbol ) );

      FamilySymbol symbol = collector.FirstElement()
        as FamilySymbol;

      _added_element_ids.Clear();

      app.DocumentChanged
        += new EventHandler<DocumentChangedEventArgs>(
          OnDocumentChanged );

      //PromptForFamilyInstancePlacementOptions opt 
      //  = new PromptForFamilyInstancePlacementOptions();

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
      ICollection<ElementId> idsAdded
        = e.GetAddedElementIds();

      int n = idsAdded.Count;

      Debug.Print( "{0} id{1} added.",
        n, Util.PluralSuffix( n ) );

      // This does not work, because the handler will
      // be called each time a new instance is added,
      // overwriting the previous ones recorded:

      //_added_element_ids = e.GetAddedElementIds();

      _added_element_ids.AddRange( idsAdded );

      if( _place_one_single_instance_then_abort
        && 0 < n )
      {
        // Why do we send the WM_KEYDOWN message twice?
        // I tried sending it once only, and that does
        // not work. Maybe the proper thing to do would 
        // be something like the Press.OneKey method...
        // nope, that did not work.

        //Press.OneKey( _revit_window.Handle,
        //  (char) Keys.Escape );

        Press.PostMessage( _revit_window.Handle,
          (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
          (uint) Keys.Escape, 0 );

        Press.PostMessage( _revit_window.Handle,
          (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
          (uint) Keys.Escape, 0 );
      }
    }
  }
}
