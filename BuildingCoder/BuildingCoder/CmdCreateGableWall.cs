#region Header
//
// CmdCreateGableWall.cs - create gable wall specifying non-rectangular wall profile
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdCreateGableWall : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Build a wall profile for the wall creation

      XYZ[] pts = new XYZ[] {
        XYZ.Zero,
        new XYZ( 20, 0, 0 ),
        new XYZ( 20, 0, 15 ),
        new XYZ( 10, 0, 30 ),
        new XYZ( 0, 0, 15 )
      };

      // Get application creation object

      Autodesk.Revit.Creation.Application appCreation
        = app.Create;

      // Create wall profile

      //CurveArray profile = new CurveArray(); // 2012

      //XYZ q = pts[pts.Length - 1];

      //foreach( XYZ p in pts )
      //{
      //  profile.Append( appCreation.NewLineBound(
      //    q, p ) );

      //  q = p;
      //}

      List<Curve> profile = new List<Curve>( // 2013
        pts.Length );

      XYZ q = pts[pts.Length - 1];

      foreach( XYZ p in pts )
      {
        //profile.Add( appCreation.NewLineBound( q, p ) ); // 2013
        profile.Add( Line.CreateBound( q, p ) ); // 2014
        q = p;
      }

      XYZ normal = XYZ.BasisY;

      //WallType wallType
      //  = new FilteredElementCollector( doc )
      //    .OfClass( typeof( WallType ) )
      //    .First<Element>( e
      //      => e.Name.Contains( "Generic" ) )
      //    as WallType;

      WallType wallType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( WallType ) )
          .First<Element>()
            as WallType;

      Level level
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Level ) )
          .First<Element>( e
            => e.Name.Equals( "Level 1" ) )
          as Level;

      Transaction trans = new Transaction( doc );

      trans.Start( "Create Gable Wall" );

      //Wall wall = doc.Create.NewWall( // 2012
      //  profile, wallType, level, true, normal );

      Wall wall = Wall.Create( // 2013
        doc, profile, wallType.Id, level.Id, true, normal );

      trans.Commit();

      return Result.Succeeded;
    }
  }
}
