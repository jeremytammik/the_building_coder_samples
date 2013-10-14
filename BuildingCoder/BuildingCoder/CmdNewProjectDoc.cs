#region Header
//
// CmdNewProjectDoc.cs - create a new project document
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
using Autodesk.Revit.UI;//using Autodesk.Revit.Collections;
//
//
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewProjectDoc : IExternalCommand
  {
    const string _template_file_path
      = "C:/Documents and Settings/All Users"
      + "/Application Data/Autodesk/RAC 2010"
      + "/Metric Templates/DefaultMetric.rte";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Application app = commandData.Application.Application;

      Document doc = app.NewProjectDocument(
        _template_file_path );

      doc.SaveAs( "C:/tmp/new_project.rvt" );

      return Result.Failed;
    }
  }
}
