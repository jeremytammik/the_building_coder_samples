#region Header
//
// CmdSharedParamModelGroup.cs - create a shared
// parameter for the doors, walls, inserted DWG,
// model groups, and model lines.
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdCreateSharedParams : IExternalCommand
  {
    const string _filename = "C:/tmp/SharedParams.txt";
    const string _groupname = "The Building Coder Parameters";
    const string _defname = "SP";
    ParameterType _deftype = ParameterType.Number;

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

    BuiltInCategory[] targets = new BuiltInCategory[] {
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Walls,
      //"Drawing1.dwg", // inserted DWG file
      BuiltInCategory.OST_IOSModelGroups, // doc.Settings.Categories.get_Item returns null
      //"Model Groups", // doc.Settings.Categories.get_Item with this argument throws an exception SystemInvalidOperationException "Operation is not valid due to the current state of the object."
      BuiltInCategory.OST_Lines // model lines
    };

    Category GetCategory(
      Document doc,
      BuiltInCategory target )
    {
      Category cat = null;

      if( target.Equals( BuiltInCategory.OST_IOSModelGroups ) )
      {
        // determine model group category:

        FilteredElementCollector collector
          = Util.GetElementsOfType( doc, typeof( Group ), // GroupType works as well
            BuiltInCategory.OST_IOSModelGroups );

        IList<Element> modelGroups = collector.ToElements();

        if( 0 == modelGroups.Count )
        {
          Util.ErrorMsg( "Please insert a model group." );
          return cat;
        }
        else
        {
          cat = modelGroups[0].Category;
        }
      }
      else
      {
        try
        {
          cat = doc.Settings.Categories.get_Item( target );
        }
        catch( Exception ex )
        {
          Util.ErrorMsg( string.Format(
            "Error obtaining document {0} category: {1}",
            target.ToString(), ex.Message ) );
          return cat;
        }
      }
      if( null == cat )
      {
        Util.ErrorMsg( string.Format(
          "Unable to obtain the document {0} category.",
          target.ToString() ) );
      }
      return cat;
    }

    /// <summary>
    /// Create a new shared parameter
    /// </summary>
    /// <param name="doc">Document</param>
    /// <param name="cat">Category to bind the parameter definition</param>
    /// <param name="nameSuffix">Parameter name suffix</param>
    /// <param name="typeParameter">Create a type parameter? If not, it is an instance parameter.</param>
    /// <returns></returns>
    bool CreateSharedParameter(
      Document doc,
      Category cat,
      int nameSuffix,
      bool typeParameter )
    {
      Application app = doc.Application;

      Autodesk.Revit.Creation.Application ca
        = app.Create;

      // get or set the current shared params filename:

      string filename
        = app.SharedParametersFilename;

      if( 0 == filename.Length )
      {
        string path = _filename;
        StreamWriter stream;
        stream = new StreamWriter( path );
        stream.Close();
        app.SharedParametersFilename = path;
        filename = app.SharedParametersFilename;
      }

      // get the current shared params file object:

      DefinitionFile file
        = app.OpenSharedParameterFile();

      if( null == file )
      {
        Util.ErrorMsg(
          "Error getting the shared params file." );

        return false;
      }

      // get or create the shared params group:

      DefinitionGroup group
        = file.Groups.get_Item( _groupname );

      if( null == group )
      {
        group = file.Groups.Create( _groupname );
      }

      if( null == group )
      {
        Util.ErrorMsg(
          "Error getting the shared params group." );

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

      bool visible = cat.AllowsBoundParameters;

      // get or create the shared params definition:

      string defname = _defname + nameSuffix.ToString();

      Definition definition = group.Definitions.get_Item(
        defname );

      if( null == definition )
      {
        definition = group.Definitions.Create(
          defname, _deftype, visible );
      }
      if( null == definition )
      {
        Util.ErrorMsg(
          "Error creating shared parameter." );

        return false;
      }

      // create the category set containing our category for binding:

      CategorySet catSet = ca.NewCategorySet();
      catSet.Insert( cat );

      // bind the param:

      try
      {
        Binding binding = typeParameter
          ? ca.NewTypeBinding( catSet ) as Binding
          : ca.NewInstanceBinding( catSet ) as Binding;

        // we could check if it is already bound,
        // but it looks like insert will just ignore
        // it in that case:

        doc.ParameterBindings.Insert( definition, binding );

        // we can also specify the parameter group here:

        //doc.ParameterBindings.Insert( definition, binding,
        //  BuiltInParameterGroup.PG_GEOMETRY );

        Debug.Print(
          "Created a shared {0} parameter '{1}' for the {2} category.",
          ( typeParameter ? "type" : "instance" ),
          defname, cat.Name );
      }
      catch( Exception ex )
      {
        Util.ErrorMsg( string.Format(
          "Error binding shared parameter to category {0}: {1}",
          cat.Name, ex.Message ) );
        return false;
      }
      return true;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;
      Category cat;
      int i = 0;

      // create instance parameters:

      foreach( BuiltInCategory target in targets )
      {
        cat = GetCategory( doc, target );
        if( null != cat )
        {
          CreateSharedParameter( doc, cat, ++i, false );
        }
      }

      // create a type parameter:

      cat = GetCategory( doc, BuiltInCategory.OST_Walls );
      CreateSharedParameter( doc, cat, ++i, true );

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

  }
}
