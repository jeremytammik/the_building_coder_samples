#region Header

//
// CmdGetMaterials.cs - determine element materials
// by iterating over its geometry faces
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    #region FindTextureBitmapPaths

    internal class FindTextureBitmapPathsWrapper
    {
        private readonly string[] targetMaterialNames =
        {
            // A standard Revit material, with 
            // textures in standard paths. 
            "Brick, Common",

            // A material with a single image from 
            // another non-material library path
            "Local Path Material"
        };

        private void FindTextureBitmapPaths(Document doc)
        {
            // Find materials
            var fec
                = new FilteredElementCollector(doc);

            fec.OfClass(typeof(Material));

            var targetMaterials
                = fec.Cast<Material>().Where(mtl =>
                    targetMaterialNames.Contains(mtl.Name));

            foreach (var material in targetMaterials)
            {
                // Get appearance asset for read
                var appearanceAssetId = material
                    .AppearanceAssetId;

                var appearanceAssetElem
                    = doc.GetElement(appearanceAssetId)
                        as AppearanceAssetElement;

                var asset = appearanceAssetElem
                    .GetRenderingAsset();

                // Walk through all first level assets to find 
                // connected Bitmap properties.  Note: it is 
                // possible to have multilevel connected 
                // properties with Bitmaps in the leaf nodes.  
                // So this would need to be recursive.

                var size = asset.Size;
                for (var assetIdx = 0; assetIdx < size; assetIdx++)
                {
                    //AssetProperty aProperty = asset[assetIdx]; // 2018
                    var aProperty = asset.Get(assetIdx); // 2019

                    if (aProperty.NumberOfConnectedProperties < 1)
                        continue;

                    // Find first connected property.  
                    // Should work for all current (2018) schemas.  
                    // Safer code would loop through all connected
                    // properties based on the number provided.

                    var connectedAsset = aProperty
                        .GetConnectedProperty(0) as Asset;

                    // We are only checking for bitmap connected assets. 

                    if (connectedAsset.Name == "UnifiedBitmapSchema")
                    {
                        // This line is 2018.1 & up because of the 
                        // property reference to UnifiedBitmap
                        // .UnifiedbitmapBitmap.  In earlier versions,
                        // you can still reference the string name 
                        // instead: "unifiedbitmap_Bitmap"

                        //AssetPropertyString path = connectedAsset[ // 2018
                        //  UnifiedBitmap.UnifiedbitmapBitmap]
                        //    as AssetPropertyString;

                        var path = connectedAsset // 2019
                                .FindByName(UnifiedBitmap.UnifiedbitmapBitmap)
                            as AssetPropertyString;

                        // This will be a relative path to the 
                        // built -in materials folder, addiitonal 
                        // render appearance folder, or an 
                        // absolute path.

                        TaskDialog.Show("Connected bitmap",
                            string.Format("{0} from {2}: {1}",
                                aProperty.Name, path.Value,
                                connectedAsset.LibraryName));
                    }
                }
            }
        }
    }

    #endregion // FindTextureBitmapPaths

    #region Victor sample code

    public class ElementComparer : IEqualityComparer<Element>
    {
        public bool Equals(Element x, Element y)
        {
            if (x == null || y == null)
                return false;

            return x.Id.IntegerValue == y.Id.IntegerValue && x.GetType() == y.GetType() && x.Document.Equals(y.Document);
        }

        public int GetHashCode(Element obj)
        {
            return obj.UniqueId.GetHashCode();
        }
    }

    public class MaterialComparer : IEqualityComparer<Material>
    {
        public bool Equals(Material x, Material y)
        {
            return x.UniqueId.Equals(y.UniqueId);
        }

        public int GetHashCode(Material obj)
        {
            return obj.UniqueId.GetHashCode();
        }
    }

    #endregion // Victor sample code

    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdGetMaterials : IExternalCommand
    {
        #region List Material Asset Sub-Texture

        /// <summary>
        ///     A description of a property consists of a name, its attributes and value
        ///     here AssetPropertyPropertyDescriptor is used to wrap AssetProperty
        ///     to display its name and value in PropertyGrid
        /// </summary>
        internal class AssetPropertyPropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            ///     Public class constructor
            /// </summary>
            /// <param name="assetProperty">the AssetProperty which a AssetPropertyPropertyDescriptor instance describes</param>
            public AssetPropertyPropertyDescriptor(AssetProperty assetProperty)
                : base(assetProperty.Name, Array.Empty<Attribute>())
            {
                m_assetProperty = assetProperty;
            }

            #region Properties

            /// <summary>
            ///     Property to get internal AssetProperty
            /// </summary>
            public AssetProperty AssetProperty => m_assetProperty;

            #endregion

            /// <summary>
            ///     Convert Autodesk.Revit.DB.DoubleArray to Double [].
            ///     For Double [] is supported by PropertyGrid.
            /// </summary>
            /// <param name="doubleArray">the original Autodesk.Revit.DB.DoubleArray </param>
            /// <returns>The converted Double []</returns>
            private static double[] GetSystemArray(DoubleArray doubleArray)
            {
                var values = new double[doubleArray.Size];
                var index = 0;
                foreach (double value in doubleArray) values[index++] = value;
                return values;
            }

            private static string GetSystemArrayAsString(DoubleArray doubleArray)
            {
                var values = GetSystemArray(doubleArray);

                //String result = "";
                //foreach( double d in values )
                //{
                //  result += d;
                //  result += ",";
                //}

                //return result;

                return string.Join(",",
                    values.Select(
                        x => x.ToString()));
            }

            #region Fields

            /// <summary>
            ///     A reference to an AssetProperty
            /// </summary>
            private readonly AssetProperty m_assetProperty;

            /// <summary>
            ///     The type of AssetProperty's property "Value"
            /// </summary>
            /// m
            private Type m_valueType;

            /// <summary>
            ///     The value of AssetProperty's property "Value"
            /// </summary>
            private object m_value;

            #endregion

            #region override Properties

            /// <summary>
            ///     Gets a value indicating whether this property is read-only
            /// </summary>
            public override bool IsReadOnly => true;

            /// <summary>
            ///     Gets the type of the component this property is bound to.
            /// </summary>
            public override Type ComponentType => m_assetProperty.GetType();

            /// <summary>
            ///     Gets the type of the property.
            /// </summary>
            public override Type PropertyType => m_valueType;

            #endregion

            #region override methods

            /// <summary>
            ///     Compares this to another object to see if they are equivalent
            /// </summary>
            /// <param name="obj">The object to compare to this AssetPropertyPropertyDescriptor. </param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return obj is AssetPropertyPropertyDescriptor other && other.AssetProperty.Equals(m_assetProperty);
            }

            /// <summary>
            ///     Returns the hash code for this object.
            ///     Here override the method "Equals", so it is necessary to override GetHashCode too.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return m_assetProperty.GetHashCode();
            }

            /// <summary>
            ///     Resets the value for this property of the component to the default value.
            /// </summary>
            /// <param name="component">The component with the property value that is to be reset to the default value.</param>
            public override void ResetValue(object component)
            {
            }

            /// <summary>
            ///     Returns whether resetting an object changes its value.
            /// </summary>
            /// <param name="component">The component to test for reset capability.</param>
            /// <returns>true if resetting the component changes its value; otherwise, false.</returns>
            public override bool CanResetValue(object component)
            {
                return false;
            }

            /// <summary>
            ///     G
            ///     Determines a value indicating whether the value of this property needs to be persisted.
            /// </summary>
            /// <param name="component">The component with the property to be examined for persistence.</param>
            /// <returns>true if the property should be persisted; otherwise, false.</returns>
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }

            /// <summary>
            ///     Gets the current value of the property on a component.
            /// </summary>
            /// <param name="component">The component with the property for which to retrieve the value.</param>
            /// <returns>The value of a property for a given component.</returns>
            public override object GetValue(object component)
            {
                var typeAndValue = GetTypeAndValue(m_assetProperty, 0);
                m_value = typeAndValue.Item2;
                m_valueType = typeAndValue.Item1;

                return m_value;
            }

            private static Tuple<Type, object> GetTypeAndValue(AssetProperty assetProperty, int level)
            {
                object theValue;
                Type valueType;
                //For each AssetProperty, it has different type and value
                //must deal with it separately
                try
                {
                    if (assetProperty is AssetPropertyBoolean boolean)
                    {
                        valueType = typeof(AssetPropertyBoolean);
                        theValue = boolean.Value;
                    }
                    else if (assetProperty is AssetPropertyDistance distance)
                    {
                        valueType = typeof(AssetPropertyDistance);
                        theValue = distance.Value;
                    }
                    else if (assetProperty is AssetPropertyDouble d)
                    {
                        valueType = typeof(AssetPropertyDouble);
                        theValue = d.Value;
                    }
                    else if (assetProperty is AssetPropertyDoubleArray2d array2d)
                    {
                        //Default, it is supported by PropertyGrid to display Double []
                        //Try to convert DoubleArray to Double []
                        valueType = typeof(AssetPropertyDoubleArray2d);
                        theValue = GetSystemArrayAsString(array2d.Value);
                    }
                    else if (assetProperty is AssetPropertyDoubleArray3d array3d)
                    {
                        valueType = typeof(AssetPropertyDoubleArray3d);
                        //theValue = GetSystemArrayAsString( property.Value ); // 2017
                        theValue = Util.DoubleArrayString(array3d.GetValueAsDoubles()); // 2018
                    }
                    else if (assetProperty is AssetPropertyDoubleArray4d array4d)
                    {
                        valueType = typeof(AssetPropertyDoubleArray4d);
                        //theValue = GetSystemArrayAsString( property.Value ); // 2017
                        theValue = Util.DoubleArrayString(array4d.GetValueAsDoubles()); // 2018
                    }
                    else if (assetProperty is AssetPropertyDoubleMatrix44 matrix44)
                    {
                        valueType = typeof(AssetPropertyDoubleMatrix44);
                        theValue = GetSystemArrayAsString(matrix44.Value);
                    }
                    else if (assetProperty is AssetPropertyEnum @enum)
                    {
                        valueType = typeof(AssetPropertyEnum);
                        theValue = @enum.Value;
                    }
                    else if (assetProperty is AssetPropertyFloat f)
                    {
                        valueType = typeof(AssetPropertyFloat);
                        theValue = f.Value;
                    }
                    else if (assetProperty is AssetPropertyInteger integer)
                    {
                        valueType = typeof(AssetPropertyInteger);
                        theValue = integer.Value;
                    }
                    else if (assetProperty is AssetPropertyReference reference)
                    {
                        valueType = typeof(AssetPropertyReference);
                        theValue = "REFERENCE"; //property.Type;
                    }
                    else if (assetProperty is AssetPropertyString s)
                    {
                        valueType = typeof(AssetPropertyString);
                        theValue = s.Value;
                    }
                    else if (assetProperty is AssetPropertyTime property)
                    {
                        valueType = typeof(AssetPropertyTime);
                        theValue = property.Value;
                    }
                    else
                    {
                        valueType = typeof(string);
                        theValue = $"Unprocessed asset type: {assetProperty.GetType().Name}";
                    }

                    if (assetProperty.NumberOfConnectedProperties > 0)
                    {
                        var result = "";
                        result = theValue.ToString();

                        TaskDialog.Show("Connected properties found", $"{assetProperty.Name}: {assetProperty.NumberOfConnectedProperties}");
                        var properties = assetProperty.GetAllConnectedProperties();

                        foreach (var property in properties)
                            if (property is Asset asset)
                            {
                                // Nested?
                                var size = asset.Size;
                                for (var i = 0; i < size; i++)
                                {
                                    //AssetProperty subproperty = asset[i]; // 2018
                                    var subproperty = asset.Get(i); // 2019
                                    var valueAndType = GetTypeAndValue(subproperty, level + 1);
                                    var indent = "";
                                    if (level > 0)
                                        for (var iLevel = 1; iLevel <= level; iLevel++)
                                            indent += "   ";
                                    result += $"\n {indent}- connected: name: {subproperty.Name} | type: {valueAndType.Item1.Name} | value: {valueAndType.Item2}";
                                }
                            }

                        theValue = result;
                    }
                }
                catch
                {
                    return null;
                }

                return new Tuple<Type, object>(valueType, theValue);
            }

            /// <summary>
            ///     Sets the value of the component to a different value.
            ///     For AssetProperty, it is not allowed to set its value, so here just return.
            /// </summary>
            /// <param name="component">The component with the property value that is to be set. </param>
            /// <param name="value">The new value.</param>
            public override void SetValue(object component, object value)
            {
            }

            #endregion
        }

        /// <summary>
        ///     supplies dynamic custom type information for an Asset while it is displayed in PropertyGrid.
        /// </summary>
        public class RenderAppearanceDescriptor : ICustomTypeDescriptor
        {
            #region Constructors

            /// <summary>
            ///     Initializes Asset object
            /// </summary>
            /// <param name="asset">an Asset object</param>
            public RenderAppearanceDescriptor(Asset asset)
            {
                m_asset = asset;
                GetAssetProperties();
            }

            #endregion

            #region Fields

            /// <summary>
            ///     Reference to Asset
            /// </summary>
            private readonly Asset m_asset;

            /// <summary>
            ///     Asset's property descriptors
            /// </summary>
            private PropertyDescriptorCollection m_propertyDescriptors;

            #endregion

            #region Methods

            #region ICustomTypeDescriptor Members

            /// <summary>
            ///     Returns a collection of custom attributes for this instance of Asset.
            /// </summary>
            /// <returns>Asset's attributes</returns>
            public AttributeCollection GetAttributes()
            {
                return TypeDescriptor.GetAttributes(m_asset, false);
            }

            /// <summary>
            ///     Returns the class name of this instance of Asset.
            /// </summary>
            /// <returns>Asset's class name</returns>
            public string GetClassName()
            {
                return TypeDescriptor.GetClassName(m_asset, false);
            }

            /// <summary>
            ///     Returns the name of this instance of Asset.
            /// </summary>
            /// <returns>The name of Asset</returns>
            public string GetComponentName()
            {
                return TypeDescriptor.GetComponentName(m_asset, false);
            }

            /// <summary>
            ///     Returns a type converter for this instance of Asset.
            /// </summary>
            /// <returns>The converter of the Asset</returns>
            public TypeConverter GetConverter()
            {
                return TypeDescriptor.GetConverter(m_asset, false);
            }

            /// <summary>
            ///     Returns the default event for this instance of Asset.
            /// </summary>
            /// <returns>
            ///     An EventDescriptor that represents the default event for this object,
            ///     or null if this object does not have events.
            /// </returns>
            public EventDescriptor GetDefaultEvent()
            {
                return TypeDescriptor.GetDefaultEvent(m_asset, false);
            }

            /// <summary>
            ///     Returns the default property for this instance of Asset.
            /// </summary>
            /// <returns>
            ///     A PropertyDescriptor that represents the default property for this object,
            ///     or null if this object does not have properties.
            /// </returns>
            public PropertyDescriptor GetDefaultProperty()
            {
                return TypeDescriptor.GetDefaultProperty(m_asset, false);
            }

            /// <summary>
            ///     Returns an editor of the specified type for this instance of Asset.
            /// </summary>
            /// <param name="editorBaseType">A Type that represents the editor for this object. </param>
            /// <returns>
            ///     An Object of the specified type that is the editor for this object,
            ///     or null if the editor cannot be found.
            /// </returns>
            public object GetEditor(Type editorBaseType)
            {
                return TypeDescriptor.GetEditor(m_asset, editorBaseType, false);
            }

            /// <summary>
            ///     Returns the events for this instance of Asset using the specified attribute array as a filter.
            /// </summary>
            /// <param name="attributes">An array of type Attribute that is used as a filter. </param>
            /// <returns>An EventDescriptorCollection that represents the filtered events for this Asset instance.</returns>
            public EventDescriptorCollection GetEvents(Attribute[] attributes)
            {
                return TypeDescriptor.GetEvents(m_asset, attributes, false);
            }

            /// <summary>
            ///     Returns the events for this instance of Asset.
            /// </summary>
            /// <returns>An EventDescriptorCollection that represents the events for this Asset instance.</returns>
            public EventDescriptorCollection GetEvents()
            {
                return TypeDescriptor.GetEvents(m_asset, false);
            }

            /// <summary>
            ///     Returns the properties for this instance of Asset using the attribute array as a filter.
            /// </summary>
            /// <param name="attributes">An array of type Attribute that is used as a filter.</param>
            /// <returns>
            ///     A PropertyDescriptorCollection that
            ///     represents the filtered properties for this Asset instance.
            /// </returns>
            public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                return m_propertyDescriptors;
            }

            /// <summary>
            ///     Returns the properties for this instance of Asset.
            /// </summary>
            /// <returns>
            ///     A PropertyDescriptorCollection that represents the properties
            ///     for this Asset instance.
            /// </returns>
            public PropertyDescriptorCollection GetProperties()
            {
                return m_propertyDescriptors;
            }

            /// <summary>
            ///     Returns an object that contains the property described by the specified property descriptor.
            /// </summary>
            /// <param name="pd">A PropertyDescriptor that represents the property whose owner is to be found. </param>
            /// <returns>Asset object</returns>
            public object GetPropertyOwner(PropertyDescriptor pd)
            {
                return m_asset;
            }

            #endregion

            /// <summary>
            ///     Get Asset's property descriptors
            /// </summary>
            private void GetAssetProperties()
            {
                if (null == m_propertyDescriptors)
                    m_propertyDescriptors = new PropertyDescriptorCollection(Array.Empty<AssetPropertyPropertyDescriptor>());
                else
                    return;

                //For each AssetProperty in Asset, create an AssetPropertyPropertyDescriptor.
                //It means that each AssetProperty will be a property of Asset
                for (var index = 0; index < m_asset.Size; index++)
                {
                    //AssetProperty assetProperty = m_asset[index]; // 2018
                    var assetProperty = m_asset.Get(index); // 2019

                    if (null != assetProperty)
                    {
                        var assetPropertyPropertyDescriptor = new AssetPropertyPropertyDescriptor(assetProperty);
                        m_propertyDescriptors.Add(assetPropertyPropertyDescriptor);
                    }
                }
            }

            #endregion
        }

        public void ShowMaterialInfo(Document doc)
        {
            // Find material
            var fec = new FilteredElementCollector(doc);
            fec.OfClass(typeof(Material));

            var materialName = "Checker"; // "Copper";//"Prism - Glass - Textured"; // "Parking Stripe"; // "Copper";
            // "Carpet (1)";// "Prism - Glass - Textured";// "Parking Stripe"; // "Prism 1";// "Brick, Common" ;// "Acoustic Ceiling Tile 24 x 48";  // "Aluminum"
            var mat = fec.Cast<Material>().First(m => m.Name == materialName);

            var appearanceAssetId = mat.AppearanceAssetId;

            var appearanceAsset = doc.GetElement(appearanceAssetId) as AppearanceAssetElement;

            var renderingAsset = appearanceAsset.GetRenderingAsset();

            var rad
                = new RenderAppearanceDescriptor(renderingAsset);

            var collection = rad.GetProperties();

            TaskDialog.Show("Total properties", $"Properties found: {collection.Count}");

            var s = ": Material Asset Properties";

            var dlg = new TaskDialog(s);

            dlg.MainInstruction = mat.Name + s;

            s = string.Empty;

            var orderableCollection = new List<PropertyDescriptor>(collection.Count);

            foreach (PropertyDescriptor descr in collection) orderableCollection.Add(descr);

            foreach (var descr in
                orderableCollection.OrderBy(pd => pd.Name).Cast<AssetPropertyPropertyDescriptor>())
            {
                var value = descr.GetValue(rad);

                s += $"\nname: {descr.Name} | type: {descr.PropertyType.Name} | value: {value}";
            }

            dlg.MainContent = s;
            dlg.Show();
        }

        //public void ListAllAssets(UIApplication uiapp)
        //{
        //  AssetSet assets = uiapp.Application.get_Assets( // Revit 2019
        //    AssetType.Appearance);
        //  TaskDialog dlg = new TaskDialog("Assets");
        //  String assetLabel = "";
        //  foreach (Asset asset in assets)
        //  {
        //    String libraryName = asset.LibraryName;
        //    //AssetPropertyString uiname = asset["UIName"] as AssetPropertyString; // 2018
        //    AssetPropertyString uiname = asset.FindByName("UIName") as AssetPropertyString; // 2019
        //    //AssetPropertyString baseSchema = asset["BaseSchema"] as AssetPropertyString; // 2018
        //    AssetPropertyString baseSchema = asset.FindByName("BaseSchema") as AssetPropertyString; // 2019
        //    assetLabel += libraryName + " | " + uiname.Value + " | " + baseSchema.Value;
        //    assetLabel += "\n";
        //  }
        //  dlg.MainInstruction = assetLabel;
        //  dlg.Show();
        //}

        #endregion // List Material Asset Sub-Texture

        #region Victor sample code

        private class ElementMaterial
        {
            public ElementMaterial(Element element, Material material)
            {
                if (element == null)
                    throw new ArgumentNullException("element");
                if (material == null)
                    throw new ArgumentNullException("material");
                Element = element;
                Material = material;
            }

            public Material Material { get; }

            public Element Element { get; }
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
        ///     Return a list of the document's materials
        ///     from a filtered element collector.
        /// </summary>
        private FilteredElementCollector FilterForMaterials(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Material));
        }

        /// <summary>
        ///     Replacement for deprecated property
        ///     Face.MaterialElement to access material name
        ///     for a given geometry face.
        /// </summary>
        private static string FaceMaterialName(
            Document doc,
            Face face)
        {
            var id = face.MaterialElementId;
            var m = doc.GetElement(id) as Material;
            return m.Name;
        }

        /// <summary>
        ///     Return family instance element material, either
        ///     for the given instance or the entire category.
        ///     If no element material is specified and the
        ///     ByCategory material information is null, set
        ///     it to a valid value at the category level.
        /// </summary>
        public Material GetMaterial(
            Document doc,
            FamilyInstance fi)
        {
            Material material = null;

            foreach (Parameter p in fi.Parameters)
            {
                var def = p.Definition;

                // the material is stored as element id:

                if (p.StorageType == StorageType.ElementId
                    && def.ParameterGroup == BuiltInParameterGroup.PG_MATERIALS
                    //&& def.ParameterType == ParameterType.Material // 2021)
                    && def.GetDataType() == SpecTypeId.Reference.Material) // 2022
                {
                    var materialId = p.AsElementId();

                    if (-1 == materialId.IntegerValue)
                        // invalid element id, so we assume
                        // the material is "By Category":

                        if (null != fi.Category)
                        {
                            material = fi.Category.Material;

                            if (null == material)
                            {
                                //MaterialOther mat
                                //  = doc.Settings.Materials.AddOther(
                                //    "GoodConditionMat" ); // 2011

                                var id = Material.Create(doc, "GoodConditionMat"); // 2012
                                var mat = doc.GetElement(id) as Material;

                                mat.Color = new Color(255, 0, 0);

                                fi.Category.Material = mat;

                                material = fi.Category.Material;
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
        ///     Return a list of all materials by recursively traversing
        ///     all the object's solids' faces, retrieving their face
        ///     materials, and collecting them in a list.
        ///     Original implementation, not always robust as noted by Andras Kiss in
        ///     http://thebuildingcoder.typepad.com/blog/2008/10/family-instance-materials.html?cid=6a00e553e1689788330115713e2e3c970b#comment-6a00e553e1689788330115713e2e3c970b
        /// </summary>
        public List<string> GetMaterials1(
            Document doc,
            GeometryElement geo)
        {
            var materials = new List<string>();

            //foreach( GeometryObject o in geo.Objects ) // 2012

            foreach (var o in geo) // 2013
                if (o is Solid solid)
                    foreach (Face face in solid.Faces)
                    {
                        //string s = face.MaterialElement.Name; // 2011
                        var s = FaceMaterialName(doc, face); // 2012
                        materials.Add(s);
                    }
                else if (o is GeometryInstance instance)
                    materials.AddRange(GetMaterials1(
                        doc, instance.SymbolGeometry));

            return materials;
        }

        /// <summary>
        ///     Return a list of all materials by traversing
        ///     all the object's and its instances' solids'
        ///     faces, retrieving their face materials,
        ///     and collecting them in a list.
        ///     Enhanced more robust implementation suggested
        ///     by Harry Mattison, but lacking the recursion
        ///     of the first version.
        /// </summary>
        public List<string> GetMaterials(
            Document doc,
            GeometryElement geo)
        {
            var materials = new List<string>();

            //foreach( GeometryObject o in geo.Objects ) // 2012

            foreach (var o in geo) // 2013
                if (o is Solid solid1)
                {
                    if (null != solid1)
                        foreach (Face face in solid1.Faces)
                        {
                            //string s = face.MaterialElement.Name; // 2011
                            var s = FaceMaterialName(doc, face); // 2012
                            materials.Add(s);
                        }
                }
                else if (o is GeometryInstance instance)
                {
                    //foreach( Object geomObj in i.SymbolGeometry.Objects ) // 2012
                    foreach (object geomObj in instance.SymbolGeometry) // 2013
                    {
                        var solid = geomObj as Solid;
                        if (solid != null)
                            foreach (Face face in solid.Faces)
                            {
                                //string s = face.MaterialElement.Name; // 2011
                                var s = FaceMaterialName(doc, face); // 2012
                                materials.Add(s);
                            }
                    }
                }

            return materials;
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;
            var sel = uidoc.Selection;
            var ids = sel.GetElementIds();
            var opt = app.Application.Create.NewGeometryOptions();
            Material mat;
            var msg = string.Empty;
            int i, n;

            foreach (var id in ids)
            {
                var e = doc.GetElement(id);

                // For 0310_ensure_material.htm:

                if (e is FamilyInstance instance)
                {
                    mat = GetMaterial(doc, instance);

                    Util.InfoMsg(
                        $"Family instance element material: {(null == mat ? "<null>" : mat.Name)}");
                }

                var geo = e.get_Geometry(opt);

                // If you are not interested in duplicate
                // materials, you can define a class that
                // overloads the Add method to only insert
                // a new entry if its value is not already
                // present in the list, instead of using
                // the standard List<> class:

                var materials = GetMaterials(doc, geo);

                msg += $"\n{Util.ElementDescription(e)}";

                n = materials.Count;

                if (0 == n)
                {
                    msg += " has no materials.";
                }
                else
                {
                    i = 0;

                    msg += $" has {n} material{Util.PluralSuffix(n)}:";

                    foreach (var s in materials)
                        msg += $"\n  {i++} {s}";
                }
            }

            if (0 == msg.Length) msg = "Please select some elements.";

            Util.InfoMsg(msg);

            return Result.Succeeded;
        }

        #region Access All Material Asset Properties

        private void GetElementMaterialInfo(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(Material));

            try
            {
                foreach (Material material in collector)
                    if (material.Name.Equals("Air"))
                    {
                        var appearanceElement
                            = doc.GetElement(material.AppearanceAssetId)
                                as AppearanceAssetElement;

                        var appearanceAsset = appearanceElement
                            .GetRenderingAsset();

                        var assetProperties
                            = new List<AssetProperty>();

                        var physicalPropSet
                            = doc.GetElement(material.StructuralAssetId)
                                as PropertySetElement;

                        var thermalPropSet
                            = doc.GetElement(material.ThermalAssetId)
                                as PropertySetElement;

                        var thermalAsset = thermalPropSet
                            .GetThermalAsset();

                        var physicalAsset = physicalPropSet
                            .GetStructuralAsset();

                        ICollection<Parameter> physicalParameters
                            = physicalPropSet.GetOrderedParameters();

                        ICollection<Parameter> thermalParameters
                            = thermalPropSet.GetOrderedParameters();

                        // Appearance Asset

                        for (var i = 0; i < appearanceAsset.Size; i++)
                        {
                            var property = appearanceAsset[i];
                            assetProperties.Add(property);
                        }

                        foreach (var assetProp in assetProperties)
                        {
                            var type = assetProp.GetType();
                            object assetPropValue = null;
                            var prop = type.GetProperty("Value");
                            if (prop != null
                                && prop.GetIndexParameters().Length == 0)
                                assetPropValue = prop.GetValue(assetProp);
                        }

                        // Physical (Structural) Asset

                        foreach (var p in physicalParameters)
                        {
                            // Work with parameters here
                            // The only parameter not in the orderedParameters 
                            // that is needed is the Asset name, which you 
                            // can get by 'physicalAsset.Name'.
                        }

                        // Thermal Asset

                        foreach (var p in thermalParameters)
                        {
                            //Work with parameters here
                            //The only parameter not in the orderedParameters 
                            // that is needed is the Asset name, shich you 
                            // can get by 'thermalAsset.Name'.
                        }
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #endregion // Access All Material Asset Properties

        #region Set material appearance asset keyword property

        //I have an issue setting a string value to the material appearance asset keyword property.
        //In one material, it can be set as expected, but another material returns an error saying, "The input value is invalid for this AssetPropertyString property.\r\nParameter name: value".
        //I found the blog article and JIRA ticket REVIT-170824 which explains that the keyword property on the Identity tab is not exposed yet.
        //https://thebuildingcoder.typepad.com/blog/2019/11/material-physical-and-thermal-assets.html#4
        //https://jira.autodesk.com/browse/REVIT-170824
        //I expect the "keyword" property on the appearance tab to accept a string value.
        //In addition, I can see some error message in the journal file.
        //Is it possible to set the "keyword" property of the appearance asset?
        /// <summary>
        ///     Set material appearance asset keyword property
        /// </summary>
        private void SetMaterialAppearanceAssetKeywordProperty(
            AppearanceAssetElement assetElem,
            string new_keyword)
        {
            var doc = assetElem.Document;

            //FilteredElementCollector materialCollector
            //  = new FilteredElementCollector( doc )
            //    .OfCategory( BuiltInCategory.OST_Materials )
            //    .OfClass( typeof( Material ) );
            //Material material = null;
            //foreach( Element e in materialCollector )
            //{
            //  if( e.Name == "HC_CB" )
            //  {
            //    material = e as Material;
            //  }
            //}
            //AppearanceAssetElement assetElem 
            //  = doc.GetElement( material.AppearanceAssetId ) 
            //    as AppearanceAssetElement;

            using var tx = new Transaction(doc);
            tx.Start("Transaction Set Keyword");
            using (var editScope
                = new AppearanceAssetEditScope(assetElem.Document))
            {
                var editableAsset = editScope.Start(assetElem.Id);

                try
                {
                    var parameter = editableAsset.FindByName("keyword");
                    if (parameter is AssetPropertyString propKeyword)
                    {
                        if (string.IsNullOrEmpty(propKeyword.Value))
                        {
                            propKeyword.Value = new_keyword;
                        }
                        else
                        {
                            if (!propKeyword.Value.Contains(new_keyword))
                            {
                                var val = $"{propKeyword.Value}: {new_keyword}";
                                propKeyword.Value = val;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                editScope.Commit(true);
            }

            tx.Commit();
        }

        #endregion // Set material appearance asset keyword property
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