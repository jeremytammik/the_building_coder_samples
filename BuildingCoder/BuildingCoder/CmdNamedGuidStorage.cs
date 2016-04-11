#region Header
//
// CmdNamedGuidStorage.cs - Test the named Guid storage class
//
// Copyright (C) 2015-2016 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNamedGuidStorage : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Result rslt = Result.Failed;

      string name = "TrackChanges_project_identifier";
      Guid named_guid;

      bool rc = JtNamedGuiStorage.Get( doc,
        name, out named_guid, false );

      if( rc )
      {
        Util.InfoMsg( string.Format(
          "This document already has a project "
          + "identifier: {0} = {1}",
          name, named_guid.ToString() ) );

        rslt = Result.Succeeded;
      }
      else
      {
        rc = JtNamedGuiStorage.Get( doc,
          name, out named_guid, true );

        if( rc )
        {
          Util.InfoMsg( string.Format(
            "Created a new project identifier "
            + "for this document: {0} = {1}",
            name, named_guid.ToString() ) );

          rslt = Result.Succeeded;
        }
        else
        {
          Util.ErrorMsg( "Something went wrong" );
        }
      }
      return rslt;
    }
  }
}
