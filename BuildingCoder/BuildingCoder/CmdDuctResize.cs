#region Header
//
// CmdDuctResize.cs - Ensure that branch ducts are no larger than the main duct they are tapping into
//
// Copyright (C) 2019 by Jared Wilson and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Based on code by Jared Wilson shared in case 14918470 [Find all ducts that have been tapped into]
  /// https://forums.autodesk.com/t5/revit-api-forum/find-all-ducts-that-have-been-tapped-into/m-p/8485269
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  class CmdDuctResize : IExternalCommand
  {
    const BuiltInParameter bipDiameter
      = BuiltInParameter.RBS_CURVE_DIAMETER_PARAM;

    const BuiltInParameter bipHeight
       = BuiltInParameter.RBS_CURVE_HEIGHT_PARAM;

    //double twoInches = UnitUtils.Convert( 2.0,
    //  DisplayUnitType.DUT_DECIMAL_INCHES,
    //  DisplayUnitType.DUT_DECIMAL_FEET );

    const double twoInches = 1.0 / 6.0; // two twelfths of a foot is a sixth

    /// <summary>
    /// Return dimension for this duct:
    /// diameter if round, else height.
    /// </summary>
    static double GetDuctDim( Duct d )
    {
      ConnectorProfileType shape = d.DuctType.Shape;

      return ConnectorProfileType.Round == shape
        ? d.Diameter
        : d.Height;
    }

    /// <summary>
    /// Return dimension for this connector:
    /// diameter if round, else height.
    /// </summary>
    static double GetConnectorDim( Connector c )
    {
      ConnectorProfileType shape = c.Shape;

      return ConnectorProfileType.Round == shape
        ? 2 * c.Radius
        : c.Height;
    }

    /// <summary>
    /// Resize ducts to ensure that branch ducts are no 
    /// larger than the main duct they are tapping into.
    /// </summary>
    void DuctResize( Document doc )
    {
      FilteredElementCollector ductCollector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Duct ) );

      using( Transaction transaction = new Transaction( doc ) )
      {
        if( transaction.Start( "Resize Ducts for Taps" )
          == TransactionStatus.Started )
        {
          int i = 0;
          foreach( Duct d in ductCollector )
          {
            ConnectorSet dctCnnctrs = d.ConnectorManager.Connectors;

            int nDCs = dctCnnctrs.Size;
            if( nDCs < 3 )
            {
              // do nothing
            }
            else
            {
              double ductDim = GetDuctDim( d );
              double largestConnector = 0.0;

              foreach( Connector c in dctCnnctrs )
              {
                if( c.ConnectorType.ToString().Equals( "End" ) )
                {
                  // Do nothing because I am not 
                  // interested in the "End" Connectors
                }
                else
                {
                  ConnectorSet taps = c.AllRefs;

                  double maxTapDim = 0.0;

                  foreach( Connector cd in taps )
                  {
                    double tapDim = GetConnectorDim( cd );

                    if( maxTapDim < tapDim )
                    {
                      maxTapDim = tapDim;
                    }
                  }

                  if( largestConnector < maxTapDim )
                  {
                    largestConnector = maxTapDim;
                  }
                }
              }

              if( largestConnector > ductDim )
              {
                double updatedHeight = largestConnector
                  + twoInches;

                Parameter ductHeight 
                  = d.get_Parameter( bipHeight )
                  ?? d.get_Parameter( bipDiameter );

                double oldHeight = ductHeight.AsDouble();

                if( !Util.IsEqual( oldHeight, updatedHeight ) )
                {
                  ductHeight.Set( updatedHeight );

                  ++i;
                }
              }
            }
          }

          // Ask the end user whether the 
          // changes are to be committed or not

          TaskDialog taskDialog = new TaskDialog(
            "Resize Ducts" );

          TaskDialogCommonButtons buttons;

          if( 0 < i )
          {
            int n = ductCollector.GetElementCount();

            taskDialog.MainContent = i + " out of "
              + n.ToString() + " ducts will be re-sized."
              + "\n\nClick [OK] to Commit or [Cancel] "
              + "to Roll back the transaction.";

            buttons = TaskDialogCommonButtons.Ok
              | TaskDialogCommonButtons.Cancel;
          }
          else
          {
            taskDialog.MainContent
              = "None of the ducts need to be re-sized.";

            buttons = TaskDialogCommonButtons.Ok;
          }

          taskDialog.CommonButtons = buttons;

          if( TaskDialogResult.Ok == taskDialog.Show() && 0 < i )
          {
            // For many various reasons, a transaction may not be committed
            // if the changes made during the transaction do not result a valid model.
            // If committing a transaction fails or is canceled by the end user,
            // the resulting status would be RolledBack instead of Committed.

            if( TransactionStatus.Committed != transaction.Commit() )
            {
              TaskDialog.Show( "Failure",
                "Transaction could not be committed" );
            }
          }

          // No need to roll back, just do not call Commit

          //else
          //{
          //  transaction.RollBack();
          //}
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

      DuctResize( doc );

      return Result.Succeeded;
    }
  }
}
