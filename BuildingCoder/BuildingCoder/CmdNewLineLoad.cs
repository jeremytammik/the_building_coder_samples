#region Header
//
// CmdNewLineLoad.cs - create a new structural line load element
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion // Namespaces

// 1251154 [NewLineLoad]

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewLineLoad : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Debug.Assert( false, "this has not been tested yet in Revit 2011!" );

      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      Autodesk.Revit.Creation.Application ca
        = app.Application.Create;

      Autodesk.Revit.Creation.Document cd
        = doc.Create;

      // determine line load symbol to use:

      FilteredElementCollector symbols
        = new FilteredElementCollector( doc );

      symbols.OfClass( typeof( LineLoadType ) );

      LineLoadType loadSymbol
        = symbols.FirstElement() as LineLoadType;

      // sketch plane and arrays of forces and moments:

      Plane plane = ca.NewPlane( XYZ.BasisZ, XYZ.Zero );

      //SketchPlane skplane = cd.NewSketchPlane( plane ); // 2013

      SketchPlane skplane = SketchPlane.Create( doc, plane ); // 2014

      XYZ forceA = new XYZ( 0, 0, 5 );
      XYZ forceB = new XYZ( 0, 0, 10 );
      List<XYZ> forces = new List<XYZ>();
      forces.Add( forceA );
      forces.Add( forceB );

      XYZ momentA = new XYZ( 0, 0, 0 );
      XYZ momentB = new XYZ( 0, 0, 0 );
      List<XYZ> moments = new List<XYZ>();
      moments.Add( momentA );
      moments.Add( momentB );

      BuiltInCategory bic
        = BuiltInCategory.OST_StructuralFraming;

      FilteredElementCollector beams = Util.GetElementsOfType(
        doc, typeof( FamilyInstance ), bic );

      XYZ p1 = new XYZ( 0, 0, 0 );
      XYZ p2 = new XYZ( 3, 0, 0 );
      List<XYZ> points = new List<XYZ>();
      points.Add( p1 );
      points.Add( p2 );

      // create a new unhosted line load on points:

      LineLoad lineLoadNoHost = cd.NewLineLoad(
        points, forces, moments,
        false, false, false,
        loadSymbol, skplane );

      Debug.Print( "Unhosted line load works." );

      // create new line loads on beam:

      foreach( Element e in beams )
      {
        try
        {
          LineLoad lineLoad = cd.NewLineLoad(
            e, forces, moments,
            false, false, false,
            loadSymbol, skplane );

          Debug.Print( "Hosted line load on beam works." );
        }
        catch( Exception ex )
        {
          Debug.Print( "Hosted line load on beam fails: "
            + ex.Message );
        }

        FamilyInstance i = e as FamilyInstance;

        AnalyticalModel am = i.GetAnalyticalModel();

        foreach( Curve curve in
          am.GetCurves( AnalyticalCurveType.ActiveCurves ) )
        {
          try
          {
            LineLoad lineLoad = cd.NewLineLoad(
              curve.Reference, forces, moments,
              false, false, false,
              loadSymbol, skplane );

            Debug.Print( "Hosted line load on "
              + "AnalyticalModelFrame curve works." );
          }
          catch( Exception ex )
          {
            Debug.Print( "Hosted line load on "
              + "AnalyticalModelFrame curve fails: "
              + ex.Message );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
