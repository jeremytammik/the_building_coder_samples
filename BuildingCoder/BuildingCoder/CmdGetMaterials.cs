#region Header
//
// CmdGetMaterials.cs - determine element materials
// by iterating over its geometry faces
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
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  #region Victor sample code
  public class ElementComparer : IEqualityComparer<Element>
  {
    public bool Equals( Element x, Element y )
    {
      if( x == null || y == null ) return false;

      return ( x.Id.IntegerValue == y.Id.IntegerValue )
             && ( x.GetType() == y.GetType()
                 && ( x.Document.Equals( y.Document ) ) );
    }

    public int GetHashCode( Element obj )
    {
      return obj.UniqueId.GetHashCode();
    }
  }

  public class MaterialComparer : IEqualityComparer<Material>
  {
    public bool Equals( Material x, Material y )
    {
      return x.UniqueId.Equals( y.UniqueId );
    }

    public int GetHashCode( Material obj )
    {
      return obj.UniqueId.GetHashCode();
    }
  }
  #endregion // Victor sample code

  [Transaction( TransactionMode.ReadOnly )]
  class CmdGetMaterials : IExternalCommand
  {
    #region Victor sample code

    class ElementMaterial
    {
      private readonly Element _element;
      private readonly Material _material;

      public ElementMaterial( Element element, Material material )
      {
        if( element == null ) throw new ArgumentNullException( "element" );
        if( material == null ) throw new ArgumentNullException( "material" );
        _element = element;
        _material = material;
      }

      public Material Material
      {
        get { return _material; }
      }

      public Element Element
      {
        get { return _element; }
      }
    }

#if BEFORE_REVIT_2013
    void Victor( Document document )
    {
      var material = document
        .Settings
        .Materials
        .OfType<Material>()
        .FirstOrDefault( m => m.Name.Equals( "Concrete" ) );

      List<Element> elements = null;

      var elementsWithMaterials =
        ( from el in elements
          from Material m in el.Materials
          select new ElementMaterial( el, m ) )
        .ToList();

      var groupedElementsInMaterial
        = elementsWithMaterials
          .GroupBy( x => x.Material, new MaterialComparer() )
          .OrderBy( x => x.Key.Name );
    }
#endif // BEFORE_REVIT_2013
    #endregion // Victor sample code

#if BEFORE_REVIT_2013
    /// <summary>
    /// Return a list of the document's 
    /// non-null materials sorted by name.
    /// </summary>
    List<Material> GetSortedMaterials( Document doc )
    {
      Materials doc_materials 
        = doc.Settings.Materials;

      int n = doc_materials.Size;

      List<Material> materials_sorted 
        = new List<Material>( n );

      foreach( Material m in doc_materials )
      {
        if( m != null )
        {
          materials_sorted.Add( m );
        }
      }
      materials_sorted.Sort( 
        delegate( Material m1, Material m2 )
        {
          return m1.Name.CompareTo( m2.Name );
        }
      );
      return materials_sorted;
    }
#endif // BEFORE_REVIT_2013

    /// <summary>
    /// Return a list of the document's materials 
    /// from a filtered element collector.
    /// </summary>
    FilteredElementCollector FilterForMaterials(
      Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( Material ) );
    }

    /// <summary>
    /// Replacement for deprecated property 
    /// Face.MaterialElement to access material name 
    /// for a given geometry face.
    /// </summary>
    static string FaceMaterialName(
      Document doc,
      Face face )
    {
      ElementId id = face.MaterialElementId;
      Material m = doc.GetElement( id ) as Material;
      return m.Name;
    }

    /// <summary>
    /// Return family instance element material, either
    /// for the given instance or the entire category.
    /// If no element material is specified and the
    /// ByCategory material information is null, set
    /// it to a valid value at the category level.
    /// </summary>
    public Material GetMaterial(
      Document doc,
      FamilyInstance fi )
    {
      Material material = null;

      foreach( Parameter p in fi.Parameters )
      {
        Definition def = p.Definition;

        // the material is stored as element id:

        if( p.StorageType == StorageType.ElementId
          && def.ParameterGroup == BuiltInParameterGroup.PG_MATERIALS
          && def.ParameterType == ParameterType.Material )
        {
          ElementId materialId = p.AsElementId();

          if( -1 == materialId.IntegerValue )
          {
            // invalid element id, so we assume
            // the material is "By Category":

            if( null != fi.Category )
            {
              material = fi.Category.Material;

              if( null == material )
              {
                //MaterialOther mat
                //  = doc.Settings.Materials.AddOther(
                //    "GoodConditionMat" ); // 2011

                ElementId id = Material.Create( doc, "GoodConditionMat" ); // 2012
                Material mat = doc.GetElement( id ) as Material;

                mat.Color = new Color( 255, 0, 0 );

                fi.Category.Material = mat;

                material = fi.Category.Material;
              }
            }
          }

#if BEFORE_REVIT_2013
          else
          {
            material = doc.Settings.Materials.get_Item(
              materialId );
          }
#endif // BEFORE_REVIT_2013

          break;
        }
      }
      return material;
    }

    /// <summary>
    /// Return a list of all materials by recursively traversing
    /// all the object's solids' faces, retrieving their face
    /// materials, and collecting them in a list.
    ///
    /// Original implementation, not always robust as noted by Andras Kiss in
    /// http://thebuildingcoder.typepad.com/blog/2008/10/family-instance-materials.html?cid=6a00e553e1689788330115713e2e3c970b#comment-6a00e553e1689788330115713e2e3c970b
    /// </summary>
    public List<string> GetMaterials1(
      Document doc,
      GeometryElement geo )
    {
      List<string> materials = new List<string>();

      //foreach( GeometryObject o in geo.Objects ) // 2012

      foreach( GeometryObject o in geo ) // 2013
      {
        if( o is Solid )
        {
          Solid solid = o as Solid;
          foreach( Face face in solid.Faces )
          {
            //string s = face.MaterialElement.Name; // 2011
            string s = FaceMaterialName( doc, face ); // 2012
            materials.Add( s );
          }
        }
        else if( o is GeometryInstance )
        {
          GeometryInstance i = o as GeometryInstance;
          materials.AddRange( GetMaterials1(
            doc, i.SymbolGeometry ) );
        }
      }
      return materials;
    }

    /// <summary>
    /// Return a list of all materials by traversing
    /// all the object's and its instances' solids'
    /// faces, retrieving their face materials,
    /// and collecting them in a list.
    ///
    /// Enhanced more robust implementation suggested
    /// by Harry Mattison, but lacking the recursion
    /// of the first version.
    /// </summary>
    public List<string> GetMaterials(
      Document doc,
      GeometryElement geo )
    {
      List<string> materials = new List<string>();

      //foreach( GeometryObject o in geo.Objects ) // 2012
      
      foreach( GeometryObject o in geo ) // 2013
      {
        if( o is Solid )
        {
          Solid solid = o as Solid;
          if( null != solid )
          {
            foreach( Face face in solid.Faces )
            {
              //string s = face.MaterialElement.Name; // 2011
              string s = FaceMaterialName( doc, face ); // 2012
              materials.Add( s );
            }
          }
        }
        else if( o is GeometryInstance )
        {
          GeometryInstance i = o as GeometryInstance;
          //foreach( Object geomObj in i.SymbolGeometry.Objects ) // 2012
          foreach( Object geomObj in i.SymbolGeometry ) // 2013
          {
            Solid solid = geomObj as Solid;
            if( solid != null )
            {
              foreach( Face face in solid.Faces )
              {
                //string s = face.MaterialElement.Name; // 2011
                string s = FaceMaterialName( doc, face ); // 2012
                materials.Add( s );
              }
            }
          }
        }
      }
      return materials;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;
      Options opt = app.Application.Create.NewGeometryOptions();
      Material mat;
      string msg = string.Empty;
      int i, n;

      foreach( Element e in sel.Elements )
      {
        // for 310_ensure_material.htm:

        if( e is FamilyInstance )
        {
          mat = GetMaterial( doc, e as FamilyInstance );

          Util.InfoMsg(
            "Family instance element material: "
            + ( null == mat ? "<null>" : mat.Name ) );
        }

        GeometryElement geo = e.get_Geometry( opt );

        // if you are not interested in duplicate
        // materials, you can define a class that
        // overloads the Add method to only insert
        // a new entry if its value is not already
        // present in the list, instead of using
        // the standard List<> class:

        List<string> materials = GetMaterials( doc, geo );

        msg += "\n" + Util.ElementDescription( e );

        n = materials.Count;
        if( 0 == n )
        {
          msg += " has no materials.";
        }
        else
        {
          i = 0;
          msg += string.Format(
            " has {0} material{1}:",
            n, Util.PluralSuffix( n ) );

          foreach( string s in materials )
          {
            msg += string.Format(
              "\n  {0} {1}", i++, s );
          }
        }
      }
      if( 0 == msg.Length )
      {
        msg = "Please select some elements.";
      }
      Util.InfoMsg( msg );
      return Result.Succeeded;
    }
  }

  #region Victor sample code 2
#if BEFORE_REVIT_2013
  [Transaction( TransactionMode.Manual )]
  public class RetrievingMaterialCommand : IExternalCommand
  {
    public Result Execute( 
      ExternalCommandData commandData, 
      ref string message, 
      ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document;

      const string materialName = "Concrete";

      FilteredElementCollector collector =
          new FilteredElementCollector( doc );

      var sw = Stopwatch.StartNew();
      var materialsLinq =
          doc
              .Settings
              .Materials
              .OfType<Material>()
              .ToList();
      sw.Stop();
      Debug.Print( "LINQ: Elapsed time: {0}\tMaterials count: {1}", sw.Elapsed, materialsLinq.Count );

      sw = Stopwatch.StartNew();
      var materials = collector
          .OfClass( typeof( Material ) )
          .OfType<Material>()
          .ToList();
      sw.Stop();
      Debug.Print( "FilteredElementCollector: Elapsed time: {0}\tMaterials count: {1}", sw.Elapsed, materials.Count );

      Debug.Print( "Document.Settings.Materials count: {0}", doc.Settings.Materials.Size );

  #region using extension methods

      var materialsInDocument = doc.GetMaterials();

      var concreteMaterial = doc.Settings.Materials
        .GetMaterialByName( materialName );

      #endregion

      var materialsRetrievingViaCollectorButNotInDocumentSettings =
          materials
              .Except( materialsLinq, new MaterialComparer() )
              .ToList();

      // ******
      // Materials.get_Item required transaction
      // Why? I'm just read data
      // ******
      var trans = new Transaction( doc, "Get Material" );
      trans.Start();

      // ******
      // NullReferencException throws here if
      // Materials collection contains null
      // ******
      var material = doc.Settings.Materials.get_Item( materialName );

      trans.RollBack();

      return Result.Succeeded;
    }
  }

  public static class DocumentExtensions
  {
    public static IEnumerable<Material> GetMaterials(
      this Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      return collector
        .OfClass( typeof( Material ) )
        .OfType<Material>();
    }

    public static Material GetMaterialByName(
      this Materials materials,
      string materialName )
    {
      return materials
        .OfType<Material>()
        .FirstOrDefault(
          m => m.Name.Equals( materialName ) );
    }
  }
#endif // BEFORE_REVIT_2013
  #endregion // Victor sample code 2
}
