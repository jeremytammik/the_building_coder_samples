#region Header
//
// CmdNestedInstanceGeo.cs - analyse
// nested instance geometry and structure
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
  [Transaction( TransactionMode.Automatic )]
  class CmdNestedInstanceGeo : IExternalCommand
  {
    /// <summary>
    /// Define equality between XYZ objects, ensuring that almost equal points compare equal.
    /// </summary>
    class XyzEqualityComparer : IEqualityComparer<XYZ>
    {
      public bool Equals( XYZ p, XYZ q )
      {
        return p.IsAlmostEqualTo( q );
      }

      public int GetHashCode( XYZ p )
      {
        return Util.PointString( p ).GetHashCode();
      }
    }

    static void GetVertices( List<XYZ> vertices, Solid s )
    {
      Debug.Assert( 0 < s.Edges.Size,
        "expected a non-empty solid" );

      Dictionary<XYZ, int> a
        = new Dictionary<XYZ, int>(
          new XyzEqualityComparer() );

      foreach( Face f in s.Faces )
      {
        Mesh m = f.Triangulate();
        foreach( XYZ p in m.Vertices )
        {
          if( !a.ContainsKey( p ) )
          {
            a.Add( p, 1 );
          }
          else
          {
            ++a[p];
          }
        }
      }
      List<XYZ> keys = new List<XYZ>( a.Keys );

      Debug.Assert( 8 == keys.Count,
        "expected eight vertices for a rectangular column" );

      keys.Sort( Util.Compare );

      foreach( XYZ p in keys )
      {
        Debug.Assert( 3 == a[p],
          "expected every vertex of solid to appear in exactly three faces" );

        vertices.Add( p );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> a = new List<Element>();

      if( !Util.GetSelectedElementsOrAll( a, uidoc,
        typeof( FamilyInstance ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some family instances."
          : "No family instances found.";
        return Result.Failed;
      }
      FamilyInstance inst = a[0] as FamilyInstance;

      // Here are two ways to traverse the nested instance geometry.
      // The first way can get the right position, but can't get the right structure.
      // The second way can get the right structure, but can't get the right position.
      // What I want is the right structure and right position.

      // First way:

      // In the current project project1.rvt, I can get myFamily3 instance via API,
      // the class is Autodesk.Revit.Elements.FamilyInstance.
      // Then i try to get its geometry:

      Options opt = app.Application.Create.NewGeometryOptions();
      GeometryElement geoElement = inst.get_Geometry( opt );

      //GeometryObjectArray a1 = geoElement.Objects; // 2012
      //int n = a1.Size; // 2012

      int n = geoElement.Count<GeometryObject>(); // 2013

      Debug.Print(
        "Family instance geometry has {0} geometry object{1}{2}",
        n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

      int i = 0;

      //foreach( GeometryObject o1 in a1 ) // 2012
      foreach( GeometryObject o1 in geoElement ) // 2013

      {
        GeometryInstance geoInstance = o1 as GeometryInstance;
        if( null != geoInstance )
        {
          // geometry includes one instance, so get its geometry:

          GeometryElement symbolGeo = geoInstance.SymbolGeometry;
          
          //GeometryObjectArray a2 = symbolGeo.Objects; // 2012
          //foreach( GeometryObject o2 in a2 ) // 2012

          // the symbol geometry contains five solids.
          // how can I find out which solid belongs to which column?
          // how to relate the solid to the family instance?

          foreach( GeometryObject o2 in symbolGeo )
          {
            Solid s = o2 as Solid;
            if( null != s && 0 < s.Edges.Size )
            {
              List<XYZ> vertices = new List<XYZ>();
              GetVertices( vertices, s );
              n = vertices.Count;

              Debug.Print( "Solid {0} has {1} vertices{2} {3}",
                i++, n, Util.DotOrColon( n ),
                Util.PointArrayString( vertices ) );
            }
          }
        }
      }

      // In the Revit 2009 API, we can use
      // FamilyInstance.Symbol.Family.Components
      // to obtain the nested family instances
      // within the top level family instance.

      // In the Revit 2010 API, this property has been
      // removed, since we can iterate through the elements
      // of a family just like any other document;
      // cf. What's New in the RevitAPI.chm:


#if REQUIRES_REVIT_2009_API
      ElementSet components = inst.Symbol.Family.Components;
      n = components.Size;
#endif // REQUIRES_REVIT_2009_API

      Document fdoc = doc.EditFamily( inst.Symbol.Family );

#if REQUIRES_REVIT_2010_API
      List<Element> components = new List<Element>();
      fdoc.get_Elements( typeof( FamilyInstance ), components );
      n = components.Count;
#endif // REQUIRES_REVIT_2010_API

      FilteredElementCollector collector
        = new FilteredElementCollector( fdoc );

      collector.OfClass( typeof( FamilyInstance ) );
      IList<Element> components = collector.ToElements();

      Debug.Print(
        "Family instance symbol family has {0} component{1}{2}",
        n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

      foreach( Element e in components )
      {

        // there are 3 FamilyInstance: Column, myFamily1, myFamily2
        // then we can loop myFamily1, myFamily2 also.
        // then get all the Column geometry
        // But all the Column's position is the same,
        // because the geometry is defined by the Symbol.
        // Not the actually position in project1.rvt

        LocationPoint lp = e.Location as LocationPoint;
        Debug.Print( "{0} at {1}",
          Util.ElementDescription( e ),
          Util.PointString( lp.Point ) );
      }
      return Result.Failed;
    }
  }
}
