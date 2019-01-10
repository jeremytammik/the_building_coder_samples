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
    /// Resize ducts to ensure that branch ducts are no 
    /// larger than the main duct they are tapping into.
    /// </summary>
    void DuctResize( Document doc )
    {
      Parameter ductHeight;

      double updatedHeight = 0;

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
            double largestConnector = 0.0;
            double previous = 0.0;
            double cnnctrDim = 0.0;

            ConnectorSet dctCnnctrs = d.ConnectorManager.Connectors;

            int nDCs = dctCnnctrs.Size;
            if( nDCs < 3 )
            {
              // do nothing
            }
            else
            {
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
                  foreach( Connector cd in taps )
                  {
                    ConnectorProfileType cShape = cd.Shape;
                    string shapeType = cShape.ToString();

                    if( shapeType.Equals( "Round" ) )
                    {
                      cnnctrDim = cd.Radius * 2.0;
                    }
                    if( shapeType.Equals( "Rectangular" ) 
                      || shapeType.Equals( "Oval" ) )
                    {
                      cnnctrDim = cd.Height;
                    }
                  }

                  if( cnnctrDim >= previous )
                  {
                    largestConnector = cnnctrDim;
                    previous = largestConnector;
                  }
                  else
                  {
                    largestConnector = previous;
                  }
                }
              }

              try
              {
                if( largestConnector >= d.Height )
                {
                  updatedHeight = largestConnector 
                    + twoInches;

                  ++i;
                }
                else
                {
                  updatedHeight = d.Height;
                }
              }
              catch
              {
                if( largestConnector >= d.Diameter )
                {
                  updatedHeight = largestConnector 
                    + twoInches;

                  ++i;
                }
                else
                {
                  updatedHeight = d.Diameter;
                }
              }

              //try
              //{
              //  ductHeight = d.get_Parameter( bipHeight );
              //  ductHeight.Set( updatedHeight );
              //}
              //catch( NullReferenceException )
              //{
              //  ductHeight = d.get_Parameter( bipDiameter );
              //  ductHeight.Set( updatedHeight );
              //}

              ductHeight = d.get_Parameter( bipHeight ) 
                ?? d.get_Parameter( bipDiameter );

              double oldHeight = ductHeight.AsDouble();

              if(!Util.IsEqual(oldHeight,updatedHeight))
              { 
              ductHeight.Set( updatedHeight );
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
            int n = ( ductCollector as ICollection<Element> ).Count;
            taskDialog.MainContent = i + " out of " 
              + n.ToString() + " ducts will be re-sized"
              + "\n\nClick [OK] to Commit or [Cancel] "
              + "to Roll back the transaction.";

            buttons = TaskDialogCommonButtons.Ok
              | TaskDialogCommonButtons.Cancel;
          }
          else
          {
            taskDialog.MainContent 
              = "None of the ducts need to be re-sized"
              + "\n\nClick [OK] to Commit or [Cancel] "
              + "to Roll back the transaction.";

            buttons = TaskDialogCommonButtons.Ok;
          }

          taskDialog.CommonButtons = buttons;

          if( 0 < i && TaskDialogResult.Ok == taskDialog.Show() )
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
