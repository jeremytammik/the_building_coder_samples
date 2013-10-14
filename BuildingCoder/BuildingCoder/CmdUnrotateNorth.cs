#region Header
//
// CmdUnrotateNorth.cs - transform element location back to
// original coordinates to cancel effect of rotating project north
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
  [Transaction( TransactionMode.ReadOnly )]
  class CmdUnrotateNorth : IExternalCommand
  {
    /// <summary>
    /// Return a location for the given element.
    /// Use either the element's LocationPoint Point property,
    /// or its LocationCurve start point, whichever is available.
    /// </summary>
    /// <param name="p">Return element location point</param>
    /// <param name="e">Revit Element</param>
    /// <returns>True if a location point is available for the given element,
    /// otherwise false.</returns>
    bool GetElementLocation(
      out XYZ p,
      Element e )
    {
      p = XYZ.Zero;
      bool rc = false;
      Location loc = e.Location;
      if( null != loc )
      {
        LocationPoint lp = loc as LocationPoint;
        if( null != lp )
        {
          p = lp.Point;
          rc = true;
        }
        else
        {
          LocationCurve lc = loc as LocationCurve;

          Debug.Assert( null != lc,
            "expected location to be either point or curve" );

          p = lc.Curve.GetEndPoint( 0 );
          rc = true;
        }
      }
      return rc;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      ElementSet els = uidoc.Selection.Elements;

      if( 1 != els.Size )
      {
        message = "Please select a single element.";
      }
      else
      {
        ElementSetIterator it = els.ForwardIterator();
        it.MoveNext();

        Element e = it.Current as Element;

        XYZ p;
        if( !GetElementLocation( out p, e ) )
        {
          message
            = "Selected element has no location defined.";

          Debug.Print( message );
        }
        else
        {
          string msg
            = "Selected element location: "
            + Util.PointString( p );

          XYZ pnp;
          double x, y, pna;

          foreach( ProjectLocation location
            in doc.ProjectLocations )
          {
            ProjectPosition projectPosition
              = location.get_ProjectPosition( XYZ.Zero );

            x = projectPosition.EastWest;
            y = projectPosition.NorthSouth;
            pnp = new XYZ( x, y, 0.0 );
            pna = projectPosition.Angle;

            msg +=
              "\nAngle between project north and true north: "
              + Util.AngleString( pna );

            // Transform tr = Transform.get_Rotation( XYZ.Zero, XYZ.BasisZ, pna ); // 2013
            Transform tr = Transform.CreateRotation( XYZ.BasisZ, pna ); // 2014

            //Transform tt = Transform.get_Translation( pnp ); // 2013
            Transform tt = Transform.CreateTranslation( pnp ); // 2014

            Transform t = tt.Multiply( tr );

            msg +=
              "\nUnrotated element location: "
              + Util.PointString( tr.OfPoint( p ) ) + " "
              + Util.PointString( tt.OfPoint( p ) ) + " "
              + Util.PointString( t.OfPoint( p ) );

            Util.InfoMsg( msg );
          }
        }
      }
      return Result.Failed;
    }
  }
}
