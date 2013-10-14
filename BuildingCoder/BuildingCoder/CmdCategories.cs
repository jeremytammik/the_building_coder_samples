#region Header
//
// CmdCategories.cs - list document and built-in categories
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdCategories : IExternalCommand
  {
    void f( Document doc )
    {
      Application app = doc.Application;

      DefinitionFile sharedParametersFile
        = app.OpenSharedParameterFile();

      DefinitionGroup group = sharedParametersFile
        .Groups.Create( "Reinforcement" );

      Definition def = group.Definitions.Create(
        "ReinforcementParameter", ParameterType.Text );

      List<BuiltInCategory> bics
        = new List<BuiltInCategory>();

      //bics.Add(BuiltInCategory.OST_AreaRein);
      //bics.Add(BuiltInCategory.OST_FabricAreas);
      //bics.Add(BuiltInCategory.OST_FabricReinforcement);
      //bics.Add(BuiltInCategory.OST_PathRein);
      //bics.Add(BuiltInCategory.OST_Rebar);

      bics.Add( BuiltInCategory
        .OST_IOSRebarSystemSpanSymbolCtrl );

      CategorySet catset = new CategorySet();

      foreach( BuiltInCategory bic in bics )
      {
        catset.Insert(
          doc.Settings.Categories.get_Item( bic ) );
      }

      InstanceBinding binding
        = app.Create.NewInstanceBinding( catset );

      doc.ParameterBindings.Insert( def, binding,
        BuiltInParameterGroup.PG_CONSTRUCTION );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Application app = uiapp.Application;
      Document doc = uiapp.ActiveUIDocument.Document;
      Categories categories = doc.Settings.Categories;

      int nCategories = categories.Size;

      Debug.Print( 
        "{0} categories and their parents obtained "
        + "from the Categories collection:", 
        nCategories );

      foreach( Category c in categories )
      {
        Category p = c.Parent;

        Debug.Print( "  {0} ({1}), parent {2}",
          c.Name, c.Id.IntegerValue,
          (null == p ? "<none>" : p.Name) );
      }

      Array bics = Enum.GetValues(
        typeof( BuiltInCategory ) );

      int nBics = bics.Length;

      Debug.Print( "{0} built-in categories and the "
        + "corresponding document ones:", nBics );

      Category cat;
      string s;

      List<BuiltInCategory> bics_null
        = new List<BuiltInCategory>();

      List<BuiltInCategory> bics_exception
        = new List<BuiltInCategory>();

      foreach( BuiltInCategory bic in bics )
      {
        try
        {
          cat = categories.get_Item( bic );

          if( null == cat )
          {
            bics_null.Add( bic );
            s = "<null>";
          }
          else
          {
            s = string.Format( "{0} ({1})",
              cat.Name, cat.Id.IntegerValue );
          }
        }
        catch( Exception ex )
        {
          bics_exception.Add( bic );

          s = ex.GetType().Name + " " + ex.Message;
        }
        Debug.Print( "  {0} --> {1}", 
          bic.ToString(), s );
      }

      int nBicsNull = bics_null.Count;
      int nBicsException = bics_exception.Count;

    #if ACCESS_HIDDEN_CATEGORIES_THROUGH_FILTERED_ELEMENT_COLLECTOR
      // Trying to use OfClass( typeof( Category ) )
      // throws an ArgumentException exception saying
      // "Input type Category is not a recognized 
      // Revit API type".

      IEnumerable<Category> cats
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .Cast<Category>();

      // Unable to cast object of type 
      // 'Autodesk.Revit.DB.Element' to type 
      // 'Autodesk.Revit.DB.Category':

      int nCategoriesFiltered = cats.Count<Category>();

      Debug.Print(
        "{0} categories obtained from a filtered "
        + "element collector:",
        nCategoriesFiltered );

      foreach( Category c in cats )
      {
        Debug.Print( "  {0}", c.Name );
      }
    #endif // ACCESS_HIDDEN_CATEGORIES_THROUGH_FILTERED_ELEMENT_COLLECTOR

      TaskDialog dlg = new TaskDialog(
        "Hidden Built-in Categories" );

      s = string.Format(
        "{0} categories obtained from the Categories collection;\r\n"
        + "{1} built-in categories;\r\n"
        + "{2} built-in categories retrieve null result;\r\n"
        + "{3} built-in categories throw an exception:\r\n",
        nCategories, nBics, nBicsNull, nBicsException );

      Debug.Print( s );

      dlg.MainInstruction = s;

      s = bics_exception
        .Aggregate<BuiltInCategory, string>(
          string.Empty,
          ( a, bic ) => a + "\n" + bic.ToString() );

      Debug.Print( s );

      dlg.MainContent = s;

      dlg.Show();

      return Result.Succeeded;
    }
  }
}
