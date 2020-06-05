#region Header
//
// CmdChangeElementColor.cs - Change element colour using OverrideGraphicSettings for active view
//
// Copyright (C) 2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdChangeElementColor : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      View view = doc.ActiveView;
      ElementId id;

      try
      {
        Selection sel = uidoc.Selection;
        Reference r = sel.PickObject(
          ObjectType.Element,
          "Pick element to change its colour" );
        id = r.ElementId;
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return Result.Cancelled;
      }

      Color color = new Color(
        (byte) 150, (byte) 200, (byte) 200 );

      OverrideGraphicSettings ogs = new OverrideGraphicSettings();
      ogs.SetProjectionLineColor( color );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Change Element Color" );
        doc.ActiveView.SetElementOverrides( id, ogs );
        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
