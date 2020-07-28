#region Header
//
// CmdCategories.cs - list document and built-in categories
//
// Copyright (C) 2010-2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdCategories : IExternalCommand
  {
    #region HideLightingFixtureHosts
    /// <summary>
    /// Hide the LightingFixtures category 
    /// Hosts subcategory in the given view, cf.
    /// http://forums.autodesk.com/t5/revit-api/how-to-get-image-of-a-family-model-without-showing-host-element/td-p/5526085
    /// http://forums.autodesk.com/t5/revit-api/how-to-change-visibility-setting/td-p/5526076
    /// http://forums.autodesk.com/t5/revit-api/how-to-get-family-model-image/td-p/5494839
    /// </summary>
    void HideLightingFixtureHosts( View view )
    {
      Document doc = view.Document;

      Categories categories = doc.Settings.Categories;

      Category catLightingFixtures
        = categories.get_Item(
          BuiltInCategory.OST_LightingFixtures );

      CategoryNameMap subcats
        = catLightingFixtures.SubCategories;

      Category catHosts = subcats.get_Item( "Hosts" );

      //view.SetVisibility( catHosts, false ); // 2016
      view.SetCategoryHidden( catHosts.Id, true ); // 2017
    }
    #endregion // HideLightingFixtureHosts

    #region ProblemAddingParameterBindingForCategory
    static Util.SpellingErrorCorrector
      _spellingErrorCorrector = null;

    void ProblemAddingParameterBindingForCategory(
      Document doc )
    {
      Application app = doc.Application;

      if( null == _spellingErrorCorrector )
      {
        _spellingErrorCorrector
          = new Util.SpellingErrorCorrector( app );
      }

      DefinitionFile sharedParametersFile
        = app.OpenSharedParameterFile();

      DefinitionGroup group = sharedParametersFile
        .Groups.Create( "Reinforcement" );

      //Definition def = group.Definitions.Create( // 2014
      //  "ReinforcementParameter", ParameterType.Text );

      ExternalDefinitionCreationOptions opt
        = new ExternalDefinitionCreationOptions(
          "ReinforcementParameter", ParameterType.Text );

      Definition def = group.Definitions.Create( opt ); // 2015

      // To handle both ExternalDefinitonCreationOptions 
      // and ExternalDefinitionCreationOptions:

      def = _spellingErrorCorrector.NewDefinition(
        group.Definitions, "ReinforcementParameter",
        ParameterType.Text );

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
    #endregion // ProblemAddingParameterBindingForCategory

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
          ( null == p ? "<none>" : p.Name ) );
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

    #region Built-in categories for legend components
    // For https://forums.autodesk.com/t5/revit-api-forum/categories-that-can-create-legend-components/m-p/9659069

    BuiltInCategory [] _bics_for_legend_component_with_FamilyInstance 
      = new BuiltInCategory[] {
        BuiltInCategory.OST_Casework,
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_Doors,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_ElectricalEquipment,
        BuiltInCategory.OST_Furniture,
        BuiltInCategory.OST_LightingFixtures,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_Planting,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_SpecialityEquipment,
        BuiltInCategory.OST_Sprinklers,
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFoundation,
        BuiltInCategory.OST_StructuralFraming,
        BuiltInCategory.OST_Windows };

    BuiltInCategory[] _bics_for_legend_component_with_SystemFamily 
      = new BuiltInCategory[] {
        BuiltInCategory.OST_CableTray,
        BuiltInCategory.OST_Ceilings,
        BuiltInCategory.OST_Conduit,
        BuiltInCategory.OST_CurtainWallPanels,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_Floors,
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_RoofSoffit,
        BuiltInCategory.OST_Roofs,
        BuiltInCategory.OST_StackedWalls,
        BuiltInCategory.OST_Walls };

    #endregion // Built-in categories for legend components
  }
}
