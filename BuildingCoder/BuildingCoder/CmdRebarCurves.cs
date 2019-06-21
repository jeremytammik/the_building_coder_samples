#region Header
//
// CmdRebarCurves.cs - Retrieve and duplicate all rebar centreline curves with model curves
//
// Copyright (C) 2010-2019 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using Autodesk.Revit.DB.Structure;
#endregion // Namespaces

namespace BuildingCoder
{
  // I’m using a filter which will give me all rebar 
  // elements.For each rear we will calculate for each 
  // bar in set it’s curves using Rebar.GetCenterlineCurves.
  // In case of shape driven, we will also move the curves 
  // for the bar at position `i` to their real position.

  [Transaction( TransactionMode.ReadOnly )]
  class CmdRebarCurves : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      FilteredElementCollector rebars 
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Rebar ) );

      IList<Curve> curves = new List<Curve>(); // collect all the curves.

      foreach( Rebar rebar in rebars )
      {
        int n = rebar.NumberOfBarPositions;

        for( int i = 0; i < n; ++i )
        {
          // Retrieve the curves of i'th bar in the set.
          // In case of shape driven rebar, they will be 
          // positioned at the location of the first bar 
          // in set.

          IList<Curve> centerlineCurves 
            = rebar.GetCenterlineCurves( 
              true, false, false, 
              MultiplanarOption.IncludeAllMultiplanarCurves, 
              i );

          // Move the curves to their position.

          if( rebar.IsRebarShapeDriven() )
          {
            RebarShapeDrivenAccessor accessor 
              = rebar.GetShapeDrivenAccessor();

            Transform trf = accessor
              .GetBarPositionTransform( i );

            foreach( Curve c in centerlineCurves )
            {
              curves.Add( c.CreateTransformed( trf ) );
            }
          }
          else
          {
            // This is a Free Form Rebar

            foreach( Curve c in centerlineCurves )
              curves.Add( c );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
