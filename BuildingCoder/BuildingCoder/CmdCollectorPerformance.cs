#region Header
//
// CmdCollectorPerformance.cs - benchmark Revit 2011 API collector performance
//
// Copyright (C) 2010-2019 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
#endregion // Namespaces

namespace BuildingCoder
{
  #region Type filter versus anonymous method versus LINQ by Piotr Zurek
  //
  // Compare TypeFilter versus using an
  // anonymous method to filter elements.
  // By Guy Robinson, info@r-e-d.co.nz.
  //
  // Copyright (C) 2008 by Jeremy Tammik,
  // Autodesk Inc. All rights reserved.
  //
  // Updated to the Revit 2011 API and added LINQ filtering.
  // By Piotr Zurek, p.zurek@gmail.com
  //
  //#region Imported Namespaces

  ////.NET common used namespaces
  //using System;
  //using System.Linq;
  //using System.Diagnostics;
  //using System.Collections.Generic;

  ////Revit.NET common used namespaces
  //using Autodesk.Revit.Attributes;
  //using Autodesk.Revit.DB;
  //using Autodesk.Revit.UI;

  //using Application = Autodesk.Revit.ApplicationServices.Application;

  //#endregion

  namespace FilterPerformance
  {
    [Transaction( TransactionMode.Manual )]
    public class Commands : IExternalCommand
    {
      public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements )
      {
        try
        {
          UIApplication uiApp = commandData.Application;
          UIDocument uidoc = uiApp.ActiveUIDocument;
          Application app = uiApp.Application;
          Document doc = uidoc.Document;

          Stopwatch sw = Stopwatch.StartNew();

          // f5 = f1 && f4
          // = f1 && (f2 || f3)
          // = family instance and (door or window)

          #region Filters and collector definitions

          ElementClassFilter f1
            = new ElementClassFilter(
              typeof( FamilyInstance ) );

          ElementCategoryFilter f2
            = new ElementCategoryFilter(
              BuiltInCategory.OST_Doors );

          ElementCategoryFilter f3
            = new ElementCategoryFilter(
              BuiltInCategory.OST_Windows );

          LogicalOrFilter f4
            = new LogicalOrFilter( f2, f3 );

          LogicalAndFilter f5
            = new LogicalAndFilter( f1, f4 );

          FilteredElementCollector collector
            = new FilteredElementCollector( doc );

          #endregion

          //#region Filtering with a class filter
          //List<Element> openingInstances =
          //  collector.WherePasses(f5).ToElements()
          //    as List<Element>;
          //#endregion

          //#region Filtering with an anonymous method
          //List<Element> openings = collector
          //  .WherePasses(f4)
          //  .ToElements() as List<Element>;
          //List<Element> openingInstances
          //  = openings.FindAll(
          //    e => e is FamilyInstance );
          //#endregion

          #region Filtering with LINQ
          List<Element> openings = collector
            .WherePasses( f4 )
            .ToElements() as List<Element>;

          List<Element> openingInstances
            = ( from instances in openings
                where instances is FamilyInstance
                select instances ).ToList<Element>();
          #endregion

          int n = openingInstances.Count;
          sw.Stop();

          Debug.WriteLine( string.Format(
            "Time to get {0} elements: {1}ms",
            n, sw.ElapsedMilliseconds ) );

          return Result.Succeeded;
        }
        catch( Exception ex )
        {
          message = ex.Message + ex.StackTrace;
          return Result.Failed;
        }
      }
    }
  }
  #endregion // Type filter versus anonymous method versus LINQ by Piotr Zurek

  #region Filter for elements in a specific view having a specific phase
  [Transaction( TransactionMode.Manual )]
  public class RevitCommand : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string messages,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      Transaction tx = new Transaction( doc, "Test" );
      tx.Start();

      // use the view filter

      FilteredElementCollector collector
        = new FilteredElementCollector(
          doc, doc.ActiveView.Id );

      // use the parameter filter.
      // get the phase id "New construction"

      ElementId idPhase = GetPhaseId(
        "New Construction", doc );

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( (int)
            BuiltInParameter.PHASE_CREATED ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericEquals();

      FilterElementIdRule rule
        = new FilterElementIdRule(
          provider, evaluator, idPhase );

      ElementParameterFilter parafilter
        = new ElementParameterFilter( rule );

      collector.WherePasses( parafilter );

      TaskDialog.Show( "Element Count",
        "There are " + collector.Count().ToString()
        + " elements in the current view created"
        + " with phase New Construction" );

      tx.Commit();

      return Result.Succeeded;
    }

    public ElementId GetPhaseId(
      string phaseName,
      Document doc )
    {
      ElementId id = null;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( Phase ) );

      var phases = from Phase phase in collector
                   where phase.Name.Equals( phaseName )
                   select phase;

      id = phases.First().Id;

      return id;
    }
  }
  #endregion // Filter for elements in a specific view having a specific phase

  #region Parameter filter using display name
  public class ParamFilterTest : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      Reference r = uidoc.Selection.PickObject(
        ObjectType.Element );

      // 'Autodesk.Revit.DB.Reference.Element' is
      // obsolete: Property will be removed. Use
      // Document.GetElement(Reference) instead.
      //Wall wall = r.Element as Wall; // 2011

      Wall wall = doc.GetElement( r ) as Wall; // 2012

      //Parameter parameter = wall.get_Parameter( "Unconnected Height" ); // 2014, causes warning CS0618: 'Autodesk.Revit.DB.Element.get_Parameter(string)' is obsolete: 'This property is obsolete in Revit 2015, as more than one parameter can have the same name on a given element. Use Element.Parameters to obtain a complete list of parameters on this Element, or Element.GetParameters(String) to get a list of all parameters by name, or Element.LookupParameter(String) to return the first available parameter with the given name.'
      Parameter parameter = wall.get_Parameter( BuiltInParameter.WALL_USER_HEIGHT_PARAM ); // 2015, avoids warning, in language indepependent and more effective to look up

      ParameterValueProvider pvp
        = new ParameterValueProvider( parameter.Id );

      FilterNumericRuleEvaluator fnrv
        = new FilterNumericGreater();

      FilterRule fRule
        = new FilterDoubleRule( pvp, fnrv, 20, 1E-6 );

      ElementParameterFilter filter
        = new ElementParameterFilter( fRule );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      // Find walls with unconnected height
      // less than or equal to 20:

      ElementParameterFilter lessOrEqualFilter
        = new ElementParameterFilter( fRule, true );

      IList<Element> lessOrEqualFounds
        = collector.WherePasses( lessOrEqualFilter )
          .OfCategory( BuiltInCategory.OST_Walls )
          .OfClass( typeof( Wall ) )
          .ToElements();

      TaskDialog.Show( "Revit", "Walls found: "
        + lessOrEqualFounds.Count );

      return Result.Succeeded;
    }
  }
  #endregion // Parameter filter using display name

  [Transaction( TransactionMode.ReadOnly )]
  class CmdCollectorPerformance : IExternalCommand
  {
    Document _doc;

    #region Retrieve all exterior walls
    /// <summary>
    /// Wall type predicate for exterior wall function
    /// </summary>
    bool IsExterior( WallType wallType )
    {
      Parameter p = wallType.get_Parameter(
        BuiltInParameter.FUNCTION_PARAM );

      Debug.Assert( null != p, "expected wall type "
        + "to have wall function parameter" );

      WallFunction f = (WallFunction) p.AsInteger();

      return WallFunction.Exterior == f;
    }

    /// <summary>
    /// Return all exterior walls, cf.
    /// https://forums.autodesk.com/t5/revit-api-forum/how-do-i-get-all-the-outermost-walls-in-the-model/m-p/7998948
    /// </summary>
    IEnumerable<Element> GetAllExteriorWalls(
      Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( Wall ) )
        .Cast<Wall>()
        .Where<Wall>( w =>
          IsExterior( w.WallType ) );
    }
    #endregion // Retrieve all exterior walls

    #region Get Families of a given Category
    // for http://forums.autodesk.com/t5/revit-api-forum/having-trouble-filtering-to-ost-titleblocks/m-p/6827759
    static bool FamilyFirstSymbolCategoryEquals(
      Family f,
      BuiltInCategory bic )
    {
      Document doc = f.Document;

      ISet<ElementId> ids = f.GetFamilySymbolIds();

      Category cat = ( 0 == ids.Count )
        ? null
        : doc.GetElement( ids.First<ElementId>() ).Category;

      return null != cat
        && cat.Id.IntegerValue.Equals( (int) bic );
    }

    static void GetFamiliesOfCategory(
      Document doc,
      BuiltInCategory bic )
    {
      // This does not work:

      //FilteredElementCollector collector
      //  = new FilteredElementCollector( doc );
      //ICollection<Element> titleFrames = collector
      //  .OfCategory( BuiltInCategory.OST_TitleBlocks )
      //  .OfClass( typeof( Family ) )
      //  .ToElements();

      IEnumerable<Family> families
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) )
          .Cast<Family>()
          .Where<Family>( f =>
            FamilyFirstSymbolCategoryEquals( f, bic ) );
    }
    #endregion // Get Families of a given Category

    #region Get parameter values from all Detail Component family instances
    // cf. http://forums.autodesk.com/t5/revit-api/get-parameter-value-for-a-collection-of-family-instances/m-p/5896191
    /// <summary>
    /// Retrieve all Detail Component family instances,
    /// read the custom parameter value from each, 
    /// assuming it is a real number, and return a 
    /// dictionary mapping all element ids to the 
    /// corresponding param values.
    /// </summary>
    Dictionary<int, double>
      GetAllDetailComponentCustomParamValues(
        Document doc )
    {
      FilteredElementCollector dcs
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) )
          .OfCategory( BuiltInCategory
            .OST_DetailComponents );

      int n = dcs.GetElementCount();

      const string param_name = "Custom_Param";

      Dictionary<int, double> d
        = new Dictionary<int, double>( n );

      foreach( Element dc in dcs )
      {
        IList<Parameter> ps = dc.GetParameters(
          param_name );

        if( 1 != ps.Count )
        {
          throw new Exception(
            "expected exactly one custom parameter" );
        }

        d.Add( dc.Id.IntegerValue, ps[0].AsDouble() );
      }
      return d;
    }
    #endregion //Get parameter values from all Detail Component family instances

    #region Collector is iterable without ToElements
    /// <summary>
    /// Iterate directly over the filtered element collector.
    /// In general, there is no need to create a copy of it.
    /// Calling ToElements creates a copy, allocating space 
    /// for that and wasting both memory and time.
    /// No need to cast either, foreach can do that 
    /// automatically.
    /// </summary>
    IEnumerable<Element> IterateOverCollector(
      Document doc )
    {
      // Do not do this!

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( Family ) ).ToElements();

      IEnumerable<Family> nestedFamilies
        = collector.Cast<Family>();

      String str = "";

      foreach( Family f in nestedFamilies )
      {
        str = str + f.Name + "\n";

        foreach( ElementId symbolId in
          f.GetFamilySymbolIds() )
        {
          Element symbolElem = doc.GetElement(
            symbolId );

          str = str + " family type： "
            + symbolElem.Name + "\n";
        }
      }

      // Iterate directly over the collector instead.
      // No need for ToElements, which creates a copy.
      // The copy wastes memory and time.
      // No need for a cast, even.

      FilteredElementCollector families
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) );

      foreach( Family f in families )
      {
        str = str + f.Name + "\n";

        // ...
      }
      return families;
    }
    #endregion // Collector is iterable without ToElements

    #region Get all model elements
    /// <summary>
    /// Return all model elements, cf.
    /// http://forums.autodesk.com/t5/revit-api/traverse-all-model-elements-in-a-project-top-down-approach/m-p/5815247
    /// </summary>
    IEnumerable<Element> GetAllModelElements(
      Document doc )
    {
      Options opt = new Options();

      return new FilteredElementCollector( doc )
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where<Element>( e
          => null != e.Category
          && null != e.get_Geometry( opt ) );
    }

    IList<Element> GetFamilyInstanceModelElements(
      Document doc )
    {
      ElementClassFilter familyInstanceFilter
        = new ElementClassFilter(
          typeof( FamilyInstance ) );

      FilteredElementCollector familyInstanceCollector
        = new FilteredElementCollector( doc );

      IList<Element> elementsCollection
        = familyInstanceCollector.WherePasses(
          familyInstanceFilter ).ToElements();

      IList<Element> modelElements
        = new List<Element>();

      foreach( Element e in elementsCollection )
      {
        if( ( null != e.Category )
        && ( null != e.LevelId )
        && ( null != e.get_Geometry( new Options() ) )
        )
        {
          modelElements.Add( e );
        }
      }
      return modelElements;
    }

    /// <summary>
    /// Select all physical items, cf.
    /// http://forums.autodesk.com/t5/revit-api-forum/select-all-physical-items-in-model/m-p/6822940
    /// </summary>
    IEnumerable<Element> SelectAllPhysicalElements(
      Document doc )
    {
      return new FilteredElementCollector( doc )
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where( e => e.IsPhysicalElement() );
    }
    #endregion // Get all model elements

    #region Get Model Extents
    /// <summary>
    /// Return a bounding box enclosing all model 
    /// elements using only quick filters.
    /// </summary>
    BoundingBoxXYZ GetModelExtents( Document doc )
    {
      FilteredElementCollector quick_model_elements
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent();

      IEnumerable<BoundingBoxXYZ> bbs = quick_model_elements
        .Where<Element>( e => null != e.Category )
        .Select<Element, BoundingBoxXYZ>( e
           => e.get_BoundingBox( null ) );

      return bbs.Aggregate<BoundingBoxXYZ>( ( a, b )
        => { a.ExpandToContain( b ); return a; } );
    }
    #endregion // Get Model Extents

    #region Traverse all model elements top down Levels > Category > Family > Type > Instance
    void TraverseInstances( Document doc )
    {
      FilteredElementCollector levels
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Level ) );

      foreach( Level level in levels )
      {
        // Now what?
        // We could set up new filtered element 
        // collectors for each level, but it would
        // get complex and we would start repeating
        // ourselves...
      }

      // Get all family instances and use those to
      // set up dictionaries for all the required
      // mappings in one fell swoop. In the end, we
      // will need the following mappings:
      // - level to all categories it hosts instances of
      // - for each level and category, all families
      // - family to its types
      // - family type to instances

      FilteredElementCollector instances
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) );

      // Top level map.

      Dictionary<ElementId, // level
        List<ElementId>> // categories
          mapLevelToCategories = new
          Dictionary<ElementId,
            List<ElementId>>();

      // What we really need is something like this.
      // It will probably simplify things to implement
      // a custom kind of dictionary for this to add 
      // new entries very simply.

      Dictionary<ElementId, // level
        Dictionary<ElementId, // category
          Dictionary<ElementId, // family
            Dictionary<ElementId, // type
              ElementId>>>> // instance
                map = new Dictionary<ElementId,
                  Dictionary<ElementId,
                    Dictionary<ElementId,
                      Dictionary<ElementId,
                        ElementId>>>>();

      foreach( FamilyInstance inst in instances )
      {
        Category cat = inst.Category;
        Level lev = doc.GetElement( inst.LevelId ) as Level;
        FamilySymbol sym = inst.Symbol;
        Family fam = sym.Family;

        Debug.Assert( null != cat, "expected valid category" );
        Debug.Assert( null != lev, "expected valid level" );
        Debug.Assert( null != sym, "expected valid symbol" );
        Debug.Assert( null != fam, "expected valid family" );

        if( map.ContainsKey( lev.Id ) )
        {
          mapLevelToCategories[lev.Id].Add( cat.Id );
        }
        else
        {
          // First time we encounter this level, 
          // so start a new level.

          List<ElementId> categoriesOnLevel
            = new List<ElementId>( 1 );

          categoriesOnLevel.Add( cat.Id );

          mapLevelToCategories.Add( lev.Id,
            categoriesOnLevel );
        }

        // Sort into families and types per level and category...
      }
    }
    #endregion // Traverse all model elements top down Levels > Category > Family > Type > Instance

    #region Retrieve a sorted list of all levels
    IOrderedEnumerable<Level> GetSortedLevels( Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( Level ) )
        .Cast<Level>()
        .OrderBy( lev => lev.Elevation );
    }
    #endregion // Retrieve a sorted list of all levels

    #region Retrieve all areas belonging to a specific area scheme
    /// <summary>
    /// Return the area scheme name of a given area element
    /// using only generic Element Parameter access.
    /// </summary>
    static string GetAreaSchemeNameFromArea( Element e )
    {
      if( !( e is Area ) )
      {
        throw new ArgumentException(
          "Expected Area element input argument." );
      }

      Document doc = e.Document;

      Parameter p = e.get_Parameter(
        BuiltInParameter.AREA_SCHEME_ID );

      if( null == p )
      {
        throw new ArgumentException(
          "element lacks AREA_SCHEME_ID parameter" );
      }

      Element areaScheme = doc.GetElement( p.AsElementId() );

      p = areaScheme.get_Parameter(
        BuiltInParameter.AREA_SCHEME_NAME );

      if( null == p )
      {
        throw new ArgumentException(
          "area scheme lacks AREA_SCHEME_NAME parameter" );
      }

      return p.AsString();
    }

    /// <summary>
    /// Retrieve all areas belonging to 
    /// a specific area scheme.
    /// </summary>
    public IEnumerable<Element> GetAreasInAreaScheme(
      Document doc,
      string areaSchemeName )
    {
      return new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_Areas )
        .OfClass( typeof( SpatialElement ) )
        .Where<Element>( e => areaSchemeName.Equals(
          GetAreaSchemeNameFromArea( e ) ) );
    }
    #endregion // Retrieve all areas belonging to a specific area scheme

    #region Retrieve all rooms on a given level
    /// <summary>
    /// Retrieve all rooms on a given level for
    /// https://forums.autodesk.com/t5/revit-api-forum/collect-all-room-in-leve-xx/m-p/6936959
    /// </summary>
    public IEnumerable<Room> GetRoomsOnLevel(
      Document doc,
      ElementId idLevel )
    {
      return new FilteredElementCollector( doc )
        .WhereElementIsNotElementType()
        .OfClass( typeof( SpatialElement ) )
        .Where( e => e.GetType() == typeof( Room ) )
        .Where( e => e.LevelId.IntegerValue.Equals(
          idLevel.IntegerValue ) )
        .Cast<Room>();
    }
    #endregion // Retrieve all rooms on a given level

    #region Filter for concrete ramps
    /// <summary>
    /// Retrieve all ramps
    /// </summary>
    void f_ramps( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfCategory( BuiltInCategory.OST_Ramps )
          .WhereElementIsNotElementType();

      foreach( Element e in collector )
      {
        Debug.Print( e.GetType().Name );
      }
    }

    /// <summary>
    /// Retrieve all concrete ramps
    /// </summary>
    IEnumerable<Element> findConcreteRamps( Document doc )
    {
      return new FilteredElementCollector( doc )
        .WhereElementIsNotElementType()
        .OfCategory( BuiltInCategory.OST_Ramps )
        //.Where( e => e.Category.Id.IntegerValue.Equals( 
        //  (int) BuiltInCategory.OST_Ramps ) )
        .Where( e =>
        {
          ElementId id = e.GetValidTypes().First(
            id2 => id2.Equals( e.GetTypeId() ) );

          Material m = doc.GetElement( doc.GetElement( id )
            .get_Parameter(
              BuiltInParameter.RAMP_ATTR_MATERIAL )
            .AsElementId() ) as Material;

          return m.Name.Contains( "Concrete" );
        } );
    }
    #endregion // Filter for concrete ramps

    #region Find parameter id for shared parameter element filter
    /// <summary>
    /// Return a list of all elements with the 
    /// specified value in their shared parameter with 
    /// the given name oand group. They are retrieved
    /// using a parameter filter, and the required 
    /// parameter id is found by temporarily adding 
    /// the shared parameter to the project info.
    /// </summary>
    static IList<Element> GetElementsMatchingParameter(
      Document doc,
      string paramName,
      string paramGroup,
      string paramValue )
    {
      IList<Element> elems = new List<Element>();

      // Determine if definition for parameter binding exists

      Definition definition = null;
      BindingMap bm = doc.ParameterBindings;
      DefinitionBindingMapIterator it = bm.ForwardIterator();
      while( it.MoveNext() )
      {
        Definition def = it.Key;
        if( def.Name.Equals( paramName ) )
        {
          definition = def;
          break;
        }
      }
      if( definition == null )
      {
        return elems; // parameter binding not defined
      }

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Set temporary parameter" );

        // Temporarily set project information element 
        // parameter in order to determine param.Id

        FilteredElementCollector collectorPI
          = new FilteredElementCollector( doc );

        collectorPI.OfCategory(
          BuiltInCategory.OST_ProjectInformation );

        Element projInfoElem
          = collectorPI.FirstElement();

        // using http://thebuildingcoder.typepad.com/blog/2012/04/adding-a-category-to-a-shared-parameter-binding.html

        Parameter param = null;

        // param = HelperParams.GetOrCreateElemSharedParam(
        //   projInfoElem, paramName, paramGroup,
        //   ParameterType.Text, false, true );

        if( param != null )
        {
          ElementId paraId = param.Id;

          tx.RollBack(); // discard project element change

          ParameterValueProvider provider
            = new ParameterValueProvider( paraId );

          FilterRule rule = new FilterStringRule(
            provider, new FilterStringEquals(),
            paramValue, true );

          ElementParameterFilter filter
            = new ElementParameterFilter( rule );

          FilteredElementCollector collector
            = new FilteredElementCollector(
              doc, doc.ActiveView.Id );

          elems = collector.WherePasses( filter )
            .ToElements();
        }
      }
      return elems;
    }
    #endregion // Find parameter id for shared parameter element filter

    #region GetAllElementsUsingType
    /// <summary>
    /// Return the all elements that
    /// use the given ElementType.
    /// </summary>
    static FilteredElementCollector
      GetAllElementsUsingType(
        Document doc,
        ElementType et )
    {
      // Built-in parameter storing the type element id:

      BuiltInParameter bip
        = BuiltInParameter.ELEM_TYPE_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericEquals();

      FilterRule rule = new FilterElementIdRule(
        provider, evaluator, et.Id );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WherePasses( filter );

      return collector;
    }
    #endregion // GetAllElementsUsingType

    #region Electrical stuff for Martin Schmid
    void f()
    {
      // how to get the TemperatureRatingTypeSet?

      FilteredElementCollector collector1
        = new FilteredElementCollector( _doc )
          .OfClass( typeof( TemperatureRatingType ) );

      // how to get the InsulationTypeSet?

      FilteredElementCollector collector2
        = new FilteredElementCollector( _doc )
          .OfClass( typeof( InsulationTypeSet ) );

      // how to get the WireSizeSet?

      FilteredElementCollector collector3
        = new FilteredElementCollector( _doc )
          .OfClass( typeof( WireSizeSet ) );

      // how to get the 'first' WireMaterialType?

      WireMaterialType firstWireMaterialType
        = new FilteredElementCollector( _doc )
        .OfClass( typeof( WireMaterialType ) )
        .Cast<WireMaterialType>()
        .First<WireMaterialType>();
    }
    #endregion // Electrical stuff for Martin Schmid

    #region Filter for various classes
    void f3()
    {
      List<ElementFilter> a
        = new List<ElementFilter>( 3 );

      a.Add( new ElementClassFilter( typeof( Family ) ) );
      a.Add( new ElementClassFilter( typeof( Duct ) ) );
      a.Add( new ElementClassFilter( typeof( Pipe ) ) );

      FilteredElementCollector collector
        = new FilteredElementCollector( _doc )
          .WherePasses( new LogicalOrFilter( a ) );
    }
    #endregion // Filter for various classes

    #region Filter for walls in a specific area
    // from RevitAPI.chm description of BoundingBoxIntersectsFilter Class
    // case 1260682 [Find walls in a specific area]
    void f2()
    {
      // Use BoundingBoxIntersects filter to find
      // elements with a bounding box that intersects
      // the given outline.

      // Create a Outline, uses a minimum and maximum
      // XYZ point to initialize the outline.

      Outline myOutLn = new Outline(
        XYZ.Zero, new XYZ( 100, 100, 100 ) );

      // Create a BoundingBoxIntersects filter with
      // this Outline

      BoundingBoxIntersectsFilter filter
        = new BoundingBoxIntersectsFilter( myOutLn );

      // Apply the filter to the elements in the
      // active document.  This filter excludes all
      // objects derived from View and objects
      // derived from ElementType

      FilteredElementCollector collector
        = new FilteredElementCollector( _doc );

      IList<Element> elements =
        collector.WherePasses( filter ).ToElements();

      // Find all walls which don't intersect with
      // BoundingBox: use an inverted filter to match
      // elements.  Use shortcut command OfClass()
      // to find walls only

      BoundingBoxIntersectsFilter invertFilter
        = new BoundingBoxIntersectsFilter( myOutLn,
          true ); // inverted filter

      collector = new FilteredElementCollector( _doc );

      IList<Element> notIntersectWalls
        = collector.OfClass( typeof( Wall ) )
          .WherePasses( invertFilter ).ToElements();
    }
    #endregion // Filter for walls in a specific area

    #region Filter for detail curves
    void f_detail_curves()
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( _doc );

      collector.OfClass( typeof( DetailCurve ) );
    }
    #endregion // Filter for detail curves

    #region Filter for views
    void f_views()
    {
      //View view;
      //List<Element> ViewElements
      //  = view.Elements.OfType<Element>().ToList();

      FilteredElementCollector coll
        = new FilteredElementCollector( _doc );

      coll.OfClass( typeof( View ) );
    }

    public static List<View> GetElementViews(
      List<Element> a,
      List<View> views )
    {
      List<View> returnViews = new List<View>();
      foreach( View view in views )
      {
        FilteredElementCollector coll
          = new FilteredElementCollector(
            view.Document, view.Id );

        coll = coll.WhereElementIsNotElementType();

        List<Element> elementList = coll.ToList();

        foreach( Element e1 in a )
        {
          Element e2 = elementList.Where(
            x => x.Id == e1.Id )
            .FirstOrDefault();

          if( e2 != null
            && null == returnViews.Where(
              x => x.Id == view.Id ).FirstOrDefault() )
          {
            returnViews.Add( view );
          }
        }
      }
      return returnViews;
    }

    public static void GetViewsAndDrawingSheets1(
      Document doc,
      List<View> views,
      List<ViewSheet> viewSheets )
    {
      FilteredElementCollector coll
        = new FilteredElementCollector( doc );

      coll.OfClass( typeof( View ) );

      foreach( Element e in coll )
      {
        if( e is View )
        {
          View view = e as View;
          if( null != view.CropBox )
            views.Add( view );
        }
        else if( e is ViewSheet )
        {
          viewSheets.Add( e as ViewSheet );
        }
      }
    }

    public static void GetViewsAndDrawingSheets(
      Document doc,
      List<View> views,
      List<ViewSheet> viewSheets )
    {
      FilteredElementCollector coll
        = new FilteredElementCollector( doc );

      coll.OfClass( typeof( View ) );

      foreach( Element e in coll )
      {
        if( e is View )
        {
          View view = e as View;
          if( !view.IsTemplate )
            views.Add( view );
        }
        else if( e is ViewSheet )
        {
          viewSheets.Add( e as ViewSheet );
        }
      }
    }
    #endregion // Filter for views

    #region Retrieve all family instances of specific named family and type
    /// <summary>
    /// Get instances by family name then type name
    /// </summary>
    static IEnumerable<FamilyInstance>
      GetFamilyInstancesByFamilyAndType(
        Document doc,
        string familyName,
        string typeName )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( FamilyInstance ) )
        .Cast<FamilyInstance>()
        .Where( x => x.Symbol.Family.Name.Equals( familyName ) ) // family
        //.Where( x => x.Name.Equals( typeName ) // family type               
        .Where( x => x.Symbol.Name.Equals( typeName ) ); // family type               
    }

    /// <summary>
    /// Get instances by element type
    /// </summary>
    static IEnumerable<Element> GetInstancesOfElementType( 
      ElementType type )
    {
      int iid = type.Id.IntegerValue;
      return new FilteredElementCollector( type.Document )
        .WhereElementIsNotElementType()
        //.OfClass( typeof( FamilyInstance ) ) // excludes walls, floors, pipes, etc.; all system family elements
        .Where( e => e.GetTypeId().IntegerValue.Equals( 
          iid ) );
    }
    #endregion // Retrieve all family instances of specific named family and type

    #region Retrieve generic family symbols whose name contains "test"
    /// <summary>
    /// Retrieve generic family symbols whose name contains "test"
    /// </summary>
    static FilteredElementCollector
      GetGenericFamilySymbolsNamedTest(
        Document doc )
    {
      // Set up the parameter filter for the symbol name

      ElementId id = new ElementId( BuiltInParameter
        .ALL_MODEL_TYPE_NAME );

      ParameterValueProvider provider
        = new ParameterValueProvider( id );

      FilterStringRuleEvaluator evaluator
        = new FilterStringContains();

      FilterRule rule = new FilterStringRule(
        provider, evaluator, "test", false );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector genericSymbolsNamedTest
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilySymbol ) )
          .OfCategory( BuiltInCategory.OST_GenericModel )
          .WherePasses( filter );

      return genericSymbolsNamedTest;
    }
    #endregion // Retrieve all family instances of specific named family and type

    #region Return first title block family symbol of specific named family and type
    /// <summary>
    /// Get title block family symbol (= definition) 
    /// by family name then type name
    /// </summary>
    static FamilySymbol
      GetTitleBlockSymbolByFamilyAndType(
        Document doc,
        string familyName,
        string typeName )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( FamilySymbol ) )
        .OfCategory( BuiltInCategory.OST_TitleBlocks )
        .Cast<FamilySymbol>()
        .Where( x => x.FamilyName.Equals( familyName ) ) // family
        .FirstOrDefault( x => x.Name == typeName ); // family type
    }

  /// <summary>
  /// Predicate returning true for the desired title
  /// block type and false for all others.
  /// </summary>
  bool IsCorrectTitleBlock( Element e )
  {
    return false;
  }

  ElementId GetSpecificTitleBlockType( Document doc )
  {
    // Create a filter to get a specific title block type:

    Element title_block_type
      = new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_TitleBlocks )
        .WhereElementIsElementType()
        .FirstOrDefault<Element>( e 
          => IsCorrectTitleBlock( e ) );

    // Use null-conditional Elvis operator:

    return title_block_type?.Id;
  }
    #endregion // Return first title block family symbol of specific named family and type

    #region Retrieve named family symbols using either LINQ or a parameter filter
    static FilteredElementCollector
      GetStructuralColumnSymbolCollector(
        Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_StructuralColumns )
        .OfClass( typeof( FamilySymbol ) );
    }

    static IEnumerable<Element> Linq( 
      Document doc, 
      string familySymbolName )
    {
      return GetStructuralColumnSymbolCollector( doc )
        .Where( x => x.Name == familySymbolName );
    }

    static IEnumerable<Element> Linq2( 
      Document doc, 
      string familySymbolName )
    {
      return GetStructuralColumnSymbolCollector( doc )
        .Where( x => x.get_Parameter( 
          BuiltInParameter.SYMBOL_NAME_PARAM )
            .AsString() == familySymbolName );
    }

    private static IEnumerable<Element> FilterRule( 
      Document doc, 
      string familySymbolName )
    {
      return GetStructuralColumnSymbolCollector( doc )
        .WherePasses(
          new ElementParameterFilter(
            new FilterStringRule(
              new ParameterValueProvider(
                new ElementId( BuiltInParameter.SYMBOL_NAME_PARAM ) ),
              new FilterStringEquals(), familySymbolName, true ) ) );
    }

    private static IEnumerable<Element> Factory( 
      Document doc, 
      string familySymbolName )
    {
      return GetStructuralColumnSymbolCollector( doc )
        .WherePasses(
          new ElementParameterFilter(
            ParameterFilterRuleFactory.CreateEqualsRule(
              new ElementId( BuiltInParameter.SYMBOL_NAME_PARAM ),
              familySymbolName, true ) ) );
    }
    #endregion // Retrieve named family symbols using either LINQ or a parameter filter

    #region Retrieve openings in wall
    /// <summary>
    /// Retrieve all openings in a given wall.
    /// </summary>
    void GetOpeningsInWall(
      Document doc,
      Wall wall )
    {
      ElementId id = wall.Id;

      BuiltInCategory bic
        = BuiltInCategory.OST_SWallRectOpening;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( Opening ) );
      collector.OfCategory( bic );

      // explicit iteration and manual
      // checking of a property:

      List<Element> openings = new List<Element>();

      foreach( Opening e in collector )
      {
        if( e.Host.Id.Equals( id ) )
        {
          openings.Add( e );
        }
      }

      // using LINQ:

      IEnumerable<Opening> openingsOnLevelLinq =
        from e in collector.Cast<Opening>()
        where e.Host.LevelId.Equals( id )
        select e;

      // using an anonymous method:

      IEnumerable<Opening> openingsOnLevelAnon =
        collector.Cast<Opening>().Where<Opening>( e
          => e.Host.Id.Equals( id ) );
    }
    #endregion // Retrieve openings in wall

    #region Retrieve ducts and pipes intersecting wall
    /// <summary>
    /// Retrieve ducts and pipes intersecting a given wall.
    /// </summary>
    FilteredElementCollector GetWallMepClashes( Wall wall )
    {
      Document doc = wall.Document;

      var cats = new List<BuiltInCategory>
      {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_PipeCurves
      };

      var mepfilter = new ElementMulticategoryFilter( cats );

      BoundingBoxXYZ bb = wall.get_BoundingBox( null );
      Outline o = new Outline( bb.Min, bb.Max );

      var bbfilter = new BoundingBoxIsInsideFilter( o );

      var clashingElements
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WherePasses( mepfilter )
          .WherePasses( bbfilter );

      return clashingElements;
    }
    #endregion // Retrieve ducts and pipes intersecting wall

    #region Retrieve pipes belonging to specific system type
    /// <summary>
    /// Retrieve all pipes belonging to 
    /// a given pipe system type, cf.
    /// https://forums.autodesk.com/t5/revit-api-forum/filteredelementcollector-by-pipe-system-types/m-p/8620113
    /// </summary>
    FilteredElementCollector GetPipesForSystemType(
      Document doc,
      string system_name )
    {
      FilteredElementCollector fec
        = new FilteredElementCollector( doc );

      // Identify the parameter to be filtered by
      ParameterValueProvider pvp
        = new ParameterValueProvider( new ElementId(
          BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM ) );

      // Set string evaluator so that it equals string
      FilterStringRuleEvaluator fsre = new FilterStringEquals();

      //Create a filter rule where string value equals system_name
      FilterRule fr = new FilterStringRule(
        pvp, fsre, system_name, true );

      // Create Filter
      ElementParameterFilter epf
        = new ElementParameterFilter( fr );

      // Apply filter to filtered element collector
      fec = fec.WherePasses( epf );

      return fec;
    }

    #endregion // Retrieve pipes belonging to specific system type

    #region Retrieve all edges in model
    void RetrieveEdges(
      Document doc,
      Dictionary<Curve, ElementId> curves )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent();

      Options opt = new Options();

      foreach( Element el in collector )
      {
        if( null != el.Category )
        {
          GeometryElement geo = el.get_Geometry( opt );
          if( geo != null )
          {
            foreach( GeometryObject obj in geo )
            {
              Solid sol = obj as Solid;
              if( null != sol )
              {
                foreach( Edge edge in sol.Edges )
                {
                  Curve edgecurve = edge.AsCurve();
                  curves.Add( edgecurve, el.Id );
                }
              }
            }
          }
        }
      }
    }
    #endregion // Retrieve all edges in model

    #region Retrieve family instances intersecting BIM element
    /// <summary>
    /// Retrieve all family instances intersecting a
    /// given BIM element, e.g. all columns 
    /// intersecting a wall.
    /// </summary>
    void GetInstancesIntersectingElement( Element e )
    {
      #region Joe's code
#if JOE_CODE
// Find intersections between family instances and a selected element  

Reference Reference = uidoc.Selection.PickObject( 
ObjectType.Element, "Select element that will "
+ "be checked for intersection with all family "
+ "instances" );

Element e = doc.GetElement( reference );

GeometryElement geomElement = e.get_Geometry( 
new Options() );

Solid solid = null;
foreach( GeometryObject geomObj in geomElement )
{
solid = geomObj as Solid;
if( solid = !null ) break;
}

FilteredElementCollector collector 
= new FilteredElementCollector( doc )
  .OfClass( typeof( FamilyInstance ) )
  .WherePasses( new ElementIntersectsSolidFilter( 
    solid ) );

TaskDialog.Show( "Revit", collector.Count() + 
"Family instances intersect with selected element (" 
+ element.Category.Name + "ID:" + element.Id + ")" );
#endif // JOE_CODE
      #endregion // Joe's code

      // Test this in these SDK sample models:
      // C:\a\lib\revit\2015\SDK\Samples\FindReferencesByDirection\FindColumns\FindColumns-Basic.rvt
      // C:\a\lib\revit\2015\SDK\Samples\FindReferencesByDirection\FindColumns\FindColumns-TestCases.rvt

      Document doc = e.Document;

      Solid solid = e.get_Geometry( new Options() )
        .OfType<Solid>()
        .Where<Solid>( s => null != s && !s.Edges.IsEmpty )
        .FirstOrDefault();

      FilteredElementCollector intersectingInstances
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) )
          .WherePasses( new ElementIntersectsSolidFilter(
            solid ) );

      int n1 = intersectingInstances.Count<Element>();

      intersectingInstances
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) )
          .WherePasses( new ElementIntersectsElementFilter(
            e ) );

      int n = intersectingInstances.Count<Element>();

      Debug.Assert( n.Equals( n1 ),
        "expected solid intersection to equal element intersection" );

      string result = string.Format(
        "{0} family instance{1} intersect{2} the "
        + "selected element {3}{4}",
        n, Util.PluralSuffix( n ),
        ( 1 == n ? "s" : "" ),
        Util.ElementDescription( e ),
        Util.DotOrColon( n ) );

      string id_list = 0 == n
        ? string.Empty
        : string.Join( ", ",
            intersectingInstances
              .Select<Element, string>(
                x => x.Id.IntegerValue.ToString() ) )
          + ".";

      Util.InfoMsg2( result, id_list );
    }

    /// <summary>
    /// Retrieve all beam family instances 
    /// intersecting two columns, cf.
    /// http://forums.autodesk.com/t5/revit-api/check-to-see-if-beam-exists/m-p/6223562
    /// </summary>
    FilteredElementCollector
      GetBeamsIntersectingTwoColumns(
        Element column1,
        Element column2 )
    {
      Document doc = column1.Document;

      if( column2.Document.GetHashCode() != doc.GetHashCode() )
      {
        throw new ArgumentException(
          "Expected two columns from same document." );
      }

      FilteredElementCollector intersectingStructuralFramingElements
        = new FilteredElementCollector( doc )
          .OfClass( typeof( FamilyInstance ) )
          .OfCategory( BuiltInCategory.OST_StructuralFraming )
          .WherePasses( new ElementIntersectsElementFilter( column1 ) )
          .WherePasses( new ElementIntersectsElementFilter( column2 ) );

      int n = intersectingStructuralFramingElements.Count<Element>();

      string result = string.Format(
        "{0} structural framing family instance{1} "
        + "intersect{2} the two beams{3}",
        n, Util.PluralSuffix( n ),
        ( 1 == n ? "s" : "" ),
        Util.DotOrColon( n ) );

      string id_list = 0 == n
        ? string.Empty
        : string.Join( ", ",
            intersectingStructuralFramingElements
              .Select<Element, string>(
                x => x.Id.IntegerValue.ToString() ) )
          + ".";

      Util.InfoMsg2( result, id_list );

      return intersectingStructuralFramingElements;
    }

    /// <summary>
    /// Dummy placeholder function to return solid from element, cf. 
    /// https://thebuildingcoder.typepad.com/blog/2012/06/real-world-concrete-corner-coordinates.html
    /// </summary>
    Solid GetSolid( Element e )
    {
      return null;
    }

    /// <summary>
    /// Collect the element ids of all elements in the 
    /// linked documents intersecting the given element.
    /// </summary>
    /// <param name="e">Target element</param>
    /// <param name="links">Linked documents</param>
    /// <param name="ids">Return intersecting element ids</param>
    /// <returns>Number of intersecting elements found</returns>
    int GetIntersectingLinkedElementIds(
      Element e,
      IList<RevitLinkInstance> links,
      List<ElementId> ids )
    {
      int count = ids.Count();
      Solid solid = GetSolid( e );

      foreach( RevitLinkInstance i in links )
      {
        Transform transform = i.GetTransform(); // GetTransform or GetTotalTransform or what?
        if( !transform.AlmostEqual( Transform.Identity ) )
        {
          solid = SolidUtils.CreateTransformed(
            solid, transform.Inverse );
        }
        ElementIntersectsSolidFilter filter
          = new ElementIntersectsSolidFilter( solid );

        FilteredElementCollector intersecting
          = new FilteredElementCollector( i.GetLinkDocument() )
            .WherePasses( filter );

        ids.AddRange( intersecting.ToElementIds() );
      }
      return ids.Count - count;
    }
    #endregion // Retrieve family instances intersecting BIM element

    #region Retrieve stairs on level
    /// <summary>
    /// Retrieve all stairs on a given level.
    /// </summary>
    FilteredElementCollector
      GetStairsOnLevel(
        Document doc,
        Level level )
    {
      ElementId id = level.Id;

      BuiltInCategory bic
        = BuiltInCategory.OST_Stairs;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfCategory( bic );

      // explicit iteration and manual
      // checking of a property:

      List<Element> stairs = new List<Element>();

      foreach( Element e in collector )
      {
        if( e.LevelId.Equals( id ) )
        {
          stairs.Add( e );
        }
      }

      // using LINQ:

      IEnumerable<Element> stairsOnLevelLinq =
        from e in collector
        where e.LevelId.Equals( id )
        select e;

      // using an anonymous method:

      IEnumerable<Element> stairsOnLevelAnon =
        collector.Where<Element>( e
          => e.LevelId.Equals( id ) );

      // using a parameter filter:

      BuiltInParameter bip
        = BuiltInParameter.STAIRS_BASE_LEVEL_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericEquals();

      FilterRule rule = new FilterElementIdRule(
        provider, evaluator, id );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      return collector.WherePasses( filter );
    }
    #endregion // Retrieve stairs on level

    #region More parameter filter samples
    // 383_param_filter.htm

    void f1( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      ICollection<Element> levels
        = collector.OfClass( typeof( Level ) )
          .ToElements();

      for( int i = 0; i < levels.Count; i++ )
      {
        ElementId levelId = levels.ElementAt( i ).Id;

        ElementLevelFilter levelFilter
          = new ElementLevelFilter( levelId );

        collector = new FilteredElementCollector( doc );

        ICollection<Element> allOnLevel
          = collector.WherePasses( levelFilter )
            .ToElements();

        // . . .
      }
    }

    void f2( Document doc, Level level )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfCategory(
        BuiltInCategory.OST_StructuralFraming );

      collector.OfClass( typeof( FamilyInstance ) );

      BuiltInParameter bip = BuiltInParameter
        .INSTANCE_REFERENCE_LEVEL_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericGreater();

      ElementId idRuleValue = level.Id;

      FilterElementIdRule rule
        = new FilterElementIdRule(
          provider, evaluator, idRuleValue );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      collector.WherePasses( filter );
    }

    [Transaction( TransactionMode.ReadOnly )]
    public class RevitCommand : IExternalCommand
    {
      public Result Execute(
        ExternalCommandData commandData,
        ref string messages,
        ElementSet elements )
      {
        UIApplication app = commandData.Application;
        Document doc = app.ActiveUIDocument.Document;

        ElementId id = new ElementId(
          BuiltInParameter.ELEM_ROOM_NUMBER );

        ParameterValueProvider provider
          = new ParameterValueProvider( id );

        FilterStringRuleEvaluator evaluator
          = new FilterStringEquals();

        string sRoomNumber = "1";

        FilterRule rule = new FilterStringRule(
          provider, evaluator, sRoomNumber, false );

        ElementParameterFilter filter
          = new ElementParameterFilter( rule );

        FilteredElementCollector collector
          = new FilteredElementCollector( doc );

        string s = string.Empty;

        foreach( Element e in collector )
        {
          s += e.Name + e.Category.Name.ToString() + "\n";

        }
        System.Windows.Forms.MessageBox.Show( s );

        return Result.Succeeded;
      }
    }

    void f3( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( Level ) );
      ElementId id = new ElementId(
        BuiltInParameter.DATUM_TEXT );

      ParameterValueProvider provider
        = new ParameterValueProvider( id );

      FilterStringRuleEvaluator evaluator
        = new FilterStringContains();

      FilterRule rule = new FilterStringRule(
        provider, evaluator, "Level", false );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );
    }

    void f4( Document doc )
    {
      // Use numeric evaluator and integer rule to test ElementId parameter
      // Filter levels whose id is greater than specified id value

      BuiltInParameter testParam
        = BuiltInParameter.ID_PARAM;

      ParameterValueProvider pvp
        = new ParameterValueProvider(
          new ElementId( (int) testParam ) );

      FilterNumericRuleEvaluator fnrv
        = new FilterNumericGreater();

      // filter elements whose Id is greater than 99

      ElementId ruleValId = new ElementId( 99 );

      FilterRule paramFr = new FilterElementIdRule(
        pvp, fnrv, ruleValId );

      ElementParameterFilter epf
        = new ElementParameterFilter( paramFr );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( ViewPlan ) )
        .WherePasses( epf ); // only deal with ViewPlan

      // Use numeric evaluator and integer rule to test bool parameter
      // Filter levels whose crop view is false

      int ruleValInt = 0;

      testParam = BuiltInParameter.VIEWER_CROP_REGION;

      pvp = new ParameterValueProvider(
        new ElementId( (int) testParam ) );

      fnrv = new FilterNumericEquals();

      paramFr = new FilterIntegerRule(
        pvp, fnrv, ruleValInt );

      epf = new ElementParameterFilter( paramFr );

      collector = new FilteredElementCollector( doc );

      collector.OfClass( typeof( ViewPlan ) )
        .WherePasses( epf ); // only deal with ViewPlan

      // Use numeric evaluator and double rule to test double parameter
      // Filter levels whose top offset is greater than specified value

      double ruleValDb = 10;

      testParam =
        BuiltInParameter.VIEWER_BOUND_OFFSET_TOP;

      pvp = new ParameterValueProvider(
        new ElementId( (int) testParam ) );

      fnrv = new FilterNumericGreater();

      paramFr = new FilterDoubleRule(
        pvp, fnrv, ruleValDb, double.Epsilon );

      collector = new FilteredElementCollector( doc );

      collector.OfClass( typeof( ViewPlan ) )
        .WherePasses( epf ); // only deal with ViewPlan

      // Use string evaluator and string rule to test string parameter
      // Filter all elements whose view name contains level

      String ruleValStr = "Level";

      testParam = BuiltInParameter.VIEW_NAME;

      pvp = new ParameterValueProvider(
        new ElementId( (int) testParam ) );

      FilterStringRuleEvaluator fnrvStr
        = new FilterStringContains();

      paramFr = new FilterStringRule(
        pvp, fnrvStr, ruleValStr, false );

      collector = new FilteredElementCollector( doc );

      collector.OfClass( typeof( ViewPlan ) )
        .WherePasses( epf ); // only deal with ViewPlan
    }
    #endregion // More parameter filter samples

    #region Helper method to create some elements to play with
    /// <summary>
    /// Create a new level at the given elevation.
    /// </summary>
    Level CreateLevel( int elevation )
    {
      //Level level = _doc.Create.NewLevel( elevation ); // 2015
      Level level = Level.Create( _doc, elevation ); // 2016
      level.Name = "Level " + elevation.ToString();
      return level;
    }
    #endregion // Helper method to create some elements to play with

    #region Methods to measure collector and post processing speed
    /// <summary>
    /// An empty method that does nothing.
    /// </summary>
    Element EmptyMethod( Type type )
    {
      return null;
    }

    /// <summary>
    /// An empty method that does nothing.
    /// </summary>
    Element EmptyMethod( Type type, string name )
    {
      return null;
    }

    /// <summary>
    /// Return all non ElementType elements.
    /// </summary>
    /// <returns></returns>
    FilteredElementCollector GetNonElementTypeElements()
    {
      return new FilteredElementCollector( _doc )
        .WhereElementIsNotElementType();
    }

    /// <summary>
    /// Return a collector of all elements of the given type.
    /// </summary>
    FilteredElementCollector GetElementsOfType(
      Type type )
    {
      return new FilteredElementCollector( _doc )
        .OfClass( type );
    }

    /// <summary>
    /// Return the first element of the given
    /// type without any further filtering.
    /// </summary>
    Element GetFirstElementOfType(
      Type type )
    {
      return new FilteredElementCollector( _doc )
        .OfClass( type )
        .FirstElement();
    }

    /// <summary>
    /// Use a parameter filter to return the first element
    /// of the given type and with the specified string-valued
    /// built-in parameter matching the given name.
    /// </summary>
    Element GetFirstElementOfTypeWithBipString(
      Type type,
      BuiltInParameter bip,
      string name )
    {
      FilteredElementCollector a
        = GetElementsOfType( type );

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterStringRuleEvaluator evaluator
        = new FilterStringEquals();

      FilterRule rule = new FilterStringRule(
        provider, evaluator, name, true );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      return a.WherePasses( filter ).FirstElement();
    }

    #region Methods to measure post processing speed retrieving all elements
    /// <summary>
    /// Return a list of all elements matching
    /// the given type using explicit code to test
    /// the element type.
    /// </summary>
    List<Element> GetElementsOfTypeUsingExplicitCode(
      Type type )
    {
      FilteredElementCollector a
        = GetNonElementTypeElements();

      List<Element> b = new List<Element>();
      foreach( Element e in a )
      {
        if( e.GetType().Equals( type ) )
        {
          b.Add( e );
        }
      }
      return b;
    }

    /// <summary>
    /// Return a list of all elements matching
    /// the given type using a LINQ query to test
    /// the element type.
    /// </summary>
    IEnumerable<Element> GetElementsOfTypeUsingLinq(
      Type type )
    {
      FilteredElementCollector a
        = GetNonElementTypeElements();

      IEnumerable<Element> b =
        from e in a
        where e.GetType().Equals( type )
        select e;

      return b;
    }
    #endregion // Methods to measure post processing speed retrieving all elements

    #region Methods to measure post processing speed retrieving a named element
    /// <summary>
    /// Return the first element of the given
    /// type and name using explicit code.
    /// </summary>
    Element GetFirstNamedElementOfTypeUsingExplicitCode(
      Type type,
      string name )
    {
      FilteredElementCollector a
        = GetElementsOfType( type );

      // explicit iteration and manual checking of a property:

      Element ret = null;
      foreach( Element e in a )
      {
        if( e.Name.Equals( name ) )
        {
          ret = e;
          break;
        }
      }
      return ret;
    }

    /// <summary>
    /// Return the first element of the given
    /// type and name using LINQ.
    /// </summary>
    Element GetFirstNamedElementOfTypeUsingLinq(
      Type type,
      string name )
    {
      FilteredElementCollector a
        = GetElementsOfType( type );

      // using LINQ:

      IEnumerable<Element> elementsByName =
        from e in a
        where e.Name.Equals( name )
        select e;

      return elementsByName.First<Element>();
    }

    /// <summary>
    /// Return the first element of the given
    /// type and name using an anonymous method
    /// to define a named method.
    /// </summary>
    Element GetFirstNamedElementOfTypeUsingAnonymousButNamedMethod(
      Type type,
      string name )
    {
      FilteredElementCollector a
        = GetElementsOfType( type );

      // using an anonymous method to define a named method:

      Func<Element, bool> nameEquals = e => e.Name.Equals( name );
      return a.First<Element>( nameEquals );
    }

    /// <summary>
    /// Return the first element of the given
    /// type and name using an anonymous method.
    /// </summary>
    Element GetFirstNamedElementOfTypeUsingAnonymousMethod(
      Type type,
      string name )
    {
      FilteredElementCollector a
        = GetElementsOfType( type );

      // using an anonymous method:

      return a.First<Element>(
        e => e.Name.Equals( name ) );
    }
    #endregion // Methods to measure post processing speed retrieving a named element

    #endregion // Methods to measure collector and post processing speed

    #region BenchmarkAllLevels
    /// <summary>
    /// Benchmark several different approaches to
    /// using filtered collectors to retrieve
    /// all levels in the model,
    /// and measure the time required to
    /// create IList and List collections
    /// from them.
    /// </summary>
    void BenchmarkAllLevels( int nLevels )
    {
      Type t = typeof( Level );
      int n;

      using( JtTimer pt = new JtTimer(
        "Empty method *" ) )
      {
        EmptyMethod( t );
      }

      using( JtTimer pt = new JtTimer(
        "NotElementType *" ) )
      {
        FilteredElementCollector a
          = GetNonElementTypeElements();
      }

      using( JtTimer pt = new JtTimer(
        "NotElementType as IList *" ) )
      {
        IList<Element> a
          = GetNonElementTypeElements().ToElements();
        n = a.Count;
      }
      Debug.Assert( nLevels <= n,
        "expected to retrieve all non-element-type elements" );

      using( JtTimer pt = new JtTimer(
        "NotElementType as List *" ) )
      {
        List<Element> a = new List<Element>(
          GetNonElementTypeElements() );

        n = a.Count;
      }
      Debug.Assert( nLevels <= n,
        "expected to retrieve all non-element-type elements" );

      using( JtTimer pt = new JtTimer( "Explicit" ) )
      {
        List<Element> a
          = GetElementsOfTypeUsingExplicitCode( t );

        n = a.Count;
      }
      Debug.Assert( nLevels == n,
        "expected to retrieve all levels" );

      using( JtTimer pt = new JtTimer( "Linq" ) )
      {
        IEnumerable<Element> a =
          GetElementsOfTypeUsingLinq( t );

        n = a.Count<Element>();
      }
      Debug.Assert( nLevels == n,
        "expected to retrieve all levels" );

      using( JtTimer pt = new JtTimer(
        "Linq as List" ) )
      {
        List<Element> a = new List<Element>(
          GetElementsOfTypeUsingLinq( t ) );

        n = a.Count;
      }
      Debug.Assert( nLevels == n,
        "expected to retrieve all levels" );

      using( JtTimer pt = new JtTimer( "Collector" ) )
      {
        FilteredElementCollector a
          = GetElementsOfType( t );
      }

      using( JtTimer pt = new JtTimer(
        "Collector as IList" ) )
      {
        IList<Element> a
          = GetElementsOfType( t ).ToElements();

        n = a.Count;
      }
      Debug.Assert( nLevels == n,
        "expected to retrieve all levels" );

      using( JtTimer pt = new JtTimer(
        "Collector as List" ) )
      {
        List<Element> a = new List<Element>(
          GetElementsOfType( t ) );

        n = a.Count;
      }
      Debug.Assert( nLevels == n,
        "expected to retrieve all levels" );
    }
    #endregion // BenchmarkAllLevels

    #region BenchmarkSpecificLevel
    /// <summary>
    /// Benchmark the use of a parameter filter versus
    /// various kinds of post processing of the
    /// results returned by the filtered element
    /// collector to find the level specified by
    /// iLevel.
    /// </summary>
    void BenchmarkSpecificLevel( int iLevel )
    {
      Type t = typeof( Level );
      string name = "Level " + iLevel.ToString();
      Element level;

      using( JtTimer pt = new JtTimer(
        "Empty method *" ) )
      {
        level = EmptyMethod(
          t, name );
      }

      level = null;

      using( JtTimer pt = new JtTimer(
        "Collector with no name check *" ) )
      {
        level = GetFirstElementOfType( t );
      }

      Debug.Assert( null != level, "expected to find a valid level" );

      level = null;

      using( JtTimer pt = new JtTimer(
        "Parameter filter" ) )
      {
        //level = GetFirstElementOfTypeWithBipString(
        //  t, BuiltInParameter.ELEM_NAME_PARAM, name );

        level = GetFirstElementOfTypeWithBipString(
          t, BuiltInParameter.DATUM_TEXT, name );
      }

      Debug.Assert( null != level,
        "expected to find a valid level" );

      level = null;

      using( JtTimer pt = new JtTimer( "Explicit" ) )
      {
        level = GetFirstNamedElementOfTypeUsingExplicitCode(
          t, name );
      }

      Debug.Assert( null != level, "expected to find a valid level" );
      level = null;

      using( JtTimer pt = new JtTimer( "Linq" ) )
      {
        level = GetFirstNamedElementOfTypeUsingLinq(
          t, name );
      }

      Debug.Assert( null != level, "expected to find a valid level" );
      level = null;

      using( JtTimer pt = new JtTimer(
        "Anonymous named" ) )
      {
        level = GetFirstNamedElementOfTypeUsingAnonymousButNamedMethod(
          t, name );
      }

      Debug.Assert( null != level, "expected to find a valid level" );
      level = null;

      using( JtTimer pt = new JtTimer( "Anonymous" ) )
      {
        level = GetFirstNamedElementOfTypeUsingAnonymousMethod(
          t, name );
      }

      Debug.Assert( null != level, "expected to find a valid level" );
    }
    #endregion // BenchmarkSpecificLevel

    #region Family tree test
    public static void CreateFamilyTreeTest(
      Document myDoc )
    {
      IEnumerable<Element> familiesCollector =
        new FilteredElementCollector( myDoc )
          .OfClass( typeof( FamilyInstance ) )
          .WhereElementIsNotElementType()
          .Cast<FamilyInstance>()
          // (family, familyInstances):
          .GroupBy( fi => fi.Symbol.Family )
          .Select( f => f.Key );

      var mapCatToFam = new Dictionary<string,
        List<Element>>();

      var categoryList = new Dictionary<string,
        Category>();

      foreach( var f in familiesCollector )
      {
        var catName = f.Category.Name;

        if( mapCatToFam.ContainsKey( catName ) )
        {
          mapCatToFam[catName].Add( f );
        }
        else
        {
          mapCatToFam.Add( catName,
            new List<Element> { f } );

          categoryList.Add( catName,
            f.Category );
        }
      }
    }
    #endregion // Family tree test

    #region Is element hidden in view by crop box, visibility or category?
    /// <summary>
    /// Checks whether a given Revit element 'e' is 
    /// hidden in a specified view 'v'. 
    /// If v has a crop box defined, e is 
    /// considered hidden if its bounding box is 
    /// outside or less than 25% contained in the 
    /// crop box. If e is not eliminated as hidden 
    /// by that test, its IsHidden predicate is 
    /// checked, followed by the visibility of its 
    /// category and all its parent categories in 
    /// the given view.
    /// Return true if the given element e is hidden
    /// in the view v. This might be due to:
    /// - e lies outside the view crop box
    /// - e is specifically hidden in the view, by element
    /// - the category of e or one of its parent 
    /// categories is hidden in v.
    /// </summary>
    bool IsElementHiddenInView(
      Element e,
      View v )
    {
      if( v.CropBoxActive )
      {
        BoundingBoxXYZ viewBox = v.CropBox;
        BoundingBoxXYZ elBox = e.get_BoundingBox( v );

        Transform transInv = v.CropBox.Transform.Inverse;

        elBox.Max = transInv.OfPoint( elBox.Max );
        elBox.Min = transInv.OfPoint( elBox.Min );

        // The transform above might switch 
        // max and min values.

        if( elBox.Min.X > elBox.Max.X )
        {
          XYZ tmpP = elBox.Min;
          elBox.Min = new XYZ( elBox.Max.X, elBox.Min.Y, 0 );
          elBox.Max = new XYZ( tmpP.X, elBox.Max.Y, 0 );
        }

        if( elBox.Min.Y > elBox.Max.Y )
        {
          XYZ tmpP = elBox.Min;
          elBox.Min = new XYZ( elBox.Min.X, elBox.Max.Y, 0 );
          elBox.Max = new XYZ( tmpP.X, elBox.Min.Y, 0 );
        }

        if( elBox.Min.X > viewBox.Max.X
          || elBox.Max.X < viewBox.Min.X
          || elBox.Min.Y > viewBox.Max.Y
          || elBox.Max.Y < viewBox.Min.Y )
        {
          return true;
        }
        else
        {
          BoundingBoxXYZ inside = new BoundingBoxXYZ();

          double x, y;

          x = elBox.Max.X;

          if( elBox.Max.X > viewBox.Max.X )
            x = viewBox.Max.X;

          y = elBox.Max.Y;

          if( elBox.Max.Y > viewBox.Max.Y )
            y = viewBox.Max.Y;

          inside.Max = new XYZ( x, y, 0 );

          x = elBox.Min.X;

          if( elBox.Min.X < viewBox.Min.X )
            x = viewBox.Min.X;

          y = elBox.Min.Y;

          if( elBox.Min.Y < viewBox.Min.Y )
            y = viewBox.Min.Y;

          inside.Min = new XYZ( x, y, 0 );

          double eBBArea = ( elBox.Max.X - elBox.Min.X )
            * ( elBox.Max.Y - elBox.Min.Y );

          double einsideArea =
            ( inside.Max.X - inside.Min.X )
            * ( inside.Max.Y - inside.Min.Y );

          double factor = einsideArea / eBBArea;

          if( factor < 0.25 )
            return true;
        }
      }

      bool hidden = e.IsHidden( v );

      if( !hidden )
      {
        Category cat = e.Category;

        while( null != cat && !hidden )
        {
          hidden = !cat.get_Visible( v );
          cat = cat.Parent;
        }
      }
      return hidden;
    }
    #endregion // Is element hidden in view by crop box, visibility or category?

    #region Return element id of crop box for a given view
    // http://thebuildingcoder.typepad.com/blog/2013/09/rotating-a-plan-view.html#comment-3734421721
    /// <summary>
    /// Return element id of crop box for a given view.
    /// The built-in parameter ID_PARAM of the crop box 
    /// contains the element id of the view it is used in;
    /// e.g., the crop box 'points' to the view using it 
    /// via ID_PARAM. Therefore, we can use a parameter 
    /// filter to retrieve all crop boxes with the 
    /// view's element id in that parameter.
    /// </summary>
    ElementId GetCropBoxFor( View view )
    {
      ParameterValueProvider provider
        = new ParameterValueProvider( new ElementId(
          (int) BuiltInParameter.ID_PARAM ) );

      FilterElementIdRule rule
        = new FilterElementIdRule( provider,
          new FilterNumericEquals(), view.Id );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      return new FilteredElementCollector( view.Document )
        .WherePasses( filter )
        .ToElementIds()
        .Where<ElementId>( a => a.IntegerValue
          != view.Id.IntegerValue )
        .FirstOrDefault<ElementId>();
    }
    #endregion // Get element id of crop box

    #region Retrieve Family Instances Satisfying Filter Rule
    // https://forums.autodesk.com/t5/revit-api-forum/how-to-filter-element-which-satisfy-filter-rule/m-p/8021978
    /// <summary>
    /// Retrieve Family Instances Satisfying Filter Rule
    /// </summary>
    void GetFamilyInstancesSatisfyingFilterRule(
      Document doc )
    {
      FilteredElementCollector pfes
        = new FilteredElementCollector( doc )
          .OfClass( typeof( ParameterFilterElement ) );

      foreach( ParameterFilterElement pfe in pfes )
      {
        #region Get Filter Name, Category and Elements underlying the categories

        ElementMulticategoryFilter catfilter
          = new ElementMulticategoryFilter(
            pfe.GetCategories() );

        FilteredElementCollector elemsByFilter
         = new FilteredElementCollector( doc )
            .WhereElementIsNotElementType()
            .WherePasses( catfilter );

        //foreach( FilterRule rule in pfe.GetRules() ) // 2018
        //{
        //  IEnumerable<Element> elemsByFilter2
        //    = elemsByFilter.Where( e
        //      => rule.ElementPasses( e ) );
        //}

        ElementFilter ef = pfe.GetElementFilter(); // 2019
        IEnumerable<Element> elemsByFilter2
          = elemsByFilter.WherePasses( ef );
        #endregion
      }
    }
    #endregion // Retrieve Family Instances Satisfying Filter Rule

    #region Determine element count for each type of each category
    /// <summary>
    /// Determine element count for each type of each category
    /// </summary>
    void GetCountPerTypePerCategory( Document doc )
    {
      FilteredElementCollector els
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType();

      // Map category id to dictionary mapping
      // type id to element count

      Dictionary<ElementId, Dictionary<ElementId, int>> map
        = new Dictionary<ElementId, Dictionary<ElementId, int>>();

      foreach( Element e in els )
      {
        Category cat = e.Category;

        ElementId idCat = ( null == cat )
          ? ElementId.InvalidElementId
          : e.Category.Id;

        ElementId idTyp = e.GetTypeId()
          ?? ElementId.InvalidElementId;

        if( !map.ContainsKey( idCat ) )
        {
          map.Add( idCat, new Dictionary<ElementId, int>() );
        }
        if( !map[idCat].ContainsKey( idTyp ) )
        {
          map[idCat].Add( idTyp, 0 );
        }
        ++map[idCat][idTyp];
      }

      List<ElementId> idsCat = new List<ElementId>( map.Keys );
      idsCat.Sort();
      int n = idsCat.Count;
      Debug.Print( "{0} categor{1}:", n, Util.PluralSuffixY( n ) );

      foreach( ElementId id in idsCat )
      {
        List<ElementId> idsTyp = new List<ElementId>( map[id].Keys );
        idsTyp.Sort();
        n = idsTyp.Count;
        Debug.Print( "  {0} type{1}:", n, Util.PluralSuffix( n ) );

        foreach( ElementId id2 in idsTyp )
        {
          n = map[id][id2];
          Debug.Print( "    {0} element{1}:", n, Util.PluralSuffix( n ) );
        }
      }
    }
    #endregion // Get count of all elements of each type of each category

    void RunBenchmark()
    {
      // Create a number of levels for us to play with:

      int maxLevel = 1000;
      for( int i = 3; i < maxLevel; ++i )
      {
        CreateLevel( i );
      }

      // Run a specified number of tests
      // to retrieve all levels in different
      // ways:

      int nLevels = GetElementsOfType( typeof( Level ) )
        .ToElements().Count;

      int nRuns = 1000;

      JtTimer totalTimer = new JtTimer(
        "TOTAL TIME" );

      using( totalTimer )
      {
        for( int i = 0; i < nRuns; ++i )
        {
          BenchmarkAllLevels( nLevels );
        }
      }

      totalTimer.Report( "Retrieve all levels:" );

      // Run a specified number of tests
      // to retrieve a randomly selected
      // specific level:

      nRuns = 1000;
      Random rand = new Random();
      totalTimer.Restart( "TOTAL TIME" );

      using( totalTimer )
      {
        for( int i = 0; i < nRuns; ++i )
        {
          int iLevel = rand.Next( 1, maxLevel );
          BenchmarkSpecificLevel( iLevel );
        }
      }

      totalTimer.Report(
        "Retrieve specific named level:" );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      _doc = uidoc.Document;

      //RunBenchmark();

      Element wall = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "a wall", true );

      GetInstancesIntersectingElement( wall );

      return Result.Succeeded;
    }
  }

  #region YBExporteContext
#if YBExporteContext
  internal class YBExporteContext : IExportContext
  {
    private Document _host_document;
    private IEnumerable<View> _2D_views_that_can_display_elements;

    public YBExporteContext(
      Document document,
      View activeView )
    {
      this._host_document = document;
      this._2D_views_that_can_display_elements
        = YbUtil.FindAllViewsThatCanDisplayElements(
          document );
    }

    /*
      * Lot of code here implementing the 
      * "IExportContext" interface...
      */

    private GeometryElement _get2DRepresentation(
      Element element )
    {
      View view = this._get2DViewForElement( element );
      if( view == null )
        return null;

      Options options = new Options();
      options.View = view;
      return element.get_Geometry( options );
    }

    /// <summary>
    /// Gets any 2D view where the element is displayed
    /// </summary>
    /// <param name="element"></param>
    /// <returns>A 2D view where the element is displayed</returns>
    private View _get2DViewForElement( Element element )
    {
      FilteredElementCollector collector;
      ICollection<ElementId> elements_in_view;

      foreach( View view in
        this._2D_views_that_can_display_elements )
      {
        collector = new FilteredElementCollector(
          this._host_document, view.Id )
            .WhereElementIsNotElementType();

        elements_in_view = collector.ToElementIds();

        if( elements_in_view.Contains( element.Id ) )
          return view;
      }

      return null;
    }
  }

  public static class YbUtil
  {
    public static IEnumerable<View>
      FindAllViewsThatCanDisplayElements(
        Document doc )
    {
      ElementMulticlassFilter filter
        = new ElementMulticlassFilter( new List<Type> { typeof( ViewPlan ) } );

      return new FilteredElementCollector( doc )
        .WherePasses( filter )
        .Cast<View>()
        .Where( v => !v.IsTemplate && v.CanBePrinted );
    }
  }
#endif // YBExporteContext
  #endregion // YBExporteContext
}
