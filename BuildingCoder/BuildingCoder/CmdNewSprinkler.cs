#region Header
//
// CmdNewSprinkler.cs - insert a new sprinkler family instance
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewSprinkler : IExternalCommand
  {
    const string _path = "C:/Documents and Settings/All Users/Application Data/Autodesk/RME 2010/Metric Library/Fire Protection/Sprinklers/";
    const string _name = "M_Sprinkler - Pendent - Hosted";
    const string _ext = ".rfa";

    const string _filename = _path + _name + _ext;

    /// <summary>
    /// Return an arbitrary point on a planar face,
    /// namely the midpoint of the first mesh triangle.
    /// </summary>
    XYZ PointOnFace( PlanarFace face )
    {
      XYZ p = new XYZ( 0, 0, 0 );
      Mesh mesh = face.Triangulate();

      for( int i = 0; i < mesh.NumTriangles; )
      {
        MeshTriangle triangle = mesh.get_Triangle( i );
        p += triangle.get_Vertex( 0 );
        p += triangle.get_Vertex( 1 );
        p += triangle.get_Vertex( 2 );
        p *= 0.3333333333333333;
        break;
      }
      return p;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      try
      {
        UIApplication app = commandData.Application;
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        // retrieve the sprinkler family symbol:

#if _2010
        Filter filter = app.Create.Filter.NewFamilyFilter(
          _name );

        List<Element> families = new List<Element>();
        doc.get_Elements( filter, families );
        Family family = null;

        foreach( Element e in families )
        {
          family = e as Family;
          if( null != family )
            break;
        }
#endif // _2010

        Family family = Util.GetFirstElementOfTypeNamed(
          doc, typeof( Family ), _name ) as Family;

        if( null == family )
        {
          if( !doc.LoadFamily( _filename, out family ) )
          {
            message = "Unable to load '" + _filename + "'.";
            return Result.Failed;
          }
        }

        FamilySymbol sprinklerSymbol = null;
        foreach( FamilySymbol fs in family.Symbols )
        {
          sprinklerSymbol = fs;
          break;
        }

        Debug.Assert( null != sprinklerSymbol,
          "expected at least one sprinkler symbol"
          + " to be defined in family" );

        // pick the host ceiling:

        Element ceiling = Util.SelectSingleElement(
          uidoc, "ceiling to host sprinkler" );

        if( null == ceiling
          || !ceiling.Category.Id.IntegerValue.Equals(
            (int) BuiltInCategory.OST_Ceilings ) )
        {
          message = "No ceiling selected.";
          return Result.Failed;
        }

        //Level level = ceiling.Level;

        //XYZ p = new XYZ( 40.1432351841559, 30.09700395984548, 8.0000 );

        // these two methods cannot create the sprinkler on the ceiling:

        //FamilyInstance fi = doc.Create.NewFamilyInstance( p, sprinklerSymbol, ceiling, level, StructuralType.NonStructural );
        //FamilyInstance fi = doc.Create.NewFamilyInstance( p, sprinklerSymbol, ceiling, StructuralType.NonStructural );

        // use this overload so get the bottom face of the ceiling instead:

        // FamilyInstance NewFamilyInstance( Face face, XYZ location, XYZ referenceDirection, FamilySymbol symbol )

        // retrieve the bottom face of the ceiling:

        Options opt = app.Application.Create.NewGeometryOptions();
        opt.ComputeReferences = true;
        GeometryElement geo = ceiling.get_Geometry( opt );

        PlanarFace ceilingBottom = null;

        foreach( GeometryObject obj in geo )
        {
          Solid solid = obj as Solid;

          if( null != solid )
          {
            foreach( Face face in solid.Faces )
            {
              PlanarFace pf = face as PlanarFace;

              if( null != pf )
              {
                XYZ normal = pf.Normal.Normalize();

                if( Util.IsVertical( normal )
                  && 0.0 > normal.Z )
                {
                  ceilingBottom = pf;
                  break;
                }
              }
            }
          }
        }
        if( null != ceilingBottom )
        {
          XYZ p = PointOnFace( ceilingBottom );

          // create the sprinkler family instance:

          FamilyInstance fi = doc.Create.NewFamilyInstance(
            ceilingBottom, p, XYZ.BasisX, sprinklerSymbol );

          return Result.Succeeded;
        }
        return Result.Failed;
      }
      catch( Exception ex )
      {
        message = ex.Message;
        return Result.Failed;
      }
    }
  }
}
