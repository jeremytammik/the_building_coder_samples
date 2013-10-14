#region Header
//
// CmdNewSweptBlend.cs - create a new swept blend element
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewSweptBlend : IExternalCommand
  {
    /// <summary>
    /// Create a sketch plane. This helper method is
    /// copied from the GenericModelCreation SDK sample.
    /// </summary>
    SketchPlane CreateSketchPlane(
      Document doc,
      XYZ normal,
      XYZ origin )
    {
      Application app = doc.Application;

      // create a Geometry.Plane required by the NewSketchPlane() method

      Plane geometryPlane = app.Create.NewPlane( normal, origin );
      if( null == geometryPlane )
      {
        throw new Exception( "Geometry plane creation failed." );
      }

      // create a sketch plane using the Geometry.Plane

      //SketchPlane plane = doc.FamilyCreate.NewSketchPlane( geometryPlane ); // 2013 

      SketchPlane plane = SketchPlane.Create( doc, geometryPlane ); // 2014

      if( null == plane )
      {
        throw new Exception( "Sketch plane creation failed." );
      }
      return plane;
    }

    /// <summary>
    /// Create a new swept blend form.
    /// The NewSweptBlend method requires the
    /// input profile to be in the XY plane.
    /// </summary>
    public void CreateNewSweptBlend( Document doc )
    {
      Debug.Assert( doc.IsFamilyDocument,
        "this method will only work in a family document" );

      Application app = doc.Application;

      Autodesk.Revit.Creation.Application creapp
        = app.Create;

      CurveArrArray curvess0
        = creapp.NewCurveArrArray();

      CurveArray curves0 = new CurveArray();

      XYZ p00 = creapp.NewXYZ( 0, 7.5, 0 );
      XYZ p01 = creapp.NewXYZ( 0, 15, 0 );

      // changing Z to 1 in the following line fails:

      XYZ p02 = creapp.NewXYZ( -1, 10, 0 );

      //curves0.Append( creapp.NewLineBound( p00, p01 ) ); // 2013

      curves0.Append( Line.CreateBound( p00, p01 ) ); // 2014
      curves0.Append( Line.CreateBound( p01, p02 ) );
      curves0.Append( Line.CreateBound( p02, p00 ) );
      curvess0.Append( curves0 );

      CurveArrArray curvess1 = creapp.NewCurveArrArray();
      CurveArray curves1 = new CurveArray();

      XYZ p10 = creapp.NewXYZ( 7.5, 0, 0 );
      XYZ p11 = creapp.NewXYZ( 15, 0, 0 );

      // changing the Z value in the following line fails:

      XYZ p12 = creapp.NewXYZ( 10, -1, 0 );

      curves1.Append( Line.CreateBound( p10, p11 ) );
      curves1.Append( Line.CreateBound( p11, p12 ) );
      curves1.Append( Line.CreateBound( p12, p10 ) );
      curvess1.Append( curves1 );

      SweepProfile sweepProfile0
        = creapp.NewCurveLoopsProfile( curvess0 );

      SweepProfile sweepProfile1
        = creapp.NewCurveLoopsProfile( curvess1 );

      XYZ pnt10 = new XYZ( 5, 0, 0 );
      XYZ pnt11 = new XYZ( 0, 20, 0 );
      Curve curve = Line.CreateBound( pnt10, pnt11 );

      XYZ normal = XYZ.BasisZ;

      SketchPlane splane = CreateSketchPlane(
        doc, normal, XYZ.Zero );

      try
      {
        SweptBlend sweptBlend = doc.FamilyCreate.NewSweptBlend(
          true, curve, splane, sweepProfile0, sweepProfile1 );
      }
      catch( Exception ex )
      {
        Util.ErrorMsg( "NewSweptBlend exception: " + ex.Message );
      }
      return;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      if( doc.IsFamilyDocument )
      {
        CreateNewSweptBlend( doc );

        return Result.Succeeded;
      }
      else
      {
        message = "Please run this command in a family document.";

        return Result.Failed;
      }
    }
  }
}
