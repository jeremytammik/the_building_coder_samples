#region Header
//
// CmdAzimuth.cs - determine direction
// of a line with regard to the north
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
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
  [Transaction( TransactionMode.ReadOnly )]
  class CmdAzimuth : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref String message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Element e = Util.SelectSingleElement(
        uidoc, "a line or wall" );

      LocationCurve curve = null;

      if( null == e )
      {
        message = "No element selected";
      }
      else
      {
        curve = e.Location as LocationCurve;
      }

      if( null == curve )
      {
        message = "No curve available";
      }
      else
      {
        XYZ p = curve.Curve.GetEndPoint( 0 );
        XYZ q = curve.Curve.GetEndPoint( 1 );

        Debug.WriteLine( "Start point "
          + Util.PointString( p ) );

        Debug.WriteLine( "End point "
          + Util.PointString( q ) );

        // the angle between the vectors from the project origin
        // to the start and end points of the wall is pretty irrelevant:

        double a = p.AngleTo( q );
        Debug.WriteLine(
          "Angle between start and end point vectors = "
          + Util.AngleString( a ) );

        XYZ v = q - p;
        XYZ vx = XYZ.BasisX;
        a = vx.AngleTo( v );
        Debug.WriteLine(
          "Angle between points measured from X axis = "
          + Util.AngleString( a ) );

        XYZ z = XYZ.BasisZ;
        a = vx.AngleOnPlaneTo( v, z );
        Debug.WriteLine(
          "Angle around measured from X axis = "
          + Util.AngleString( a ) );

        if( e is Wall )
        {
          Wall wall = e as Wall;
          XYZ w = z.CrossProduct( v ).Normalize();
          if( wall.Flipped ) { w = -w; }
          a = vx.AngleOnPlaneTo( w, z );
          Debug.WriteLine(
            "Angle pointing out of wall = "
            + Util.AngleString( a ) );
        }
      }

      foreach( ProjectLocation location
        in doc.ProjectLocations )
      {
        ProjectPosition projectPosition
          = location.get_ProjectPosition( XYZ.Zero );
        double pna = projectPosition.Angle;
        Debug.WriteLine(
          "Angle between project north and true north "
          + Util.AngleString( pna ) );
      }
      return Result.Failed;
    }
  }
}
