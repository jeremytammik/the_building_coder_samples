#region Header
//
// CmdNewExtrusionRoof.cs - create a strangely stair shaped new extrusion roof
//
// Copyright (C) 2014 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
          new Autodesk.Revit.DB.Plane( vx, vy,
            origin ) );

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
