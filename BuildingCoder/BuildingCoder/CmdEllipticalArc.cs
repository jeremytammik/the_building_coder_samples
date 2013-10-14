#region Header
//
// CmdEllipticalArc.cs - create an elliptical arc geometry object
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdEllipticalArc : IExternalCommand
  {
    /// <summary>
    /// Create and return a new elliptical arc geometry object
    /// with a given start and end angle.
    /// </summary>
    Ellipse CreateEllipse( Application app )
    {
      XYZ center = XYZ.Zero;

      double radX = 30;
      double radY = 50;

      XYZ xVec = XYZ.BasisX;
      XYZ yVec = XYZ.BasisY;

      double param0 = 0.0;
      double param1 = 2 * Math.PI;

      //Ellipse e = app.Create.NewEllipse( center, radX, radY, xVec, yVec, param0, param1 ); // 2013

      Ellipse e = Ellipse.Create( center, radX, radY, xVec, yVec, param0, param1 ); // 2014

      // Create a line from ellipse center in
      // direction of target angle:

      double targetAngle = Math.PI / 3.0;

      XYZ direction = new XYZ(
        Math.Cos( targetAngle ),
        Math.Sin( targetAngle ),
        0 );

      //Line line = app.Create.NewLineUnbound( center, direction ); // 2013

      Line line = Line.CreateUnbound( center, direction ); // 2014

      // Find intersection between line and ellipse:

      IntersectionResultArray results;
      e.Intersect( line, out results );

      // Find the shortest intersection segment:

      foreach( IntersectionResult result in results )
      {
        double p = result.UVPoint.U;
        if( p < param1 )
        {
          param1 = p;
        }
      }

      // Apply parameter to the ellipse:

      e.MakeBound( param0, param1 );

      return e;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Application app = commandData.Application.Application;

      Ellipse e = CreateEllipse( app );

      return Result.Failed;
    }
  }
}
