#region Header
//
// CmdMultistoryStairSubelements.cs - Access all subelements of all MultistoryStair instances
//
// Copyright (C) 2018 Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdMultistoryStairSubelements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Retrieve selected multistory stairs, or all 
      // such elements, if nothing is pre-selected:

      List<Element> msss = new List<Element>();

      if( !Util.GetSelectedElementsOrAll(
        msss, uidoc, typeof( MultistoryStairs ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.GetElementIds().Count )
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      foreach( MultistoryStairs mss in msss )
      {

      }
      return Result.Succeeded;
    }
  }
}
