#region Header
//
// CmdUnrotateNorth.cs - transform element location back to
// original coordinates to cancel effect of rotating project north
//
// Copyright (C) 2009-2019 by Jeremy Tammik,
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
  [Transaction( TransactionMode.ReadOnly )]
  class CmdUnrotateNorth : IExternalCommand
  {
    #region Set project location to city location
    void SetSiteLocationToCity1( Document doc )
    {
      Autodesk.Revit.DB.CitySet cities = doc.Application.Cities;
      int nCount = cities.Size;
      try
      {
        CitySetIterator item = cities.ForwardIterator();
        while( item != null )
        {
          item.MoveNext();
          City city = item.Current as City;
          if( city.Name.Contains( "中国" ) ||
          city.Name.Contains( "China" ) )
          {
            Transaction transaction = new Transaction( doc, "Create Wall" );
            transaction.Start();

            ProjectLocation projectLocation = doc.ActiveProjectLocation;

            //SiteLocation site = projectLocation.SiteLocation; // 2017
            SiteLocation site = projectLocation.GetSiteLocation(); // 2018

            // site.PlaceName = city.Name;
            site.Latitude = city.Latitude; // latitude information
            site.Longitude = city.Longitude; // longitude information
            site.TimeZone = city.TimeZone; // TimeZone information
            transaction.Commit();
            break;
          }
        }
      }
      catch( Exception ret )
      {
        Debug.Print( ret.Message );
      }
    }

    void SetSiteLocationToCity2( Document doc )
    {
      CitySet cities = doc.Application.Cities;

      foreach( City city in cities )
      {
        string s = city.Name;

        if( s.Contains( "中国" ) || s.Contains( "China" ) )
        {
          using( Transaction t = new Transaction( doc ) )
          {
            t.Start( "Set Site Location to City" );

            ProjectLocation projectLocation = doc.ActiveProjectLocation;

            //SiteLocation site = projectLocation.SiteLocation; // 2017
            SiteLocation site = projectLocation.GetSiteLocation(); // 2018

            // site.PlaceName = city.Name;
            site.Latitude = city.Latitude; // latitude information
            site.Longitude = city.Longitude; // longitude information
            site.TimeZone = city.TimeZone; // TimeZone information

            // SiteLocation property is read-only:
            //projectLocation.SiteLocation = site;

            t.Commit();
          }
          break;
        }
      }
    }
    #endregion // Set project location to city location

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      #region Determine true north rotation

      Element projectInfoElement
        = new FilteredElementCollector( doc )
          .OfCategory( BuiltInCategory.OST_ProjectBasePoint )
          .FirstElement();

      BuiltInParameter bipAtn
        = BuiltInParameter.BASEPOINT_ANGLETON_PARAM;

      Parameter patn = projectInfoElement.get_Parameter(
        bipAtn );

      double atn = patn.AsDouble();

      Debug.Print(
        "Angle to north from project info: {0}",
        Util.AngleString( atn ) );

      #endregion // Determine true north rotation

      //ElementSet els = uidoc.Selection.Elements; // 2014

      ICollection<ElementId> ids = uidoc.Selection.GetElementIds(); // 2015

      if( 1 != ids.Count )
      {
        message = "Please select a single element.";
      }
      else
      {
        //ElementSetIterator it = els.ForwardIterator();
        //it.MoveNext();
        //Element e = it.Current as Element; // 2014

        Element e = doc.GetElement( ids.First() ); // 2015

        XYZ p;
        if( !Util.GetElementLocation( out p, e ) )
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
            //ProjectPosition projectPosition
            //  = location.get_ProjectPosition( XYZ.Zero ); // 2017

            ProjectPosition projectPosition
              = location.GetProjectPosition( XYZ.Zero ); // 2018

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
