#region Header
//
// CmdSlopedWall.cs - create a sloped wall
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
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
  [Transaction( TransactionMode.Automatic )]
  class CmdSlopedWall : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;

      Autodesk.Revit.Creation.Application ac
        = app.Application.Create;

      //CurveArray profile = ac.NewCurveArray(); // 2012
      List<Curve> profile = new List<Curve>( 4 ); // 2012

      double length = 10;
      double heightStart = 5;
      double heightEnd = 8;

      XYZ p = XYZ.Zero;
      XYZ q = ac.NewXYZ( length, 0.0, 0.0 );

      //profile.Append( ac.NewLineBound( p, q ) ); // 2012
      profile.Add( Line.CreateBound( p, q ) ); // 2014

      p = q;
      q += heightEnd * XYZ.BasisZ;

      //profile.Append( ac.NewLineBound( p, q ) ); // 2012
      profile.Add( Line.CreateBound( p, q ) ); // 2014

      p = q;
      q = ac.NewXYZ( 0.0, 0.0, heightStart );

      //profile.Append( ac.NewLineBound( p, q ) ); // 2012
      //profile.Add( ac.NewLineBound( p, q ) ); // 2013
      profile.Add( Line.CreateBound( p, q ) ); // 2014

      p = q;
      q = XYZ.Zero;

      //profile.Append( ac.NewLineBound( p, q ) ); // 2012
      //profile.Add( ac.NewLineBound( p, q ) ); // 2013
      profile.Add( Line.CreateBound( p, q ) ); // 2014

      Document doc = app.ActiveUIDocument.Document;

      //Wall wall = doc.Create.NewWall( profile, false ); // 2012
      Wall wall = Wall.Create( doc, profile, false ); // 2013

      return Result.Succeeded;
    }
  }

  #region TestWall
  /// <summary>
  /// Answer to http://forums.autodesk.com/t5/Autodesk-Revit-API/Why-cannot-create-the-wall-by-following-profiles/m-p/3186912/highlight/false#M2351
  /// 1. Please look at the following two commands in The Building Coder samples: CmdCreateGableWall and CmdSlopedWall.
  /// 2. I actually went and tested your code, and I see that you are checking the verticality using an epsilon value of 1.e-5. I would suggest raising that to 1.e-9, which is more like what Revit used internally. I did so, and then the IsVertical test fails on your three given points, so the message box saying "not vertical" is displayed. After that, Revit displays an error message saying "Can't make Extrusion.ÿ" At the same time, I temporarily see three model lines on the graphics screen. I have to select Cancel in the Revit message box, though, and afterwards the three model lines disappear again.
  /// So I would say the problem lies in your three points. They are not well enough aligned. You need higher precision.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  public class TestWall : IExternalCommand
  {
    public virtual Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      Autodesk.Revit.Creation.Application ac
        = app.Application.Create;

      Transaction transaction = new Transaction( doc );
      transaction.Start( "TestWall" );

      XYZ[] pts = new XYZ[] {
          new XYZ(5.675844469, 8.769334161, -5.537348007),
          new XYZ(5.665137820, 8.771332255, 2.956630685),
          new XYZ(7.672569880, 8.396698044, 2.959412671),
        };

      //CurveArray profile = new CurveArray(); // 2012
      List<Curve> profile = new List<Curve>( pts.Length ); // 2013

      XYZ q = pts[pts.Length - 1];

      foreach( XYZ p in pts )
      {
        //profile.Append( CreateLine( ac, q, p, true ) ); // 2012
        //profile.Add( CreateLine( ac, q, p, true ) ); // 2013

        profile.Add( Line.CreateBound( q, p ) ); // 2014

        q = p;
      }

      XYZ t1 = pts[0] - pts[1];
      XYZ t2 = pts[1] - pts[2];
      XYZ normal2 = t1.CrossProduct( t2 );
      normal2 = normal2.Normalize();

      // Verify this plane is vertical to plane XOY

      if( !IsVertical( normal2, XYZ.BasisZ ) )
      {
        System.Windows.Forms.MessageBox.Show( "not vertical" );
      }

      SketchPlane sketchPlane = CreateSketchPlane(
        doc, normal2, pts[0] );

      //CreateModelCurveArray( // 2012
      //  doc, profile, sketchPlane );

      foreach( Curve c in profile ) // 2013
      {
        doc.Create.NewModelCurve( c, sketchPlane );
      }

      WallType wallType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( WallType ) )
          .First<Element>() as WallType;

      Level level
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Level ) )
          .First<Element>(
            e => e.Name.Equals( "Level 1" ) ) as Level;

      //Wall wall = doc.Create.NewWall( // 2012
      //  profile, wallType, level, true, normal2 );

      Wall wall = Wall.Create( doc,
        profile, wallType.Id, level.Id, true, normal2 );

      transaction.Commit();

      return Result.Succeeded;
    }

    //Line CreateLine(
    //  Autodesk.Revit.Creation.Application ac,
    //  XYZ point,
    //  XYZ endOrDirection,
    //  bool bound )
    //{
    //  return ac.NewLine( point, endOrDirection, 
    //    bound ); // 2013
    //}

    bool IsVertical( XYZ v1, XYZ v2 )
    {
      return 1e-9 > Math.Abs(
        v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z );
    }

    SketchPlane CreateSketchPlane(
      Document doc,
      XYZ normal,
      XYZ origin )
    {
      Plane geometryPlane = doc.Application.Create
        .NewPlane( normal, origin );

      //return doc.Create.NewSketchPlane( geometryPlane ); // 2013

      return SketchPlane.Create( doc, geometryPlane ); // 2014
    }

    ModelCurveArray CreateModelCurveArray(
      Document doc,
      CurveArray geometryCurveArray,
      SketchPlane sketchPlane )
    {
      return doc.Create.NewModelCurveArray(
        geometryCurveArray, sketchPlane );
    }
  }
  #endregion // TestWall
}
