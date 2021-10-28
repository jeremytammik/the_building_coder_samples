#region Header

//
// CmdSharedParamModelGroup.cs - create a shared
// parameter for the doors, walls, inserted DWG,
// model groups, and model lines.
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdCreateSharedParams : IExternalCommand
    {
        private const string _filename = "C:/tmp/SharedParams.txt";
        private const string _groupname = "The Building Coder Parameters";

        private const string _defname = "SP";

        //ParameterType _deftype = ParameterType.Number; // 2021
        private readonly ForgeTypeId _deftype = SpecTypeId.Number; // 2022

        // What element types are we interested in? The standard
        // SDK FireRating sample uses BuiltInCategory.OST_Doors.

        // We can also use BuiltInCategory.OST_Walls to demonstrate
        // that the same technique works with system families just
        // as well as with standard ones.

        // To test attaching shared parameters to inserted DWG files,
        // which generate their own category on the fly, we can also
        // identify the category by category name instead of built-
        // in category enumeration, as discussed in

        // http://thebuildingcoder.typepad.com/blog/2008/11/adding-a-shared-parameter-to-a-dwg-file.html

        // We can attach shared parameters to model groups.
        // Unfortunately, this does not work in the
        // same way as the others, because we cannot retrieve the
        // category from the document Settings.Categories collection.

        // In that case, we can obtain the category from an existing
        // instance of a group.

        private readonly BuiltInCategory[] targets =
        {
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Walls,
            //"Drawing1.dwg", // inserted DWG file
            BuiltInCategory.OST_IOSModelGroups, // doc.Settings.Categories.get_Item returns null
            //"Model Groups", // doc.Settings.Categories.get_Item with this argument throws an exception SystemInvalidOperationException "Operation is not valid due to the current state of the object."
            BuiltInCategory.OST_Lines // model lines
        };

        private Category GetCategory(
            Document doc,
            BuiltInCategory target)
        {
            Category cat = null;

            if (target.Equals(BuiltInCategory.OST_IOSModelGroups))
            {
                // determine model group category:

                var collector
                    = Util.GetElementsOfType(doc, typeof(Group), // GroupType works as well
                        BuiltInCategory.OST_IOSModelGroups);

                var modelGroups = collector.ToElements();

                if (0 == modelGroups.Count)
                {
                    Util.ErrorMsg("Please insert a model group.");
                    return cat;
                }

                cat = modelGroups[0].Category;
            }
            else
            {
                try
                {
                    cat = doc.Settings.Categories.get_Item(target);
                }
                catch (Exception ex)
                {
                    Util.ErrorMsg($"Error obtaining document {target.ToString()} category: {ex.Message}");
                    return cat;
                }
            }

            if (null == cat)
                Util.ErrorMsg($"Unable to obtain the document {target.ToString()} category.");
            return cat;
        }

        /// <summary>
        ///     Create a new shared parameter
        /// </summary>
        /// <param name="doc">Document</param>
        /// <param name="cat">Category to bind the parameter definition</param>
        /// <param name="nameSuffix">Parameter name suffix</param>
        /// <param name="typeParameter">Create a type parameter? If not, it is an instance parameter.</param>
        /// <returns></returns>
        private bool CreateSharedParameter(
            Document doc,
            Category cat,
            int nameSuffix,
            bool typeParameter)
        {
            var app = doc.Application;

            var ca
                = app.Create;

            // get or set the current shared params filename:

            var filename
                = app.SharedParametersFilename;

            if (0 == filename.Length)
            {
                var path = _filename;
                StreamWriter stream;
                stream = new StreamWriter(path);
                stream.Close();
                app.SharedParametersFilename = path;
                filename = app.SharedParametersFilename;
            }

            // get the current shared params file object:

            var file
                = app.OpenSharedParameterFile();

            if (null == file)
            {
                Util.ErrorMsg(
                    "Error getting the shared params file.");

                return false;
            }

            // get or create the shared params group:

            var group
                = file.Groups.get_Item(_groupname);

            if (null == group) group = file.Groups.Create(_groupname);

            if (null == group)
            {
                Util.ErrorMsg(
                    "Error getting the shared params group.");

                return false;
            }

            // set visibility of the new parameter:

            // Category.AllowsBoundParameters property
            // indicates if a category can have user-visible
            // shared or project parameters. If it is false,
            // it may not be bound to visible shared params
            // using the BindingMap. Please note that
            // non-user-visible parameters can still be
            // bound to these categories.

            var visible = cat.AllowsBoundParameters;

            // get or create the shared params definition:

            var defname = _defname + nameSuffix;

            var definition = group.Definitions.get_Item(
                defname);

            if (null == definition)
            {
                //definition = group.Definitions.Create( defname, _deftype, visible ); // 2014

                var opt
                    = new ExternalDefinitionCreationOptions(
                        defname, _deftype);

                opt.Visible = visible;

                definition = group.Definitions.Create(opt); // 2015
            }

            if (null == definition)
            {
                Util.ErrorMsg(
                    "Error creating shared parameter.");

                return false;
            }

            // create the category set containing our category for binding:

            var catSet = ca.NewCategorySet();
            catSet.Insert(cat);

            // bind the param:

            try
            {
                var binding = typeParameter
                    ? ca.NewTypeBinding(catSet)
                    : ca.NewInstanceBinding(catSet) as Binding;

                // we could check if it is already bound,
                // but it looks like insert will just ignore
                // it in that case:

                doc.ParameterBindings.Insert(definition, binding);

                // we can also specify the parameter group here:

                //doc.ParameterBindings.Insert( definition, binding,
                //  BuiltInParameterGroup.PG_GEOMETRY );

                Debug.Print(
                    "Created a shared {0} parameter '{1}' for the {2} category.",
                    typeParameter ? "type" : "instance",
                    defname, cat.Name);
            }
            catch (Exception ex)
            {
                Util.ErrorMsg($"Error binding shared parameter to category {cat.Name}: {ex.Message}");
                return false;
            }

            return true;
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            using var t = new Transaction(doc);
            t.Start("Create Shared Parameter");
            Category cat;
            var i = 0;

            // create instance parameters:

            foreach (var target in targets)
            {
                cat = GetCategory(doc, target);
                if (null != cat) CreateSharedParameter(doc, cat, ++i, false);
            }

            // create a type parameter:

            cat = GetCategory(doc, BuiltInCategory.OST_Walls);
            CreateSharedParameter(doc, cat, ++i, true);
            t.Commit();

            return Result.Succeeded;
        }

        #region REINSERT

#if REINSERT
    public static BindSharedParamResult BindSharedParam(
      Document doc,
      Category cat,
      string paramName,
      string grpName,
      ParameterType paramType,
      bool visible,
      bool instanceBinding )
    {
      try // generic
      {
        Application app = doc.Application;

        // This is needed already here to 
        // store old ones for re-inserting

        CategorySet catSet = app.Create.NewCategorySet();

        // Loop all Binding Definitions
        // IMPORTANT NOTE: Categories.Size is ALWAYS 1 !?
        // For multiple categories, there is really one 
        // pair per each category, even though the 
        // Definitions are the same...

        DefinitionBindingMapIterator iter
          = doc.ParameterBindings.ForwardIterator();

        while( iter.MoveNext() )
        {
          Definition def = iter.Key;
          ElementBinding elemBind
            = (ElementBinding) iter.Current;

          // Got param name match

          if( paramName.Equals( def.Name,
            StringComparison.CurrentCultureIgnoreCase ) )
          {
            // Check for category match - Size is always 1!

            if( elemBind.Categories.Contains( cat ) )
            {
              // Check Param Type

              if( paramType != def.ParameterType )
                return BindSharedParamResult.eWrongParamType;

              // Check Binding Type

              if( instanceBinding )
              {
                if( elemBind.GetType() != typeof( InstanceBinding ) )
                  return BindSharedParamResult.eWrongBindingType;
              }
              else
              {
                if( elemBind.GetType() != typeof( TypeBinding ) )
                  return BindSharedParamResult.eWrongBindingType;
              }

              // Check Visibility - cannot (not exposed)
              // If here, everything is fine, 
              // ie already defined correctly

              return BindSharedParamResult.eAlreadyBound;
            }

            // If here, no category match, hence must 
            // store "other" cats for re-inserting

            else
            {
              foreach( Category catOld
                in elemBind.Categories )
                catSet.Insert( catOld ); // 1 only, but no index...
            }
          }
        }

        // If here, there is no Binding Definition for 
        // it, so make sure Param defined and then bind it!

        DefinitionFile defFile
          = GetOrCreateSharedParamsFile( app );

        DefinitionGroup defGrp
          = GetOrCreateSharedParamsGroup(
            defFile, grpName );

        Definition definition
          = GetOrCreateSharedParamDefinition(
            defGrp, paramType, paramName, visible );

        catSet.Insert( cat );

        InstanceBinding bind = null;

        if( instanceBinding )
        {
          bind = app.Create.NewInstanceBinding(
            catSet );
        }
        else
        {
          bind = app.Create.NewTypeBinding( catSet );
        }

        // There is another strange API "feature". 
        // If param has EVER been bound in a project 
        // (in above iter pairs or even if not there 
        // but once deleted), Insert always fails!? 
        // Must use .ReInsert in that case.
        // See also similar findings on this topic in: 
        // http://thebuildingcoder.typepad.com/blog/2009/09/adding-a-category-to-a-parameter-binding.html 
        // - the code-idiom below may be more generic:

        if( doc.ParameterBindings.Insert(
          definition, bind ) )
        {
          return BindSharedParamResult.eSuccessfullyBound;
        }
        else
        {
          if( doc.ParameterBindings.ReInsert(
            definition, bind ) )
          {
            return BindSharedParamResult.eSuccessfullyBound;
          }
          else
          {
            return BindSharedParamResult.eFailed;
          }
        }
      }
      catch( Exception ex )
      {
        MessageBox.Show( string.Format(
          "Error in Binding Shared Param: {0}",
          ex.Message ) );

        return BindSharedParamResult.eFailed;
      }
    }
#endif // REINSERT

        #endregion // REINSERT

        #region SetAllowVaryBetweenGroups

        /// <summary>
        ///     Helper method to control `SetAllowVaryBetweenGroups`
        ///     option for instance binding param
        /// </summary>
        private static void SetInstanceParamVaryBetweenGroupsBehaviour(
            Document doc,
            Guid guid,
            bool allowVaryBetweenGroups = true)
        {
            try // last resort
            {
                var sp
                    = SharedParameterElement.Lookup(doc, guid);

                // Should never happen as we will call 
                // this only for *existing* shared param.

                if (null == sp) return;

                var def = sp.GetDefinition();

                if (def.VariesAcrossGroups != allowVaryBetweenGroups)
                    // Must be within an outer transaction!

                    def.SetAllowVaryBetweenGroups(doc, allowVaryBetweenGroups);
            }
            catch
            {
            } // ideally, should report something to log...
        }

#if SetInstanceParamVaryBetweenGroupsBehaviour_SAMPLE_CALL
    // Assumes outer transaction
    public static Parameter GetOrCreateElemSharedParam( 
      Element elem,
      string paramName,
      string grpName,
      ParameterType paramType,
      bool visible,
      bool instanceBinding,
      bool userModifiable,
      Guid guid,
      bool useTempSharedParamFile,
      string tooltip = "",
      BuiltInParameterGroup uiGrp = BuiltInParameterGroup.INVALID,
      bool allowVaryBetweenGroups = true )
    {
      try
      {
        // Check if existing
        Parameter param = elem.LookupParameter( paramName );
        if( null != param )
        {
          // NOTE: If you don't want forcefully setting 
          // the "old" instance params to 
          // allowVaryBetweenGroups =true,
          // just comment the next 3 lines.
          if( instanceBinding && allowVaryBetweenGroups )
          {
            SetInstanceParamVaryBetweenGroupsBehaviour( 
              elem.Document, guid, allowVaryBetweenGroups );
          }
          return param;
        }

        // If here, need to create it (my custom 
        // implementation and classes…)

        BindSharedParamResult res = BindSharedParam( 
          elem.Document, elem.Category, paramName, grpName,
          paramType, visible, instanceBinding, userModifiable,
          guid, useTempSharedParamFile, tooltip, uiGrp );

        if( res != BindSharedParamResult.eSuccessfullyBound
          && res != BindSharedParamResult.eAlreadyBound )
        {
          return null;
        }

        // Set AllowVaryBetweenGroups for NEW Instance 
        // Binding Shared Param

        if( instanceBinding )
        {
          SetInstanceParamVaryBetweenGroupsBehaviour( 
            elem.Document, guid, allowVaryBetweenGroups );
        }

        // If here, binding is OK and param seems to be
        // IMMEDIATELY available from the very same command

        return elem.LookupParameter( paramName );
      }
      catch( Exception ex )
      {
        System.Windows.Forms.MessageBox.Show( 
          string.Format( 
            "Error in getting or creating Element Param: {0}", 
            ex.Message ) );

        return null;
      }
    }
#endif // SetInstanceParamVaryBetweenGroupsBehaviour_SAMPLE_CALL

        #endregion // SetAllowVaryBetweenGroups

        #region Modify Many Shared Parameter Values

        // for https://forums.autodesk.com/t5/revit-api-forum/modify-shared-parameters-for-high-number-of-family-instance/m-p/9727166
        private class IdForSynchro
        {
            public ElementId RevitId { get; set; }
            public int Param1 { get; set; }
            public string Param2 { get; set; }
            public double Param3 { get; set; }
        }

        private void modifyParameterValues(Document doc, IList<IdForSynchro> data)
        {
            using var tr = new Transaction(doc);
            var guid1 = Guid.Empty;
            var guid2 = Guid.Empty;
            var guid3 = Guid.Empty;

            tr.Start("synchro");

            foreach (var d in data) // Main.idForSynchro is the collection of data
            {
                var e = doc.GetElement(d.RevitId);

                if (Guid.Empty == guid1)
                {
                    guid1 = e.LookupParameter("PLUGIN_PARAM1").GUID;
                    guid2 = e.LookupParameter("PLUGIN_PARAM2").GUID;
                    guid3 = e.LookupParameter("PLUGIN_PARAM3").GUID;
                }

                e.get_Parameter(guid1).Set(d.Param1);
                e.get_Parameter(guid2).Set(d.Param2);
                e.get_Parameter(guid3).Set(d.Param3);
            }

            tr.Commit();
        }

        #endregion // Modify Many Shared Parameter Values
    }
}