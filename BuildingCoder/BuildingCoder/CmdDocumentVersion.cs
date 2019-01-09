#region Header
//
// CmdDocumentVersion.cs - list DocumentVersion data, i.e. document GUID and save count
//
// Copyright (C) 2014-2019 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdDocumentVersion : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData revit,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = revit.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      string path = doc.PathName;

      BasicFileInfo info = BasicFileInfo.Extract(
        path );

      DocumentVersion v = info.GetDocumentVersion();

      int n = v.NumberOfSaves;

      Util.InfoMsg( string.Format(
        "Document '{0}' has GUID {1} and {2} save{3}.",
        path, v.VersionGUID, n,
        Util.PluralSuffix( n ) ) );

      return Result.Succeeded;
    }
  }
}
