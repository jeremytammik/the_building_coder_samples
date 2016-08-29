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
    const string line_style_name = "_Solid-Red-1";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector graphic_styles
        = new FilteredElementCollector( doc )
          .OfClass( typeof( GraphicsStyle ) );

      int n1 = graphic_styles.Count<Element>();

      IEnumerable<Element> red_line_styles 
        = graphic_styles.Where<Element>( e 
          => e.Name.Contains( line_style_name ) );

      int n2 = red_line_styles.Count<Element>();

      if( 0 < n2 )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Delete Line Styles" );

          doc.Delete( red_line_styles
            .Select<Element, ElementId>( e => e.Id )
            .ToArray<ElementId>() );

          tx.Commit();

          Util.InfoMsg( string.Format(
            "Deleted {0} {1} line style{2} "
            + "from {3} graohic styles.",
            n2, line_style_name, 
            Util.PluralSuffix( n2 ), n1 ) );
        }
      }
      return Result.Succeeded;
    }
  }
}
