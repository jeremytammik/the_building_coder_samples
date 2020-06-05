#region Header
//
// CmdChangeElementColor.cs - Change element colour using OverrideGraphicSettings for active view
//
// Also change its category's material to a random material
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
using System;
using System.Collections.Generic;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdChangeElementColor : IExternalCommand
  {
    void ChangeElementColor( Document doc, ElementId id )
    {
      Color color = new Color(
        (byte) 200, (byte) 100, (byte) 100 );

      OverrideGraphicSettings ogs = new OverrideGraphicSettings();
      ogs.SetProjectionLineColor( color );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Change Element Color" );
        doc.ActiveView.SetElementOverrides( id, ogs );
        tx.Commit();
      }
    }

    void ChangeElementMaterial( Document doc, ElementId id )
    {
      List<Material> materials = new List<Material>(
        new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .OfClass( typeof( Material ) )
          .ToElements()
          .Cast<Material>() );

      Random r = new Random();
      int i = r.Next( materials.Count );

      Element e = doc.GetElement( id );

      if( null != e.Category )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Change Element Material" );
          e.Category.Material = materials[ i ];
          tx.Commit();
        }
      }
    }

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

      ChangeElementColor( doc, id );

      ChangeElementMaterial( doc, id );

      return Result.Succeeded;
    }
  }
}
