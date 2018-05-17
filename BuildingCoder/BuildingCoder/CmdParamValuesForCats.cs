#region Header
//
// CmdParamValuesForCats.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018 Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdParamValuesForCats : IExternalCommand
  {
    /// <summary>
    /// List all built-in categories of interest
    /// </summary>
    static BuiltInCategory[] _cats =
    {
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Rooms,
      BuiltInCategory.OST_Windows
    };

    /// <summary>
    /// Return all the parameter values  
    /// deemed relevant for the given element
    /// in string form.
    /// </summary>
    List<string> GetParamValues( Element e )
    {
      // Two choices: 
      // Element.Parameters property -- Retrieves 
      // a set containing all the parameters.
      // GetOrderedParameters method -- Gets the 
      // visible parameters in order.

      IList<Parameter> ps = e.GetOrderedParameters();

      List<string> param_values = new List<string>( 
        ps.Count );

      foreach( Parameter p in ps)
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
    Dictionary<string, Dictionary<string, List<string>>>
      GetParamValuesForCats(
        Document doc, 
        BuiltInCategory[] cats )
    {
      // Set up the return value dictionary

      Dictionary<string,
        Dictionary<string,
          List<string>>>
            map_cat_to_uid_to_param_values
              = new Dictionary<string,
                Dictionary<string,
                  List<string>>>();

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
          (e.Category.Id.IntegerValue);

        string catkey = bic.Description();
        string uid = e.UniqueId;

        map_cat_to_uid_to_param_values[catkey].Add( 
          uid, param_values );
      }
      return map_cat_to_uid_to_param_values;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Parameter names are not guaranteed to be 
      // unique! Therefore, it may be impossible to
      // include all parameter values in a dictionary
      // using the parameter name as a key.

#if PARAMETER_NAMES_ARE_UNIQUE
      Dictionary<string,
        Dictionary<string,
          Dictionary<string, string>>>
            map_cat_to_uid_to_param_values;
#else // unfortunately, parameter names are not unique:
      Dictionary<string,
        Dictionary<string,
          List<string>>>
            map_cat_to_uid_to_param_values;
#endif // PARAMETER_NAMES_ARE_UNIQUE

      map_cat_to_uid_to_param_values 
        = GetParamValuesForCats( doc, _cats );

#if DEBUG
      List<string> keys = new List<string>( 
        map_cat_to_uid_to_param_values.Keys );
      keys.Sort();

      foreach( string key in keys )
      {
        Dictionary<string, List<string>> els 
          = map_cat_to_uid_to_param_values[key];

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
#endif // DEBUG

      return Result.Succeeded;
    }
  }
}
