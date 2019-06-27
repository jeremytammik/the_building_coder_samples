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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System.Diagnostics;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdRebarCurves : IExternalCommand
  {
    static IList<Curve> GetRebarCurves( Document doc )
    {
      // I’m using a filter which gives me all rebar 
      // elements. For each rebar, we calculate for each 
      // bar in set it’s curves using Rebar.GetCenterlineCurves.
      // In case of shape driven, we also move the curves for
      // the bar at position `i` to their real position.

      IList<Curve> curves = new List<Curve>();

      FilteredElementCollector rebars
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Rebar ) );

      int n, nElements = 0, nCurves = 0;

      foreach( Rebar rebar in rebars )
      {
        ++nElements;

        n = rebar.NumberOfBarPositions;

        nCurves += n;

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

      n = curves.Count;

      Debug.Print( "Processed {0} rebar element{1} "
        + "with {2} bar position{3}, extracted {4} "
        + "curve{5}",
        nElements, Util.PluralSuffix( nElements ),
        nCurves, Util.PluralSuffix( nCurves ),
        n, Util.PluralSuffix( n ) );

      return curves;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      IList<Curve> curves = GetRebarCurves( doc );

      return Result.Succeeded;
    }
  }
}
