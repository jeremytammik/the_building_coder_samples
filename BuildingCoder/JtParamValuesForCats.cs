#region Header

//
// JtParamValuesForCats.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    ///     Container class for Revit database element
    ///     parameter data 'param_values'.
    ///     'param_values' is a list of strings 'name = value'.
    ///     Each element is identified by its UniqueId string 'uid'.
    ///     Each category is equipped with a dictionary mapping
    ///     'uid' to 'param_values'.
    /// </summary>
    internal class JtParamValuesForCats :

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
        ///     Return all the parameter values
        ///     deemed relevant for the given element
        ///     in string form.
        /// </summary>
        private static List<string> GetParamValues(Element e)
        {
            // Two choices: 
            // Element.Parameters property -- Retrieves 
            // a set containing all the parameters.
            // GetOrderedParameters method -- Gets the 
            // visible parameters in order.

            var ps = e.GetOrderedParameters();

            var param_values = new List<string>(
                ps.Count);

            foreach (var p in ps)
                // AsValueString displays the value as the 
                // user sees it. In some cases, the underlying
                // database value returned by AsInteger, AsDouble,
                // etc., may be more relevant.

                param_values.Add($"{p.Definition.Name} = {p.AsValueString()}");
            return param_values;
        }

        /// <summary>
        ///     Return parameter data for all
        ///     elements of all the given categories
        /// </summary>
        private static void GetParamValuesForCats(
            Dictionary<string, Dictionary<string, List<string>>>
                map_cat_to_uid_to_param_values,
            Document doc,
            BuiltInCategory[] cats)
        {
            // One top level dictionary per category

            foreach (var cat in cats)
                map_cat_to_uid_to_param_values.Add(
                    cat.Description(),
                    new Dictionary<string,
                        List<string>>());

            // Collect all required elements

            // The FilterCategoryRule as used here seems to 
            // have no filtering effect at all! 
            // It passes every single element, afaict. 

            var ids
                = new List<BuiltInCategory>(cats)
                    .ConvertAll(c
                        => new ElementId((int) c));

            var r
                = new FilterCategoryRule(ids);

            var f
                = new ElementParameterFilter(r, true);

            // Use a logical OR of category filters

            IList<ElementFilter> a
                = new List<ElementFilter>(cats.Length);

            foreach (var bic in cats) a.Add(new ElementCategoryFilter(bic));

            var categoryFilter
                = new LogicalOrFilter(a);

            // Run the collector

            var els
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .WherePasses(categoryFilter);

            // Retrieve parameter data for each element

            foreach (var e in els)
            {
                var cat = e.Category;
                if (null == cat)
                {
                    Debug.Print(
                        "element {0} {1} has null category",
                        e.Id, e.Name);
                    continue;
                }

                var param_values = GetParamValues(e);

                var bic = (BuiltInCategory)
                    e.Category.Id.IntegerValue;

                var catkey = bic.Description();
                var uid = e.UniqueId;

                map_cat_to_uid_to_param_values[catkey].Add(
                    uid, param_values);
            }
        }

        /// <summary>
        ///     Constructor.
        ///     Input: document and list of categories;
        ///     Output: dict mapping categories to dicts
        ///     of element unique ids with list of param
        ///     data for each element.
        /// </summary>
        public JtParamValuesForCats(
            Document doc,
            BuiltInCategory[] categories)
        {
            GetParamValuesForCats(this, doc, categories);
        }

#if DEBUG
        /// <summary>
        ///     Print categories, number of elements each and
        ///     some sample data to the Visual Studio debug
        ///     output window.
        /// </summary>
        public void DebugPrint()
        {
            var keys = new List<string>(
                Keys);
            keys.Sort();

            foreach (var key in keys)
            {
                var els
                    = this[key];

                var n = els.Count;

                Debug.Print("{0} ({1} element{2}){3}",
                    key, n, Util.PluralSuffix(n),
                    Util.DotOrColon(n));

                if (0 < n)
                {
                    var uids = new List<string>(els.Keys);
                    var uid = uids[0];

                    var param_values = els[uid];
                    param_values.Sort();

                    n = param_values.Count;

                    Debug.Print("  first element {0} has {1} parameter{2}{3}",
                        uid, n, Util.PluralSuffix(n),
                        Util.DotOrColon(n));

                    param_values.ForEach(pv
                        => Debug.Print($"    {pv}"));
                }
            }
        }
#endif // DEBUG
    }
}