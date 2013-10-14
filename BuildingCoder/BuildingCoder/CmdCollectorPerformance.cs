#region Header
//
// CmdCollectorPerformance.cs - benchmark Revit 2011 API collector performance
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
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
          UIDocument uiDoc = uiApp.ActiveUIDocument;
          Application app = uiApp.Application;
          Document doc = uiDoc.Document;

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

      Transaction trans = new Transaction( doc, "Test" );
      trans.Start();

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

      trans.Commit();

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

      Parameter parameter = wall.get_Parameter(
        "Unconnected Height" );

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

  [Transaction( TransactionMode.Automatic )]
  class CmdCollectorPerformance : IExternalCommand
  {
    Document _doc;

    #region Filter for concrete ramps
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
        
        //param = HelperParams.GetOrCreateElemSharedParam(
        //     projInfoElem, paramName, paramGroup,
        //     ParameterType.Text, false, true );

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
      // built-in parameter storing the type element id:

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

    #region Retrieve named family symbols
    static FilteredElementCollector GetStructuralColumnSymbolCollector( Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfCategory( BuiltInCategory.OST_StructuralColumns )
        .OfClass( typeof( FamilySymbol ) );
    }

    static IList<Element> Linq( Document doc, string familySymbolName )
    {
      IList<Element> elements
        = GetStructuralColumnSymbolCollector( doc )
          .ToElements();

      elements = elements.OfType<FamilySymbol>()
        .Where( x => x.Name == familySymbolName )
        .Cast<Element>()
        .ToList();

      return elements;
    }

    static IList<Element> Linq2( Document doc, string familySymbolName )
    {
      IList<Element> elements
        = GetStructuralColumnSymbolCollector( doc )
          .ToElements();

      elements = elements.OfType<FamilySymbol>()
        .Where( x => x.get_Parameter( BuiltInParameter.SYMBOL_NAME_PARAM ).AsString() == familySymbolName )
        .Cast<Element>()
        .ToList();

      return elements;
    }

    private static IList<Element> FilterRule( Document doc, string familySymbolName )
    {
      IList<Element> elements
        = GetStructuralColumnSymbolCollector( doc )
          .WherePasses(
            new ElementParameterFilter(
              new FilterStringRule(
                new ParameterValueProvider( new ElementId( BuiltInParameter.SYMBOL_NAME_PARAM ) ),
                new FilterStringEquals(), familySymbolName, true ) ) )
          .ToElements();

      return elements;
    }

    private static IList<Element> Factory( Document doc, string familySymbolName )
    {
      IList<Element> elements
        = GetStructuralColumnSymbolCollector( doc )
          .WherePasses(
            new ElementParameterFilter(
              ParameterFilterRuleFactory.CreateEqualsRule(
                new ElementId( BuiltInParameter.SYMBOL_NAME_PARAM ), familySymbolName, true ) ) )
          .ToElements();

      return elements;
    }
    #endregion // Retrieve named family symbols

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

    #region Filter for ramps
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
    #endregion // Filter for ramps


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

        foreach( Element elem in collector )
        {
          s += elem.Name + elem.Category.Name.ToString() + "\n";

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
      Level level = _doc.Create.NewLevel( elevation );
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

    public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements )
    {
      UIApplication app = commandData.Application;
      _doc = app.ActiveUIDocument.Document;

      // create a number of levels for us to play with:

      int maxLevel = 1000;
      for( int i = 3; i < maxLevel; ++i )
      {
        CreateLevel( i );
      }

      // run a specified number of tests
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

      // run a specified number of tests
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

      return Result.Failed;
    }
  }
}
