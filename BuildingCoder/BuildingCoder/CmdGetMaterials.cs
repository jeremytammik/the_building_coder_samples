#region Header
//
// CmdGetMaterials.cs - determine element materials
// by iterating over its geometry faces
//
// Copyright (C) 2008-2017 by Jeremy Tammik,
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
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.ComponentModel;
//using Autodesk.Revit.Utility;
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
    #region List Material Asset Sub-Texture
    /// <summary>
    /// A description of a property consists of a name, its attributes and value
    /// here AssetPropertyPropertyDescriptor is used to wrap AssetProperty 
    /// to display its name and value in PropertyGrid
    /// </summary>
    internal class AssetPropertyPropertyDescriptor : PropertyDescriptor
    {
      #region Fields
      /// <summary>
      /// A reference to an AssetProperty
      /// </summary>
      private AssetProperty m_assetProperty;

      /// <summary>
      /// The type of AssetProperty's property "Value"
      /// </summary>m
      private Type m_valueType;

      /// <summary>
      /// The value of AssetProperty's property "Value"
      /// </summary>
      private Object m_value;
      #endregion

      #region Properties
      /// <summary>
      /// Property to get internal AssetProperty
      /// </summary>
      public AssetProperty AssetProperty
      {
        get { return m_assetProperty; }
      }
      #endregion

      #region override Properties
      /// <summary>
      /// Gets a value indicating whether this property is read-only
      /// </summary>
      public override bool IsReadOnly
      {
        get
        {
          return true;
        }
      }

      /// <summary>
      /// Gets the type of the component this property is bound to. 
      /// </summary>
      public override Type ComponentType
      {
        get
        {
          return m_assetProperty.GetType();
        }
      }

      /// <summary>
      /// Gets the type of the property. 
      /// </summary>
      public override Type PropertyType
      {
        get
        {
          return m_valueType;
        }
      }
      #endregion

      /// <summary>
      /// Public class constructor
      /// </summary>
      /// <param name="assetProperty">the AssetProperty which a AssetPropertyPropertyDescriptor instance describes</param>
      public AssetPropertyPropertyDescriptor( AssetProperty assetProperty )
          : base( assetProperty.Name, new Attribute[0] )
      {
        m_assetProperty = assetProperty;
      }

      #region override methods
      /// <summary>
      /// Compares this to another object to see if they are equivalent
      /// </summary>
      /// <param name="obj">The object to compare to this AssetPropertyPropertyDescriptor. </param>
      /// <returns></returns>
      public override bool Equals( object obj )
      {
        AssetPropertyPropertyDescriptor other = obj as AssetPropertyPropertyDescriptor;
        return other != null && other.AssetProperty.Equals( m_assetProperty );
      }

      /// <summary>
      /// Returns the hash code for this object.
      /// Here override the method "Equals", so it is necessary to override GetHashCode too.
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
        return m_assetProperty.GetHashCode();
      }

      /// <summary>
      /// Resets the value for this property of the component to the default value. 
      /// </summary>
      /// <param name="component">The component with the property value that is to be reset to the default value.</param>
      public override void ResetValue( object component )
      {

      }

      /// <summary>
      /// Returns whether resetting an object changes its value. 
      /// </summary>
      /// <param name="component">The component to test for reset capability.</param>
      /// <returns>true if resetting the component changes its value; otherwise, false.</returns>
      public override bool CanResetValue( object component )
      {
        return false;
      }

      /// <summary>G
      /// Determines a value indicating whether the value of this property needs to be persisted.
      /// </summary>
      /// <param name="component">The component with the property to be examined for persistence.</param>
      /// <returns>true if the property should be persisted; otherwise, false.</returns>
      public override bool ShouldSerializeValue( object component )
      {
        return false;
      }

      /// <summary>
      /// Gets the current value of the property on a component.
      /// </summary>
      /// <param name="component">The component with the property for which to retrieve the value.</param>
      /// <returns>The value of a property for a given component.</returns>
      public override object GetValue( object component )
      {
        Tuple<Type, Object> typeAndValue = GetTypeAndValue( m_assetProperty, 0 );
        m_value = typeAndValue.Item2;
        m_valueType = typeAndValue.Item1;

        return m_value;
      }

      private static Tuple<Type, Object> GetTypeAndValue( AssetProperty assetProperty, int level )
      {
        Object theValue;
        Type valueType;
        //For each AssetProperty, it has different type and value
        //must deal with it separately
        try
        {
          if( assetProperty is AssetPropertyBoolean )
          {
            AssetPropertyBoolean property = assetProperty as AssetPropertyBoolean;
            valueType = typeof( AssetPropertyBoolean );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyDistance )
          {
            AssetPropertyDistance property = assetProperty as AssetPropertyDistance;
            valueType = typeof( AssetPropertyDistance );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyDouble )
          {
            AssetPropertyDouble property = assetProperty as AssetPropertyDouble;
            valueType = typeof( AssetPropertyDouble );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyDoubleArray2d )
          {
            //Default, it is supported by PropertyGrid to display Double []
            //Try to convert DoubleArray to Double []
            AssetPropertyDoubleArray2d property = assetProperty as AssetPropertyDoubleArray2d;
            valueType = typeof( AssetPropertyDoubleArray2d );
            theValue = GetSystemArrayAsString( property.Value );
          }
          else if( assetProperty is AssetPropertyDoubleArray3d )
          {
            AssetPropertyDoubleArray3d property = assetProperty as AssetPropertyDoubleArray3d;
            valueType = typeof( AssetPropertyDoubleArray3d );
            theValue = GetSystemArrayAsString( property.Value ); // 2017
          }
          else if( assetProperty is AssetPropertyDoubleArray4d )
          {
            AssetPropertyDoubleArray4d property = assetProperty as AssetPropertyDoubleArray4d;
            valueType = typeof( AssetPropertyDoubleArray4d );
            theValue = GetSystemArrayAsString( property.Value );
          }
          else if( assetProperty is AssetPropertyDoubleMatrix44 )
          {
            AssetPropertyDoubleMatrix44 property = assetProperty as AssetPropertyDoubleMatrix44;
            valueType = typeof( AssetPropertyDoubleMatrix44 );
            theValue = GetSystemArrayAsString( property.Value );
          }
          else if( assetProperty is AssetPropertyEnum )
          {
            AssetPropertyEnum property = assetProperty as AssetPropertyEnum;
            valueType = typeof( AssetPropertyEnum );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyFloat )
          {
            AssetPropertyFloat property = assetProperty as AssetPropertyFloat;
            valueType = typeof( AssetPropertyFloat );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyInteger )
          {
            AssetPropertyInteger property = assetProperty as AssetPropertyInteger;
            valueType = typeof( AssetPropertyInteger );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyReference )
          {
            AssetPropertyReference property = assetProperty as AssetPropertyReference;
            valueType = typeof( AssetPropertyReference );
            theValue = "REFERENCE"; //property.Type;
          }
          else if( assetProperty is AssetPropertyString )
          {
            AssetPropertyString property = assetProperty as AssetPropertyString;
            valueType = typeof( AssetPropertyString );
            theValue = property.Value;
          }
          else if( assetProperty is AssetPropertyTime )
          {
            AssetPropertyTime property = assetProperty as AssetPropertyTime;
            valueType = typeof( AssetPropertyTime );
            theValue = property.Value;
          }
          else
          {
            valueType = typeof( String );
            theValue = "Unprocessed asset type: " + assetProperty.GetType().Name;
          }

          if( assetProperty.NumberOfConnectedProperties > 0 )
          {

            String result = "";
            result = theValue.ToString();

            TaskDialog.Show( "Connected properties found", assetProperty.Name + ": " + assetProperty.NumberOfConnectedProperties );
            IList<AssetProperty> properties = assetProperty.GetAllConnectedProperties();

            foreach( AssetProperty property in properties )
            {
              if( property is Asset )
              {
                // Nested?
                Asset asset = property as Asset;
                int size = asset.Size;
                for( int i = 0; i < size; i++ )
                {
                  AssetProperty subproperty = asset[i];
                  Tuple<Type, Object> valueAndType = GetTypeAndValue( subproperty, level + 1 );
                  String indent = "";
                  if( level > 0 )
                  {
                    for( int iLevel = 1; iLevel <= level; iLevel++ )
                      indent += "   ";
                  }
                  result += "\n " + indent + "- connected: name: " + subproperty.Name + " | type: " + valueAndType.Item1.Name +
                    " | value: " + valueAndType.Item2.ToString();
                }
              }
            }

            theValue = result;
          }
        }
        catch
        {
          return null;
        }
        return new Tuple<Type, Object>( valueType, theValue );
      }

      /// <summary>
      /// Sets the value of the component to a different value.
      /// For AssetProperty, it is not allowed to set its value, so here just return.
      /// </summary>
      /// <param name="component">The component with the property value that is to be set. </param>
      /// <param name="value">The new value.</param>
      public override void SetValue( object component, object value )
      {
        return;
      }
      #endregion

      /// <summary>
      /// Convert Autodesk.Revit.DB.DoubleArray to Double [].
      /// For Double [] is supported by PropertyGrid.
      /// </summary>
      /// <param name="doubleArray">the original Autodesk.Revit.DB.DoubleArray </param>
      /// <returns>The converted Double []</returns>
      private static Double[] GetSystemArray( DoubleArray doubleArray )
      {
        double[] values = new double[doubleArray.Size];
        int index = 0;
        foreach( Double value in doubleArray )
        {
          values[index++] = value;
        }
        return values;
      }

      private static String GetSystemArrayAsString( DoubleArray doubleArray )
      {
        double[] values = GetSystemArray( doubleArray );

        String result = "";
        foreach( double d in values )
        {
          result += d;
          result += ",";
        }

        return result;
      }
    }

    /// <summary>
    /// supplies dynamic custom type information for an Asset while it is displayed in PropertyGrid.
    /// </summary>
    public class RenderAppearanceDescriptor : ICustomTypeDescriptor
    {
      #region Fields
      /// <summary>
      /// Reference to Asset
      /// </summary>
      Asset m_asset;

      /// <summary>
      /// Asset's property descriptors
      /// </summary>
      PropertyDescriptorCollection m_propertyDescriptors;
      #endregion

      #region Constructors
      /// <summary>
      /// Initializes Asset object
      /// </summary>
      /// <param name="asset">an Asset object</param>
      public RenderAppearanceDescriptor( Asset asset )
      {
        m_asset = asset;
        GetAssetProperties();
      }

      #endregion

      #region Methods
      #region ICustomTypeDescriptor Members

      /// <summary>
      /// Returns a collection of custom attributes for this instance of Asset.
      /// </summary>
      /// <returns>Asset's attributes</returns>
      public AttributeCollection GetAttributes()
      {
        return TypeDescriptor.GetAttributes( m_asset, false );
      }

      /// <summary>
      /// Returns the class name of this instance of Asset.
      /// </summary>
      /// <returns>Asset's class name</returns>
      public string GetClassName()
      {
        return TypeDescriptor.GetClassName( m_asset, false );
      }

      /// <summary>
      /// Returns the name of this instance of Asset.
      /// </summary>
      /// <returns>The name of Asset</returns>
      public string GetComponentName()
      {
        return TypeDescriptor.GetComponentName( m_asset, false );
      }

      /// <summary>
      /// Returns a type converter for this instance of Asset.
      /// </summary>
      /// <returns>The converter of the Asset</returns>
      public TypeConverter GetConverter()
      {
        return TypeDescriptor.GetConverter( m_asset, false );
      }

      /// <summary>
      /// Returns the default event for this instance of Asset.
      /// </summary>
      /// <returns>An EventDescriptor that represents the default event for this object, 
      /// or null if this object does not have events.</returns>
      public EventDescriptor GetDefaultEvent()
      {
        return TypeDescriptor.GetDefaultEvent( m_asset, false );
      }

      /// <summary>
      /// Returns the default property for this instance of Asset.
      /// </summary>
      /// <returns>A PropertyDescriptor that represents the default property for this object, 
      /// or null if this object does not have properties.</returns>
      public PropertyDescriptor GetDefaultProperty()
      {
        return TypeDescriptor.GetDefaultProperty( m_asset, false );
      }

      /// <summary>
      /// Returns an editor of the specified type for this instance of Asset.
      /// </summary>
      /// <param name="editorBaseType">A Type that represents the editor for this object. </param>
      /// <returns>An Object of the specified type that is the editor for this object, 
      /// or null if the editor cannot be found.</returns>
      public object GetEditor( Type editorBaseType )
      {
        return TypeDescriptor.GetEditor( m_asset, editorBaseType, false );
      }

      /// <summary>
      /// Returns the events for this instance of Asset using the specified attribute array as a filter.
      /// </summary>
      /// <param name="attributes">An array of type Attribute that is used as a filter. </param>
      /// <returns>An EventDescriptorCollection that represents the filtered events for this Asset instance.</returns>
      public EventDescriptorCollection GetEvents( Attribute[] attributes )
      {
        return TypeDescriptor.GetEvents( m_asset, attributes, false );
      }

      /// <summary>
      /// Returns the events for this instance of Asset.
      /// </summary>
      /// <returns>An EventDescriptorCollection that represents the events for this Asset instance.</returns>
      public EventDescriptorCollection GetEvents()
      {
        return TypeDescriptor.GetEvents( m_asset, false );
      }

      /// <summary>
      /// Returns the properties for this instance of Asset using the attribute array as a filter.
      /// </summary>
      /// <param name="attributes">An array of type Attribute that is used as a filter.</param>
      /// <returns>A PropertyDescriptorCollection that 
      /// represents the filtered properties for this Asset instance.</returns>
      public PropertyDescriptorCollection GetProperties( Attribute[] attributes )
      {
        return m_propertyDescriptors;
      }

      /// <summary>
      /// Returns the properties for this instance of Asset.
      /// </summary>
      /// <returns>A PropertyDescriptorCollection that represents the properties 
      /// for this Asset instance.</returns>
      public PropertyDescriptorCollection GetProperties()
      {
        return m_propertyDescriptors;
      }

      /// <summary>
      /// Returns an object that contains the property described by the specified property descriptor.
      /// </summary>
      /// <param name="pd">A PropertyDescriptor that represents the property whose owner is to be found. </param>
      /// <returns>Asset object</returns>
      public object GetPropertyOwner( PropertyDescriptor pd )
      {
        return m_asset;
      }
      #endregion

      /// <summary>
      /// Get Asset's property descriptors
      /// </summary>
      private void GetAssetProperties()
      {
        if( null == m_propertyDescriptors )
        {
          m_propertyDescriptors = new PropertyDescriptorCollection( new AssetPropertyPropertyDescriptor[0] );
        }
        else
        {
          return;
        }

        //For each AssetProperty in Asset, create an AssetPropertyPropertyDescriptor.
        //It means that each AssetProperty will be a property of Asset
        for( int index = 0; index < m_asset.Size; index++ )
        {
          AssetProperty assetProperty = m_asset[index];
          if( null != assetProperty )
          {
            AssetPropertyPropertyDescriptor assetPropertyPropertyDescriptor = new AssetPropertyPropertyDescriptor( assetProperty );
            m_propertyDescriptors.Add( assetPropertyPropertyDescriptor );
          }
        }
      }
      #endregion
    }

    public void ShowMaterialInfo( Document doc )
    {
      // Find material
      FilteredElementCollector fec = new FilteredElementCollector( doc );
      fec.OfClass( typeof( Material ) );

      String materialName = "Checker"; // "Copper";//"Prism - Glass - Textured"; // "Parking Stripe"; // "Copper";
                                       // "Carpet (1)";// "Prism - Glass - Textured";// "Parking Stripe"; // "Prism 1";// "Brick, Common" ;// "Acoustic Ceiling Tile 24 x 48";  // "Aluminum"
      Material mat = fec.Cast<Material>().First<Material>( m => m.Name == materialName );



      ElementId appearanceAssetId = mat.AppearanceAssetId;

      AppearanceAssetElement appearanceAsset = doc.GetElement( appearanceAssetId ) as AppearanceAssetElement;

      Asset renderingAsset = appearanceAsset.GetRenderingAsset();



      RenderAppearanceDescriptor rad
            = new RenderAppearanceDescriptor( renderingAsset );


      PropertyDescriptorCollection collection = rad.GetProperties();

      TaskDialog.Show( "Total properties", "Properties found: " + collection.Count );
      //

      string s = ": Material Asset Properties";

      TaskDialog dlg = new TaskDialog( s );

      dlg.MainInstruction = mat.Name + s;

      s = string.Empty;

      List<PropertyDescriptor> orderableCollection = new List<PropertyDescriptor>( collection.Count );

      foreach( PropertyDescriptor descr in collection )
      {
        orderableCollection.Add( descr );
      }



      foreach( AssetPropertyPropertyDescriptor descr in
               orderableCollection.OrderBy<PropertyDescriptor, String>( pd => pd.Name ).Cast<AssetPropertyPropertyDescriptor>() )
      {
        object value = descr.GetValue( rad );

        s += "\nname: " + descr.Name
          + " | type: " + descr.PropertyType.Name
         + " | value: " + value;
      }
      dlg.MainContent = s;
      dlg.Show();


    }

    public void ListAllAssets(UIApplication uiapp)
    {
      AssetSet assets = uiapp.Application.get_Assets( AssetType.Appearance );

      TaskDialog dlg = new TaskDialog( "Assets" );

      String assetLabel = "";

      foreach( Asset asset in assets )
      {
        String libraryName = asset.LibraryName;
        AssetPropertyString uiname = asset["UIName"] as AssetPropertyString;
        AssetPropertyString baseSchema = asset["BaseSchema"] as AssetPropertyString;

        assetLabel += libraryName + " | " + uiname.Value + " | " + baseSchema.Value;
        assetLabel += "\n";
      }

      dlg.MainInstruction = assetLabel;

      dlg.Show();
    }
    #endregion // List Material Asset Sub-Texture

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
      ICollection<ElementId> ids = sel.GetElementIds();
      Options opt = app.Application.Create.NewGeometryOptions();
      Material mat;
      string msg = string.Empty;
      int i, n;

      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        // For 0310_ensure_material.htm:

        if( e is FamilyInstance )
        {
          mat = GetMaterial( doc, e as FamilyInstance );

          Util.InfoMsg(
            "Family instance element material: "
            + ( null == mat ? "<null>" : mat.Name ) );
        }

        GeometryElement geo = e.get_Geometry( opt );

        // If you are not interested in duplicate
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
      var tx = new Transaction( doc, "Get Material" );
      tx.Start();

      // ******
      // NullReferencException throws here if
      // Materials collection contains null
      // ******
      var material = doc.Settings.Materials.get_Item( materialName );

      tx.RollBack();

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
