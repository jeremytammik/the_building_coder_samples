#region Header
//
// JtParamValuesForCats.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018-2019 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Container class for Revit database element 
  /// parameter data 'param_values'. 
  /// 'param_values' is a list of strings 'name = value'.
  /// Each element is identified by its UniqueId string 'uid'.
  /// Each category is equipped with a dictionary mapping 
  /// 'uid' to 'param_values'.
  /// </summary>
  class JtParamValuesForCats :

    // Parameter names are not guaranteed to be 
    // unique! Therefore, it may be impossible to
    // include all parameter values in a dictionary
    // using the parameter name as a key.

#if PARAMETER_NAMES_ARE_UNIQUE
    Dictionary<string,
      Dictionary<string,
        Dictionary<string, string>>>
#else // unfortunately, parameter names are not unique:
    Dictionary<string,
      Dictionary<string,
        List<string>>>
#endif // PARAMETER_NAMES_ARE_UNIQUE

  {
    /// <summary>
    /// Return all the parameter values  
    /// deemed relevant for the given element
    /// in string form.
    /// </summary>
    static List<string> GetParamValues( Element e )
    {
      // Two choices: 
      // Element.Parameters property -- Retrieves 
      // a set containing all the parameters.
      // GetOrderedParameters method -- Gets the 
      // visible parameters in order.

      IList<Parameter> ps = e.GetOrderedParameters();

      List<string> param_values = new List<string>(
        ps.Count );

      foreach( Parameter p in ps )
      {
        // AsValueString displays the value as the 
        // user sees it. In some cases, the underlying
        // database value returned by AsInteger, AsDouble,
        // etc., may be more relevant.

        param_values.Add( string.Format( "{0} = {1}",
          p.Definition.Name, p.AsValueString() ) );
      }
      return param_values;
    }

    /// <summary>
    /// Return parameter data for all  
    /// elements of all the given categories
    /// </summary>
    static void GetParamValuesForCats(
      Dictionary<string, Dictionary<string, List<string>>>
        map_cat_to_uid_to_param_values,
      Document doc,
      BuiltInCategory[] cats )
    {
      // One top level dictionary per category

      foreach( BuiltInCategory cat in cats )
      {
        map_cat_to_uid_to_param_values.Add(
          cat.Description(),
          new Dictionary<string,
            List<string>>() );
      }

      // Collect all required elements

      // The FilterCategoryRule as used here seems to 
      // have no filtering effect at all! 
      // It passes every single element, afaict. 

      List<ElementId> ids
        = new List<BuiltInCategory>( cats )
          .ConvertAll<ElementId>( c
            => new ElementId( (int) c ) );

      FilterCategoryRule r
        = new FilterCategoryRule( ids );

      ElementParameterFilter f
        = new ElementParameterFilter( r, true );

      // Use a logical OR of category filters

      IList<ElementFilter> a
        = new List<ElementFilter>( cats.Length );

      foreach( BuiltInCategory bic in cats )
      {
        a.Add( new ElementCategoryFilter( bic ) );
      }

      LogicalOrFilter categoryFilter
        = new LogicalOrFilter( a );

      // Run the collector

      FilteredElementCollector els
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .WherePasses( categoryFilter );

      // Retrieve parameter data for each element

      foreach( Element e in els )
      {
        Category cat = e.Category;
        if( null == cat )
        {
          Debug.Print(
            "element {0} {1} has null category",
            e.Id, e.Name );
          continue;
        }
        List<string> param_values = GetParamValues( e );

        BuiltInCategory bic = (BuiltInCategory)
          ( e.Category.Id.IntegerValue );

        string catkey = bic.Description();
        string uid = e.UniqueId;

        map_cat_to_uid_to_param_values[catkey].Add(
          uid, param_values );
      }
    }

    /// <summary>
    /// Constructor.
    /// Input: document and list of categories;
    /// Output: dict mapping categories to dicts
    /// of element unique ids with list of param
    /// data for each element.
    /// </summary>
    public JtParamValuesForCats( 
      Document doc,
      BuiltInCategory[] categories )
    {
      GetParamValuesForCats( this, doc, categories );
    }

#if DEBUG
    /// <summary>
    /// Print categories, number of elements each and
    /// some sample data to the Visual Studio debug
    /// output window.
    /// </summary>
    public void DebugPrint()
    {
      List<string> keys = new List<string>(
        this.Keys );
      keys.Sort();

      foreach( string key in keys )
      {
        Dictionary<string, List<string>> els
          = this[key];

        int n = els.Count;

        Debug.Print( "{0} ({1} element{2}){3}",
          key, n, Util.PluralSuffix( n ),
          Util.DotOrColon( n ) );

        if( 0 < n )
        {
          List<string> uids = new List<string>( els.Keys );
          string uid = uids[0];

          List<string> param_values = els[uid];
          param_values.Sort();

          n = param_values.Count;

          Debug.Print( "  first element {0} has {1} parameter{2}{3}",
            uid, n, Util.PluralSuffix( n ),
            Util.DotOrColon( n ) );

          param_values.ForEach( pv
            => Debug.Print( "    " + pv ) );
        }
      }
    }
#endif // DEBUG

  }
}
