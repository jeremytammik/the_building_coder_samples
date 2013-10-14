#region Header
//
// CmdWallFooting.cs - determine wall footing from wall.
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
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdWallFooting : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Wall wall = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "a wall", false ) 
          as Wall;

      if ( null == wall )
      {
        message 
          = "Please select a single wall element.";

        return Result.Failed;
      }

      ICollection<ElementId> delIds = null;

      using( Transaction t = new Transaction( doc ) )
      {
        try
        {
          t.Start( "Temporary Wall Deletion" );

          delIds = doc.Delete( wall.Id );

          t.RollBack();
        }
        catch ( Exception ex )
        {
          message = "Deletion failed: " + ex.Message;
          t.RollBack();
        }
      }

      ContFooting footing = null;

      foreach ( ElementId id in delIds )
      {
        footing = doc.GetElement( id ) as ContFooting;

        if( null != footing )
        {
          break;
        }
      }

      string s = Util.ElementDescription( wall );

      Util.InfoMsg( ( null == footing )
        ? string.Format( "No footing found for {0}.", s )
        : string.Format( "{0} has {1}.", s,
          Util.ElementDescription( footing ) ) );

      return Result.Succeeded;
    }
  }
}
