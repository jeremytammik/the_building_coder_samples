#region Header

//
// CmdParamValuesForCats.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdParamValuesForCats : IExternalCommand
    {
        /// <summary>
        ///     List all built-in categories of interest
        /// </summary>
        private static readonly BuiltInCategory[] _categories =
        {
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Rooms,
            BuiltInCategory.OST_Windows
        };

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            #region Obsolete inline code

#if NEED_ALL_THE_INLINE_CODE
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
#endif

            #endregion // Obsolete inline code

            var data
                = new JtParamValuesForCats(doc, _categories);

#if DEBUG
            data.DebugPrint();
#endif // DEBUG

            return Result.Succeeded;
        }
    }
}