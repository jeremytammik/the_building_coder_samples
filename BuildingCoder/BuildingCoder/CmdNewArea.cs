#region Header
//
// CmdNewArea.cs - create a new area element
//
// Copyright (C) 2009-2019 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewArea : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Result rc = Result.Failed;

      ViewPlan view = commandData.View as ViewPlan;

      if( null == view
        || view.ViewType != ViewType.AreaPlan )
      {
        message = "Please run this command in an area plan view.";
        return rc;
      }

      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Element room = Util.GetSingleSelectedElement( uidoc );

      if( null == room || !(room is Room) )
      {
        room = Util.SelectSingleElement( uidoc, "a room" );
      }

      if( null == room || !( room is Room ) )
      {
        message = "Please select a single room element.";
      }
      else
      {
        using ( Transaction t = new Transaction( doc ) )
        {
          t.Start( "Create New Area" );

          Location loc = room.Location;
          LocationPoint lp = loc as LocationPoint;
          XYZ p = lp.Point;
          UV q = new UV( p.X, p.Y );
          Area area = doc.Create.NewArea( view, q );
          rc = Result.Succeeded;
          t.Commit();
        }
      }
      return rc;
    }
  }
}
