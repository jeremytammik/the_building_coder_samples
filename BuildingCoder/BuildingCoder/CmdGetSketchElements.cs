#region Header
//
// CmdGetSketchElements.cs - retrieve sketch elements for a selected wall, floor, roof, filled region, etc.
//
// Copyright (C) 2010-2016 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdGetSketchElements : IExternalCommand
  {
    const string _caption = "Retrieve Sketch Elements";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;

      Reference r = sel.PickObject( ObjectType.Element,
        "Please pick an element" );

      // 'Autodesk.Revit.DB.Reference.Element' is
      // obsolete: Property will be removed. Use
      // Document.GetElement(Reference) instead.
      //Element e = r.Element; // 2011

      Element e = doc.GetElement( r ); // 2012

      Transaction tx = new Transaction( doc );

      tx.Start( _caption );

      ICollection<ElementId> ids = doc.Delete( e.Id );

      tx.RollBack();

      bool showOnlySketchElements = true;

      /*
      StringBuilder s = new StringBuilder(
        _caption
        + " for host element "
        + Util.ElementDescription( e )
        + ": " );

      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        if( !showOnlySketchElements
          || e is Sketch
          || e is SketchPlane )
        {
          s.Append( Util.ElementDescription( e ) + ", " );
        }
      }
      */

      List<Element> a = new List<Element>(
        ids.Select( id => doc.GetElement( id ) ) );

      string s = _caption
        + " for host element "
        + Util.ElementDescription( e )
        + ": ";

      s += string.Join( ", ",
        a.Where( e2 => !showOnlySketchElements
          || e2 is Sketch
          || e2 is SketchPlane )
        .Select( e2 => Util.ElementDescription( e2 ) )
        .ToArray() );

      Util.InfoMsg( s );

      return Result.Succeeded;
    }
  }
}
