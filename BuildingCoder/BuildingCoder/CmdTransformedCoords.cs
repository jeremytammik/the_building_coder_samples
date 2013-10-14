#region Header
//
// CmdTransformedCoords.cs - retrieve coordinates
// from family instance transformed into world
// coordinate system
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
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdTransformedCoords : IExternalCommand
  {
    /// <summary>
    /// Sample file is at
    /// C:\a\j\adn\case\bsd\1242980\attach\mullion.rvt
    /// </summary>
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = app.ActiveUIDocument.Document;
      Selection sel = uidoc.Selection;

      Options options = app.Application.Create.NewGeometryOptions();
      string s, msg = string.Empty;
      int n;
      foreach( Element e in sel.Elements )
      {
        Mullion mullion = e as Mullion;
        if( null != mullion )
        {
          //Location location = mullion.AsFamilyInstance.Location; // seems to be uninitialised // 2011

          Location location = mullion.Location; // 2012

          LocationPoint lp
            = mullion.Location
              as LocationPoint;

          Debug.Assert( null != lp,
            "expected a valid mullion location point" );

          Debug.Assert( null != mullion.LocationCurve, // 2012
            "in Revit 2012, the mullion also has a valid location curve" ); // 2012

          GeometryElement geoElem
            = mullion.get_Geometry( options );

          //GeometryObjectArray objects = geoElem.Objects; // 2012
          //n = objects.Size; // 2012

          n = geoElem.Count<GeometryObject>(); // 2013

          s = string.Format(
            "Mullion <{0} {1}> at {2} rotation"
            + " {3} has {4} geo object{5}:",
            mullion.Name, mullion.Id.IntegerValue,
            Util.PointString( lp.Point ),
            Util.RealString( lp.Rotation ),
            n, Util.PluralSuffix( n ) );

          if( 0 < msg.Length ) { msg += "\n\n"; }
          msg += s;

          //foreach( GeometryObject obj in objects ) // 2012

          foreach( GeometryObject obj in geoElem ) // 2013
          {
            GeometryInstance inst = obj as GeometryInstance;
            Transform t = inst.Transform;

            s = "  Transform " + Util.TransformString( t );
            msg += "\n" + s;

            GeometryElement elem2 = inst.SymbolGeometry;

            //foreach( GeometryObject obj2 in elem2.Objects ) // 2012

            foreach( GeometryObject obj2 in elem2 ) // 2013
            {
              Solid solid = obj2 as Solid;
              if( null != solid )
              {
                FaceArray faces = solid.Faces;
                n = faces.Size;

                s = string.Format(
                  "  {0} face{1}, face point > WCS point:",
                  n, Util.PluralSuffix( n ) );

                msg += "\n" + s;

                foreach( Face face in solid.Faces )
                {
                  s = string.Empty;
                  Mesh mesh = face.Triangulate();
                  foreach( XYZ p in mesh.Vertices )
                  {
                    s += ( 0 == s.Length ) ? "    " : ", ";
                    s += string.Format( "{0} > {1}",
                      Util.PointString( p ),
                      Util.PointString( t.OfPoint( p ) ) );
                  }
                  msg += "\n" + s;
                }
              }
            }
          }
        }
      }
      if( 0 == msg.Length )
      {
        msg = "Please select some mullions.";
      }
      Util.InfoMsg( msg );
      return Result.Failed;
    }
  }
}
