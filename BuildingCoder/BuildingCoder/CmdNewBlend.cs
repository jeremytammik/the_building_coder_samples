#region Header
//
// CmdNewBlend.cs - create a new blend element using the NewBlend method
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
  class CmdNewBlend : IExternalCommand
  {
    static Blend CreateBlend( Document doc )
    {
      Debug.Assert( doc.IsFamilyDocument,
        "this method will only work in a family document" );

      Application app = doc.Application;

      Autodesk.Revit.Creation.Application creApp
        = app.Create;

      Autodesk.Revit.Creation.FamilyItemFactory factory
        = doc.FamilyCreate;

      double startAngle = 0;
      double midAngle = Math.PI;
      double endAngle = 2 * Math.PI;

      XYZ xAxis = XYZ.BasisX;
      XYZ yAxis = XYZ.BasisY;

      XYZ center = XYZ.Zero;
      XYZ normal = -XYZ.BasisZ;
      double radius = 0.7579;

      //Arc arc1 = creApp.NewArc( center, radius, startAngle, midAngle, xAxis, yAxis ); // 2013
      //Arc arc2 = creApp.NewArc( center, radius, midAngle, endAngle, xAxis, yAxis ); // 2013

      Arc arc1 = Arc.Create( center, radius, startAngle, midAngle, xAxis, yAxis ); // 2014
      Arc arc2 = Arc.Create( center, radius, midAngle, endAngle, xAxis, yAxis ); // 2014

      CurveArray baseProfile = new CurveArray();

      baseProfile.Append( arc1 );
      baseProfile.Append( arc2 );

      // create top profile:

      CurveArray topProfile = new CurveArray();

      bool circular_top = false;

      if( circular_top )
      {
        // create a circular top profile:

        XYZ center2 = new XYZ( 0, 0, 1.27 );

        //Arc arc3 = creApp.NewArc( center2, radius, startAngle, midAngle, xAxis, yAxis ); // 2013
        //Arc arc4 = creApp.NewArc( center2, radius, midAngle, endAngle, xAxis, yAxis ); // 2013

        Arc arc3 = Arc.Create( center2, radius, startAngle, midAngle, xAxis, yAxis ); // 2014
        Arc arc4 = Arc.Create( center2, radius, midAngle, endAngle, xAxis, yAxis ); // 2014

        topProfile.Append( arc3 );
        topProfile.Append( arc4 );
      }
      else
      {
        // create a skewed rectangle top profile:

        XYZ[] pts = new XYZ[] {
          new XYZ(0,0,3),
          new XYZ(2,0,3),
          new XYZ(3,2,3),
          new XYZ(0,4,3)
        };

        for( int i = 0; i < 4; ++i )
        {
          //topProfile.Append( creApp.NewLineBound( // 2013

          topProfile.Append( Line.CreateBound( // 2014
            pts[0 == i ? 3 : i - 1], pts[i] ) );
        }
      }

      Plane basePlane = creApp.NewPlane(
        normal, center );

      //SketchPlane sketch = factory.NewSketchPlane( basePlane ); // 2013
      SketchPlane sketch = SketchPlane.Create( doc, basePlane ); // 2014

      Blend blend = factory.NewBlend( true,
        topProfile, baseProfile, sketch );

      return blend;
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
        Blend blend = CreateBlend( doc );

        return Result.Succeeded;
      }
      else
      {
        message = "Please run this command "
          + "in a family document.";

        return Result.Failed;
      }
    }
  }
}
