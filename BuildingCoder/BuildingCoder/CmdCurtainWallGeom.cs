#region Header
//
// CmdCurtainWallGeom.cs - retrieve curtain wall geometry
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdCurtainWallGeom : IExternalCommand
  {
    #region list_wall_geom
    void list_wall_geom( Wall w, Application app )
    {
      string s = "";

      CurtainGrid cgrid = w.CurtainGrid;

      Options options
        = app.Create.NewGeometryOptions();

      options.ComputeReferences = true;
      options.IncludeNonVisibleObjects = true;

      GeometryElement geomElem
        = w.get_Geometry( options );

      foreach( GeometryObject obj in geomElem )
      {
        Visibility vis = obj.Visibility;

        string visString = vis.ToString();

        Arc arc = obj as Arc;
        Line line = obj as Line;
        Solid solid = obj as Solid;

        if( arc != null )
        {
          double length = arc.ApproximateLength;

          s += "Length (arc) (" + visString + "): "
            + length + "\n";
        }
        if( line != null )
        {
          double length = line.ApproximateLength;

          s += "Length (line) (" + visString + "): "
            + length + "\n";
        }
        if( solid != null )
        {
          int faceCount = solid.Faces.Size;

          s += "Faces: " + faceCount + "\n";

          foreach( Face face in solid.Faces )
          {
            s += "Face area (" + visString + "): "
              + face.Area + "\n";
          }
        }
        if( line == null && solid == null && arc == null )
        {
          s += "<Other>\n";
        }
      }
      TaskDialog.Show( "revit", s );
    }
    #endregion // list_wall_geom

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      Wall wall = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "a curtain wall", false )
        as Wall;

      if( null == wall )
      {
        message = "Please select a single "
          + "curtain wall element.";

        return Result.Failed;
      }
      else
      {
        LocationCurve locationcurve
          = wall.Location as LocationCurve;

        Curve curve = locationcurve.Curve;

        // move whole geometry over by length of wall:

        XYZ p = curve.GetEndPoint( 0 );
        XYZ q = curve.GetEndPoint( 1 );
        XYZ v = q - p;

        Transform tv = Transform.CreateTranslation( v );

        //curve = curve.get_Transformed( tv ); // 2013
        curve = curve.CreateTransformed( tv ); // 2014

        Creator creator = new Creator( doc );
        creator.CreateModelCurve( curve );

        Options opt = app.Create.NewGeometryOptions();
        opt.IncludeNonVisibleObjects = true;

        GeometryElement e = wall.get_Geometry( opt );

        foreach( GeometryObject obj in e )
        {
          curve = obj as Curve;

          if( null != curve )
          {
            //curve = curve.get_Transformed( tv ); // 2013
            curve = curve.CreateTransformed( tv ); // 2014
            creator.CreateModelCurve( curve );
          }
        }
        return Result.Succeeded;
      }
    }
  }
}

// C:\a\j\adn\case\bsd\1259898\attach\curtain_wall.rvt
