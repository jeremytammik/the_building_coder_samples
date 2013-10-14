#region Header
//
// CmdParameterUnitConverter.cs - test ParameterUnitConverter on all floating point valued parameters on a selected element
//
// Copyright (C) 2011 by Victor Chekalin and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdParameterUnitConverter : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Reference r;

      try
      {
        r = uidoc.Selection.PickObject(
          ObjectType.Element );
      }
      catch( OperationCanceledException )
      {
        return Result.Cancelled;
      }

      Element e = doc.GetElement( r.ElementId );

      foreach( Parameter p in e.Parameters )
      {
        if( StorageType.Double == p.StorageType )
        {
          try
          {
            Debug.Print( 
              "Parameter name: {0}\tParameter value (imperial): {1}\t" 
              + "Parameter unit value: {2}\tParameter AsValueString: {3}",
              p.Definition.Name,
              p.AsDouble(),
              p.AsProjectUnitTypeDouble(),
              p.AsValueString() );
          }
          catch( Exception ex )
          {
            Debug.Print( 
              "Parameter name: {0}\tException: {1}", 
              p.Definition.Name, ex.Message );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
