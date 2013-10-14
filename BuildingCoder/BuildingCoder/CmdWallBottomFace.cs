#region Header
//
// CmdWallBottomFace.cs - determine the bottom face of a wall
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
  class CmdWallBottomFace : IExternalCommand
  {
    const double _tolerance = 0.001;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      string s = "a wall, to retrieve its bottom face";

      Wall wall = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), s, false ) as Wall;

      if( null == wall )
      {
        message = "Please select a wall.";
      }
      else
      {
        Options opt = app.Application.Create.NewGeometryOptions();
        GeometryElement e = wall.get_Geometry( opt );

        //foreach( GeometryObject obj in e.Objects ) // 2012

        foreach( GeometryObject obj in e ) // 2013
        {
          Solid solid = obj as Solid;
          if( null != solid )
          {
            foreach( Face face in solid.Faces )
            {
              PlanarFace pf = face as PlanarFace;
              if( null != pf )
              {
                if( Util.IsVertical( pf.Normal, _tolerance )
                  && pf.Normal.Z < 0 )
                {
                  Util.InfoMsg( string.Format(
                    "The bottom face area is {0},"
                    + " and its origin is at {1}.",
                    Util.RealString( pf.Area ),
                    Util.PointString( pf.Origin ) ) );
                  break;
                }
              }
            }
          }
        }
      }
      return Result.Failed;
    }
  }
}
