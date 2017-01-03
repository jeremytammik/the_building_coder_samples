#region Header
//
// CmdNewExtrusionRoof.cs - create a strangely stair shaped new extrusion roof
//
// Copyright (C) 2014-2017 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewExtrusionRoof : IExternalCommand
  {
    #region Revit Online Help sample code from the section on Roofs
    void f( Document doc )
    {
      // Before invoking this sample, select some walls 
      // to add a roof over. Make sure there is a level 
      // named "Roof" in the document.

      Level level
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Level ) )
          .Where<Element>( e =>
            !string.IsNullOrEmpty( e.Name )
            && e.Name.Equals( "Roof" ) )
          .FirstOrDefault<Element>() as Level;

      RoofType roofType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( RoofType ) )
          .FirstOrDefault<Element>() as RoofType;

      // Get the handle of the application
      Application application = doc.Application;

      // Define the footprint for the roof based on user selection
      CurveArray footprint = application.Create
        .NewCurveArray();

      UIDocument uidoc = new UIDocument( doc );

      ICollection<ElementId> selectedIds
        = uidoc.Selection.GetElementIds();

      if( selectedIds.Count != 0 )
      {
        foreach( ElementId id in selectedIds )
        {
          Element element = doc.GetElement( id );
          Wall wall = element as Wall;
          if( wall != null )
          {
            LocationCurve wallCurve = wall.Location as LocationCurve;
            footprint.Append( wallCurve.Curve );
            continue;
          }

          ModelCurve modelCurve = element as ModelCurve;
          if( modelCurve != null )
          {
            footprint.Append( modelCurve.GeometryCurve );
          }
        }
      }
      else
      {
        throw new Exception(
          "Please select a curve loop, wall loop or "
          + "combination of walls and curves to "
          + "create a footprint roof." );
      }

      ModelCurveArray footPrintToModelCurveMapping
        = new ModelCurveArray();

      FootPrintRoof footprintRoof
        = doc.Create.NewFootPrintRoof(
          footprint, level, roofType,
          out footPrintToModelCurveMapping );

      ModelCurveArrayIterator iterator
        = footPrintToModelCurveMapping.ForwardIterator();

      iterator.Reset();
      while( iterator.MoveNext() )
      {
        ModelCurve modelCurve = iterator.Current as ModelCurve;
        footprintRoof.set_DefinesSlope( modelCurve, true );
        footprintRoof.set_SlopeAngle( modelCurve, 0.5 );
      }
    }
    #endregion // Revit Online Help sample code from the section on Roofs

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "NewExtrusionRoof" );

        RoofType fs
          = new FilteredElementCollector( doc )
            .OfClass( typeof( RoofType ) )
            .Cast<RoofType>()
            .FirstOrDefault<RoofType>( a => null != a );

        Level lvl
          = new FilteredElementCollector( doc )
            .OfClass( typeof( Level ) )
            .Cast<Level>()
            .FirstOrDefault<Level>( a => null != a );

        double x = 1;

        XYZ origin = new XYZ( x, 0, 0 );
        XYZ vx = XYZ.BasisY;
        XYZ vy = XYZ.BasisZ;

        SketchPlane sp = SketchPlane.Create( doc,
          //new Autodesk.Revit.DB.Plane( vx, vy, origin ) // 2016
          Plane.CreateByOriginAndBasis( origin, vx, vy ) );// 2017

        CurveArray ca = new CurveArray();

        XYZ[] pts = new XYZ[] {
          new XYZ( x, 1, 0 ), 
          new XYZ( x, 1, 1 ), 
          new XYZ( x, 2, 1 ), 
          new XYZ( x, 2, 2 ), 
          new XYZ( x, 3, 2 ), 
          new XYZ( x, 3, 3 ), 
          new XYZ( x, 4, 3 ), 
          new XYZ( x, 4, 4 ) };

        int n = pts.Length;

        for( int i = 1; i < n; ++i )
        {
          ca.Append( Line.CreateBound(
            pts[i - 1], pts[i] ) );
        }

        doc.Create.NewModelCurveArray( ca, sp );

        View v = doc.ActiveView;

        ReferencePlane rp
          = doc.Create.NewReferencePlane2(
            origin, origin + vx, origin + vy, v );

        rp.Name = "MyRoofPlane";

        ExtrusionRoof er
          = doc.Create.NewExtrusionRoof(
            ca, rp, lvl, fs, 0, 3 );

        Debug.Print( "Extrusion roof element id: "
          + er.Id.ToString() );

        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
