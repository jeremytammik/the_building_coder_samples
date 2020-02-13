#region Header
//
// CmdCoordsOfViewOnSheet.cs - retrieve coordinates of view on sheet
//
// Copyright (C) 2009-2020 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
  class CmdCoordsOfViewOnSheet : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref String message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      ViewSheet currentSheet
        = doc.ActiveView as ViewSheet;

      //foreach( View v in currentSheet.Views ) // 2014 warning	'Autodesk.Revit.DB.ViewSheet.Views' is obsolete.  Use GetAllPlacedViews() instead.

      foreach( ElementId id in currentSheet.GetAllPlacedViews() ) // 2015
      {
        View v = doc.GetElement( id ) as View;

        // the values returned here do not seem to
        // accurately reflect the positions of the
        // views on the sheet:

        BoundingBoxUV loc = v.Outline;

        Debug.Print(
          "Coordinates of {0} view '{1}': {2}",
          v.ViewType, v.Name,
          Util.PointString( loc.Min ) );
      }

      return Result.Failed;
    }
  }
}
