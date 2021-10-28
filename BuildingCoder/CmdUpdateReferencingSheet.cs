#region Header

//
// CmdUpdateReferencingSheet.cs - update 'Referencing Sheet' parameter displayed in section view header
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdUpdateReferencingSheet : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var doc = commandData.Application
                .ActiveUIDocument.Document;

            if (doc.ActiveView is not ViewSection selectedViewport)
            {
                message = "Please run this command "
                          + " in a section view";

                return Result.Failed;
            }

            UpdateReferencingSheet(selectedViewport);
            return Result.Succeeded;
        }

        private void UpdateReferencingSheet(
            ViewSection selectedViewport)
        {
            var bip
                = BuiltInParameter.VIEW_DISCIPLINE;

            var discipline
                = selectedViewport.get_Parameter(bip);

            var disciplineNo = discipline.AsInteger();

            var doc = selectedViewport.Document;

            var transaction = new Transaction(doc);

            if (TransactionStatus.Started
                == transaction.Start("Updating the model"))
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

                discipline.Set(1 == disciplineNo ? 2 : 1);
                transaction.Commit();
            }
        }
    }
}