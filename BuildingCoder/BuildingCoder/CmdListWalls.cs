#region Header
//
// CmdListWalls.cs - list walls
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListWalls : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector walls
        = new FilteredElementCollector( doc );

      walls.OfClass( typeof( Wall ) );

      foreach( Wall wall in walls )
      {
        Parameter param = wall.get_Parameter(
          BuiltInParameter.HOST_AREA_COMPUTED );

        double a = ( ( null != param )
          && ( StorageType.Double == param.StorageType ) )
          ? param.AsDouble()
          : 0.0;

        string s = ( null != param )
          ? param.AsValueString()
          : "null";

        LocationCurve lc = wall.Location as LocationCurve;

        XYZ p = lc.Curve.GetEndPoint( 0 );
        XYZ q = lc.Curve.GetEndPoint( 1 );

        double l = q.DistanceTo( p );

        string format
          = "Wall <{0} {1}> length {2} area {3} ({4})";

        Debug.Print( format,
          wall.Id.IntegerValue.ToString(), wall.Name,
          Util.RealString( l ), Util.RealString( a ),
          s );
      }
      return Result.Succeeded;
    }
  }
}
