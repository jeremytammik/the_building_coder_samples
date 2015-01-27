#region Header
//
// CmdNewSweptBlend.cs - create a new swept blend element
//
// Copyright (C) 2010-2015 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
  [Transaction( TransactionMode.Manual )]
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

      // Create a Geometry.Plane required by the 
      // NewSketchPlane() method

      Plane geometryPlane = app.Create.NewPlane(
        normal, origin );

      if( null == geometryPlane )
      {
        throw new Exception( "Geometry plane creation failed." );
      }

      // Create a sketch plane using the Geometry.Plane

      //SketchPlane plane = doc.FamilyCreate.NewSketchPlane( geometryPlane ); // 2013 

      SketchPlane plane = SketchPlane.Create(
        doc, geometryPlane ); // 2014

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
    }

    /// <summary>
    /// Create a new swept blend form using arcs to
    /// define circular start and end profiles and an
    /// arc path. The NewSweptBlend method requires 
    /// the input profiles to be in the XY plane.
    /// </summary>
    public void CreateNewSweptBlendArc( Document doc )
    {
      Debug.Assert( doc.IsFamilyDocument,
        "this method will only work in a family document" );

      Application app = doc.Application;

      Autodesk.Revit.Creation.Application creapp
        = app.Create;

      Autodesk.Revit.Creation.FamilyItemFactory credoc
        = doc.FamilyCreate;

      #region Original code for Revit 2012
      #if COMPILE_ORIGINAL_CODE
      XYZ pnt1 = new XYZ( 0, -1, 0 );
      XYZ pnt2 = new XYZ( 1, 0, 0 );
      XYZ pnt3 = new XYZ( 0, 1, 0 );
      XYZ pnt4 = new XYZ( -1, 0, 0 );
      Arc aArc1 = creapp.NewArc( pnt1, pnt3, pnt2 );
      Arc aArc2 = creapp.NewArc( pnt3, pnt1, pnt4 );
      CurveArrArray arrarr1 = new CurveArrArray();

      SweepProfile bottomProfile
        = creapp.NewCurveLoopsProfile( arrarr1 );

      CurveArray arr1 = new CurveArray();
      arr1.Append( aArc1 );
      arr1.Append( aArc2 );
      XYZ pnt6 = new XYZ( 0, -2, 0 );
      XYZ pnt7 = new XYZ( 2, 0, 0 );
      XYZ pnt8 = new XYZ( 0, 2, 0 );
      XYZ pnt9 = new XYZ( -2, 0, 0 );
      Arc aArc3 = creapp.NewArc( pnt6, pnt8, pnt7 );
      Arc aArc4 = creapp.NewArc( pnt8, pnt6, pnt9 );
      CurveArrArray arrarr2 = new CurveArrArray();
      CurveArray arr2 = new CurveArray();
      arr2.Append( aArc3 );
      arr2.Append( aArc4 );
      arrarr2.Append( arr2 );

      SweepProfile topProfile
        = creapp.NewCurveLoopsProfile( arrarr2 );

      XYZ pnt10 = new XYZ( 0, 0, 0 );
      XYZ pnt11 = new XYZ( 0, 5, 0 );
      XYZ pnt122 = new XYZ( 2.5, 2.5, 0 );
      Arc testArc = creapp.NewArc( pnt10, pnt11, pnt122 );
      Curve curve = (Curve) testArc;

      Plane geometryPlane = creapp.NewPlane(
        XYZ.BasisZ, XYZ.Zero );

      SketchPlane sketchPlane = doc.NewSketchPlane(
        geometryPlane );

      SweptBlend aSweptBlend = doc.NewSweptBlend(
        true, curve, sketchPlane, bottomProfile,
        topProfile );
      #endif // COMPILE_ORIGINAL_CODE
      #endregion // Original code for Revit 2012

      XYZ px = XYZ.BasisX;
      XYZ py = XYZ.BasisY;
      Arc arc1 = Arc.Create( -px, px, -py );
      Arc arc2 = Arc.Create( px, -px, py );
      CurveArray arr1 = new CurveArray();
      arr1.Append( arc1 );
      arr1.Append( arc2 );
      CurveArrArray arrarr1 = new CurveArrArray();
      arrarr1.Append( arr1 );

      SweepProfile bottomProfile
        = creapp.NewCurveLoopsProfile( arrarr1 );

      px += px;
      py += py;
      Arc arc3 = Arc.Create( -px, px, -py );
      Arc arc4 = Arc.Create( px, -px, py );
      CurveArray arr2 = new CurveArray();
      arr2.Append( arc3 );
      arr2.Append( arc4 );
      CurveArrArray arrarr2 = new CurveArrArray();
      arrarr2.Append( arr2 );

      SweepProfile topProfile
        = creapp.NewCurveLoopsProfile( arrarr2 );

      XYZ p0 = XYZ.Zero;
      XYZ p5 = 5 * XYZ.BasisY;
      XYZ pmid = new XYZ( 2.5, 2.5, 0 );
      Arc testArc = Arc.Create( p0, p5, pmid );

      Plane geometryPlane = creapp.NewPlane(
        XYZ.BasisZ, XYZ.Zero );

      SketchPlane sketchPlane = SketchPlane.Create(
        doc, geometryPlane );

      SweptBlend aSweptBlend = credoc.NewSweptBlend(
        true, testArc, sketchPlane, bottomProfile,
        topProfile );
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
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Create New Swept Blend" );

          CreateNewSweptBlend( doc );
          CreateNewSweptBlendArc( doc );

          tx.Commit();

          return Result.Succeeded;
        }
      }
      else
      {
        message
          = "Please run this command in a family document.";

        return Result.Failed;
      }
    }
  }
}
