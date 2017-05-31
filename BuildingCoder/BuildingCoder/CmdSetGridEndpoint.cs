#region Header
//
// CmdSetGridEndpoint.cs - move selected grid endpoints in Y direction using SetCurveInView
//
// Copyright (C) 2017 by  Ryuji Ogasawara and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// Written by Ryuji Ogasawara.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdSetGridEndpoint : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;
      View view = doc.ActiveView;

      ISelectionFilter f
        = new JtElementsOfClassSelectionFilter<Grid>();

      Reference elemRef = sel.PickObject(
        ObjectType.Element, f, "Pick a grid" );

      Grid grid = doc.GetElement( elemRef ) as Grid;

      IList<Curve> gridCurves = grid.GetCurvesInView(
        DatumExtentType.Model, view );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Modify Grid Endpoints" );

        foreach( Curve c in gridCurves )
        {
          XYZ start = c.GetEndPoint( 0 );
          XYZ end = c.GetEndPoint( 1 );

          XYZ newStart = start + 10 * XYZ.BasisY;
          XYZ newEnd = end - 10 * XYZ.BasisY;

          Line newLine = Line.CreateBound( newStart, newEnd );

          grid.SetCurveInView(
            DatumExtentType.Model, view, newLine );
        }
        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
