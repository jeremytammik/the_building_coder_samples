#region Header
//
// CmdSetGridEndpoint.cs - move selected grid endpoints in Y direction using SetCurveInView
//
// Copyright (C) 2018-2019 by Ryuji Ogasawara and Jeremy Tammik, Autodesk Inc. All rights reserved.
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
using System;
using System.Collections.Generic;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdSetGridEndpoint : IExternalCommand
  {
    /// <summary>
    /// Align the given grid horizontally or vertically 
    /// if it is very slightly off axis, by Fair59 in
    /// https://forums.autodesk.com/t5/revit-api-forum/grids-off-axis/m-p/7129065
    /// </summary>
    void AlignOffAxisGrid(
      Grid grid )
    {
      //Grid grid = doc.GetElement( 
      //  sel.GetElementIds().FirstOrDefault() ) as Grid;

      Document doc = grid.Document;

      XYZ direction = grid.Curve
        .GetEndPoint( 1 )
        .Subtract( grid.Curve.GetEndPoint( 0 ) )
        .Normalize();

      double distance2hor = direction.DotProduct( XYZ.BasisY );
      double distance2vert = direction.DotProduct( XYZ.BasisX );
      double angle = 0;

      // Maybe use another criterium then <0.0001

      double max_distance = 0.0001;

      if( Math.Abs( distance2hor ) < max_distance )
      {
        XYZ vector = direction.X < 0 
          ? direction.Negate() 
          : direction;

        angle = Math.Asin( -vector.Y );
      }

      if( Math.Abs( distance2vert ) < max_distance )
      {
        XYZ vector = direction.Y < 0 
          ? direction.Negate() 
          : direction;

        angle = Math.Asin( vector.X );
      }

      if( angle.CompareTo( 0 ) != 0 )
      {
        using( Transaction t = new Transaction( doc ) )
        {
          t.Start( "correctGrid" );

          ElementTransformUtils.RotateElement( doc, 
            grid.Id, 
            Line.CreateBound( grid.Curve.GetEndPoint( 0 ), 
              grid.Curve.GetEndPoint( 0 ).Add( XYZ.BasisZ ) ), 
            angle );

          t.Commit();
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
