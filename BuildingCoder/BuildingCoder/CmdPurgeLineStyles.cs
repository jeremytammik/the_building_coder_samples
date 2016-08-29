#region Header
//
// CmdPurgeLineStyles.cs - purge specific line styles
//
// Copyright (C) 2010-2016 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdPurgeLineStyles : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector line_styles = new FilteredElementCollector( doc )
        .OfClass( typeof( GraphicsStyle ) )
        .OfCategory( BuiltInCategory.OST_Lines );

        //.ToElementIds();

      int n = line_styles.Count<Element>();

      IEnumerable<ElementId> ids = line_styles
        .Where<Element>( e => e.Name.Contains( "_Solid-Red-1" ) )
        .Select<Element, ElementId>( e => e.Id );

      int n2 = line_styles.Count<Element>();


      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Delete Line Styles" );

        doc.Delete( ids.ToArray<ElementId>() );

        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
