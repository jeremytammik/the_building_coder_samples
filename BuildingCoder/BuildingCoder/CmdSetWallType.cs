#region Header
//
// CmdSetWallType.cs - Set the wall type of a selected wall
//
// Copyright (C) 2015 by Paolo Serra and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdSetWallType : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Pick a wall top determine its element id

      Element wall_picked = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "wall", true );

      // Grab wall element id

      ElementId wall_id = new ElementId( 25122 );
      wall_id = wall_picked.Id;

      List<ElementId> ids = new List<ElementId>( 1 );
      ids.Add( wall_id );

      // Now for the automatic stuff

      // Retrieve a wall from the database by element id.

      Wall wall
        = new FilteredElementCollector( doc, ids )
          .OfClass( typeof( Wall ) )
          .FirstElement() as Wall;

      // Retrtieve the first wall type found

      WallType wallType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( WallType ) )
          .Cast<WallType>()
          .FirstOrDefault<WallType>();

      // Change the wall type

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Change Wall Type" );
        wall.WallType = wallType;
        t.Commit();
      }
      return Result.Succeeded;
    }
  }
}
