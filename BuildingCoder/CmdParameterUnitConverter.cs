#region Header

//
// CmdParameterUnitConverter.cs - test ParameterUnitConverter on all floating point valued parameters on a selected element
//
// Copyright (C) 2011-2020 by Victor Chekalin and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

#endregion // Namespaces

namespace BuildingCoder
{
    #region Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021

#if USE_PRE_FORGE_UNIT_FUNCTIONALITY
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
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
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
#endif // USE_PRE_FORGE_UNIT_FUNCTIONALITY

    #endregion // Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021
}