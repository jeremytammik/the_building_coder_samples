#region Header
//
// CmdUpdateReferencingSheet.cs - update 'Referencing Sheet' parameter displayed in section view header
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdUpdateReferencingSheet : IExternalCommand
  {
    void UpdateReferencingSheet(
      ViewSection selectedViewport )
    {
      BuiltInParameter bip
        = BuiltInParameter.VIEW_DISCIPLINE;

      Parameter discipline
        = selectedViewport.get_Parameter( bip );

      int disciplineNo = discipline.AsInteger();

      Document doc = selectedViewport.Document;

      Transaction transaction = new Transaction( doc );

      if( TransactionStatus.Started
        == transaction.Start( "Updating the model" ) )
      {
        //switch( disciplineNo )
        //{
        //  case 1:
        //    discipline.Set( 2 );
        //    break;
        //  default:
        //    discipline.Set( 1 );
        //    break;
        //}
        //discipline.Set( disciplineNo );

        discipline.Set( 1 == disciplineNo ? 2 : 1 );
        transaction.Commit();
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Document doc = commandData.Application
        .ActiveUIDocument.Document;

      ViewSection selectedViewport
        = doc.ActiveView as ViewSection;

      if( null == selectedViewport )
      {
        message = "Please run this command "
          + " in a section view";

        return Result.Failed;
      }
      else
      {
        UpdateReferencingSheet( selectedViewport );
        return Result.Succeeded;
      }
    }
  }
}
