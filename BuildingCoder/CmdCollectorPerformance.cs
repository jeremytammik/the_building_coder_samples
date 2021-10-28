#region Header

//
// CmdCollectorPerformance.cs - benchmark Revit 2011 API collector performance
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using View = Autodesk.Revit.DB.View;

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
        [Transaction(TransactionMode.Manual)]
        public class Commands : IExternalCommand
        {
            public Result Execute(
                ExternalCommandData commandData,
                ref string message,
                ElementSet elements)
            {
                try
                {
                    var uiApp = commandData.Application;
                    var uidoc = uiApp.ActiveUIDocument;
                    var app = uiApp.Application;
                    var doc = uidoc.Document;

                    var sw = Stopwatch.StartNew();

                    // f5 = f1 && f4
                    // = f1 && (f2 || f3)
                    // = family instance and (door or window)

                    #region Filters and collector definitions

                    var f1
                        = new ElementClassFilter(
                            typeof(FamilyInstance));

                    var f2
                        = new ElementCategoryFilter(
                            BuiltInCategory.OST_Doors);

                    var f3
                        = new ElementCategoryFilter(
                            BuiltInCategory.OST_Windows);

                    var f4
                        = new LogicalOrFilter(f2, f3);

                    var f5
                        = new LogicalAndFilter(f1, f4);

                    var collector
                        = new FilteredElementCollector(doc);

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

                    var openings = collector
                        .WherePasses(f4)
                        .ToElements() as List<Element>;

                    var openingInstances
                        = (from instances in openings
                            where instances is FamilyInstance
                            select instances).ToList();

                    #endregion

                    var n = openingInstances.Count;
                    sw.Stop();

                    Debug.WriteLine("Time to get {0} elements: {1}ms", n, sw.ElapsedMilliseconds);

                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    message = ex.Message + ex.StackTrace;
                    return Result.Failed;
                }
            }
        }
    }

    #endregion // Type filter versus anonymous method versus LINQ by Piotr Zurek

    #region Filter for elements in a specific view having a specific phase

    [Transaction(TransactionMode.Manual)]
    public class RevitCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string messages,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var tx = new Transaction(doc, "Test");
            tx.Start();

            // use the view filter

            var collector
                = new FilteredElementCollector(
                    doc, doc.ActiveView.Id);

            // use the parameter filter.
            // get the phase id "New construction"

            var idPhase = GetPhaseId(
                "New Construction", doc);

            var provider
                = new ParameterValueProvider(
                    new ElementId((int)
                        BuiltInParameter.PHASE_CREATED));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericEquals();

            var rule
                = new FilterElementIdRule(
                    provider, evaluator, idPhase);

            var parafilter
                = new ElementParameterFilter(rule);

            collector.WherePasses(parafilter);

            TaskDialog.Show("Element Count",
                $"There are {collector.Count()} elements in the current view created with phase New Construction");

            tx.Commit();

            return Result.Succeeded;
        }

        public ElementId GetPhaseId(
            string phaseName,
            Document doc)
        {
            ElementId id = null;

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Phase));

            var phases = from Phase phase in collector
                where phase.Name.Equals(phaseName)
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
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            var r = uidoc.Selection.PickObject(
                ObjectType.Element);

            // 'Autodesk.Revit.DB.Reference.Element' is
            // obsolete: Property will be removed. Use
            // Document.GetElement(Reference) instead.
            //Wall wall = r.Element as Wall; // 2011

            var wall = doc.GetElement(r) as Wall; // 2012

            //Parameter parameter = wall.get_Parameter( "Unconnected Height" ); // 2014, causes warning CS0618: 'Autodesk.Revit.DB.Element.get_Parameter(string)' is obsolete: 'This property is obsolete in Revit 2015, as more than one parameter can have the same name on a given element. Use Element.Parameters to obtain a complete list of parameters on this Element, or Element.GetParameters(String) to get a list of all parameters by name, or Element.LookupParameter(String) to return the first available parameter with the given name.'
            var parameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM); // 2015, avoids warning, in language indepependent and more effective to look up

            var pvp
                = new ParameterValueProvider(parameter.Id);

            FilterNumericRuleEvaluator fnrv
                = new FilterNumericGreater();

            FilterRule fRule
                = new FilterDoubleRule(pvp, fnrv, 20, 1E-6);

            var filter
                = new ElementParameterFilter(fRule);

            var collector
                = new FilteredElementCollector(doc);

            // Find walls with unconnected height
            // less than or equal to 20:

            var lessOrEqualFilter
                = new ElementParameterFilter(fRule, true);

            var lessOrEqualFounds
                = collector.WherePasses(lessOrEqualFilter)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .OfClass(typeof(Wall))
                    .ToElements();

            TaskDialog.Show("Revit", $"Walls found: {lessOrEqualFounds.Count}");

            return Result.Succeeded;
        }
    }

    #endregion // Parameter filter using display name

    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdCollectorPerformance : IExternalCommand
    {
        private Document _doc;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            _doc = uidoc.Document;

            ListElementsInAssembly(_doc);

            //RunBenchmark();

            var wall = Util.SelectSingleElementOfType(
                uidoc, typeof(Wall), "a wall", true);

            GetInstancesIntersectingElement(wall);

            return Result.Succeeded;
        }

        #region Get parameter values from all Detail Component family instances

        // cf. http://forums.autodesk.com/t5/revit-api/get-parameter-value-for-a-collection-of-family-instances/m-p/5896191
        /// <summary>
        ///     Retrieve all Detail Component family instances,
        ///     read the custom parameter value from each,
        ///     assuming it is a real number, and return a
        ///     dictionary mapping all element ids to the
        ///     corresponding param values.
        /// </summary>
        private Dictionary<int, double>
            GetAllDetailComponentCustomParamValues(
                Document doc)
        {
            var dcs
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory
                        .OST_DetailComponents);

            var n = dcs.GetElementCount();

            const string param_name = "Custom_Param";

            var d
                = new Dictionary<int, double>(n);

            foreach (var dc in dcs)
            {
                var ps = dc.GetParameters(
                    param_name);

                if (1 != ps.Count)
                    throw new Exception(
                        "expected exactly one custom parameter");

                d.Add(dc.Id.IntegerValue, ps[0].AsDouble());
            }

            return d;
        }

        #endregion //Get parameter values from all Detail Component family instances

        #region Collector is iterable without ToElements

        /// <summary>
        ///     Iterate directly over the filtered element collector.
        ///     In general, there is no need to create a copy of it.
        ///     Calling ToElements creates a copy, allocating space
        ///     for that and wasting both memory and time.
        ///     No need to cast either, foreach can do that
        ///     automatically.
        /// </summary>
        private IEnumerable<Element> IterateOverCollector(
            Document doc)
        {
            // Do not do this!

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Family)).ToElements();

            var nestedFamilies
                = collector.Cast<Family>();

            var str = "";

            foreach (var f in nestedFamilies)
            {
                str = $"{str}{f.Name}\n";

                foreach (var symbolId in
                    f.GetFamilySymbolIds())
                {
                    var symbolElem = doc.GetElement(
                        symbolId);

                    str = $"{str} family type： {symbolElem.Name}\n";
                }
            }

            // Iterate directly over the collector instead.
            // No need for ToElements, which creates a copy.
            // The copy wastes memory and time.
            // No need for a cast, even.

            var families
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family));

            foreach (Family f in families)
                str = $"{str}{f.Name}\n";

            // ...
            return families;
        }

        #endregion // Collector is iterable without ToElements

        #region Get Model Extents

        /// <summary>
        ///     Return a bounding box enclosing all model
        ///     elements using only quick filters.
        /// </summary>
        private BoundingBoxXYZ GetModelExtents(Document doc)
        {
            var quick_model_elements
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent();

            var boxes = quick_model_elements
                .Where(e => null != e.Category)
                .Select(e
                    => e.get_BoundingBox(null));

            return boxes.Aggregate((a, b)
                =>
            {
                a.ExpandToContain(b);
                return a;
            });
        }

        #endregion // Get Model Extents

        #region Traverse all model elements top down Levels > Category > Family > Type > Instance

        private void TraverseInstances(Document doc)
        {
            var levels
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level));

            foreach (Level level in levels)
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

            var instances
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance));

            // Top level map.

            var // categories
                mapLevelToCategories = new
                    Dictionary<ElementId,
                        List<ElementId>>();

            // What we really need is something like this.
            // It will probably simplify things to implement
            // a custom kind of dictionary for this to add 
            // new entries very simply.

            var // instance
                map = new Dictionary<ElementId,
                    Dictionary<ElementId,
                        Dictionary<ElementId,
                            Dictionary<ElementId,
                                ElementId>>>>();

            foreach (FamilyInstance inst in instances)
            {
                var cat = inst.Category;
                var lev = doc.GetElement(inst.LevelId) as Level;
                var sym = inst.Symbol;
                var fam = sym.Family;

                Debug.Assert(null != cat, "expected valid category");
                Debug.Assert(null != lev, "expected valid level");
                Debug.Assert(null != sym, "expected valid symbol");
                Debug.Assert(null != fam, "expected valid family");

                if (map.ContainsKey(lev.Id))
                {
                    mapLevelToCategories[lev.Id].Add(cat.Id);
                }
                else
                {
                    // First time we encounter this level, 
                    // so start a new level.

                    var categoriesOnLevel
                        = new List<ElementId>(1);

                    categoriesOnLevel.Add(cat.Id);

                    mapLevelToCategories.Add(lev.Id,
                        categoriesOnLevel);
                }

                // Sort into families and types per level and category...
            }
        }

        #endregion // Traverse all model elements top down Levels > Category > Family > Type > Instance

        #region Retrieve all rooms on a given level

        /// <summary>
        ///     Retrieve all rooms on a given level for
        ///     https://forums.autodesk.com/t5/revit-api-forum/collect-all-room-in-leve-xx/m-p/6936959
        /// </summary>
        public IEnumerable<Room> GetRoomsOnLevel(
            Document doc,
            ElementId idLevel)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(SpatialElement))
                .Where(e => e.GetType() == typeof(Room))
                .Where(e => e.LevelId.IntegerValue.Equals(
                    idLevel.IntegerValue))
                .Cast<Room>();
        }

        #endregion // Retrieve all rooms on a given level

        #region Find parameter id for shared parameter element filter

        /// <summary>
        ///     Return a list of all elements with the
        ///     specified value in their shared parameter with
        ///     the given name and group. They are retrieved
        ///     using a parameter filter, and the required
        ///     parameter id is found by temporarily adding
        ///     the shared parameter to the project info.
        /// </summary>
        private static IList<Element> GetElementsMatchingParameter(
            Document doc,
            string paramName,
            string paramGroup,
            string paramValue)
        {
            IList<Element> elems = new List<Element>();

            // Determine if definition for parameter binding exists

            Definition definition = null;
            var bm = doc.ParameterBindings;
            var it = bm.ForwardIterator();
            while (it.MoveNext())
            {
                var def = it.Key;
                if (def.Name.Equals(paramName))
                {
                    definition = def;
                    break;
                }
            }

            if (definition == null) return elems; // parameter binding not defined

            using var tx = new Transaction(doc);
            tx.Start("Set temporary parameter");

            // Temporarily set project information element 
            // parameter in order to determine param.Id

            var collectorPI
                = new FilteredElementCollector(doc);

            collectorPI.OfCategory(
                BuiltInCategory.OST_ProjectInformation);

            var projInfoElem
                = collectorPI.FirstElement();

            // using http://thebuildingcoder.typepad.com/blog/2012/04/adding-a-category-to-a-shared-parameter-binding.html

            Parameter param = null;

            // param = HelperParams.GetOrCreateElemSharedParam(
            //   projInfoElem, paramName, paramGroup,
            //   ParameterType.Text, false, true );

            if (param != null)
            {
                var paraId = param.Id;

                tx.RollBack(); // discard project element change

                var provider
                    = new ParameterValueProvider(paraId);

                //FilterRule rule = new FilterStringRule( // 2021
                //  provider, new FilterStringEquals(),
                //  paramValue, true );

                FilterRule rule = new FilterStringRule( // 2022
                    provider, new FilterStringEquals(), paramValue);

                var filter
                    = new ElementParameterFilter(rule);

                var collector
                    = new FilteredElementCollector(
                        doc, doc.ActiveView.Id);

                elems = collector.WherePasses(filter)
                    .ToElements();
            }

            return elems;
        }

        #endregion // Find parameter id for shared parameter element filter

        #region GetAllElementsUsingType

        /// <summary>
        ///     Return the all elements that
        ///     use the given ElementType.
        /// </summary>
        private static FilteredElementCollector
            GetAllElementsUsingType(
                Document doc,
                ElementType et)
        {
            // Built-in parameter storing the type element id:

            var bip
                = BuiltInParameter.ELEM_TYPE_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericEquals();

            FilterRule rule = new FilterElementIdRule(
                provider, evaluator, et.Id);

            var filter
                = new ElementParameterFilter(rule);

            var collector
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(filter);

            return collector;
        }

        #endregion // GetAllElementsUsingType

        #region Electrical stuff for Martin Schmid

        private void f()
        {
            // how to get the TemperatureRatingTypeSet?

            var collector1
                = new FilteredElementCollector(_doc)
                    .OfClass(typeof(TemperatureRatingType));

            // how to get the InsulationTypeSet?

            var collector2
                = new FilteredElementCollector(_doc)
                    .OfClass(typeof(InsulationTypeSet));

            // how to get the WireSizeSet?

            var collector3
                = new FilteredElementCollector(_doc)
                    .OfClass(typeof(WireSizeSet));

            // how to get the 'first' WireMaterialType?

            var firstWireMaterialType
                = new FilteredElementCollector(_doc)
                    .OfClass(typeof(WireMaterialType))
                    .Cast<WireMaterialType>()
                    .First();
        }

        #endregion // Electrical stuff for Martin Schmid

        #region Filter for various classes

        private void f3()
        {
            var a
                = new List<ElementFilter>(3);

            a.Add(new ElementClassFilter(typeof(Family)));
            a.Add(new ElementClassFilter(typeof(Duct)));
            a.Add(new ElementClassFilter(typeof(Pipe)));

            var collector
                = new FilteredElementCollector(_doc)
                    .WherePasses(new LogicalOrFilter(a));
        }

        #endregion // Filter for various classes

        #region Filter for walls in a specific area

        // from RevitAPI.chm description of BoundingBoxIntersectsFilter Class
        // case 1260682 [Find walls in a specific area]
        private void f2()
        {
            // Use BoundingBoxIntersects filter to find
            // elements with a bounding box that intersects
            // the given outline.

            // Create a Outline, uses a minimum and maximum
            // XYZ point to initialize the outline.

            var myOutLn = new Outline(
                XYZ.Zero, new XYZ(100, 100, 100));

            // Create a BoundingBoxIntersects filter with
            // this Outline

            var filter
                = new BoundingBoxIntersectsFilter(myOutLn);

            // Apply the filter to the elements in the
            // active document.  This filter excludes all
            // objects derived from View and objects
            // derived from ElementType

            var collector
                = new FilteredElementCollector(_doc);

            var elements =
                collector.WherePasses(filter).ToElements();

            // Find all walls which don't intersect with
            // BoundingBox: use an inverted filter to match
            // elements.  Use shortcut command OfClass()
            // to find walls only

            var invertFilter
                = new BoundingBoxIntersectsFilter(myOutLn,
                    true); // inverted filter

            collector = new FilteredElementCollector(_doc);

            var notIntersectWalls
                = collector.OfClass(typeof(Wall))
                    .WherePasses(invertFilter).ToElements();
        }

        #endregion // Filter for walls in a specific area

        #region Delete non-room-separating curve elements

        // for https://forums.autodesk.com/t5/revit-api-forum/deleting-lines-that-are-not-assigned-to-the-lt-room-separation/m-p/8765491
        /// <summary>
        ///     Delete all non-room-separating curve elements
        /// </summary>
        private void DeleteNonRoomSeparators(Document doc)
        {
            var non_room_separator
                = new ElementCategoryFilter(
                    BuiltInCategory.OST_RoomSeparationLines,
                    true);

            var a
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(CurveElement))
                    .WherePasses(non_room_separator);

            doc.Delete(a.ToElementIds());
        }

        #endregion // Delete non-room-separating curve elements

        #region Filter for sheets based on browser organisation

        // for https://forums.autodesk.com/t5/revit-api-forum/change-the-sheet-issue-date-on-sheets-filtered-by-project/td-p/10633770
        /// <summary>
        ///     Filter for sheets based on browser organisation
        /// </summary>
        private IEnumerable<ElementId> FilterForSheetsByBrowserOrganisation(
            Document doc,
            string folder_name)
        {
            // Dim Els As List(Of ElementId) = FEC.WherePasses(ECF).ToElementIds _
            //   .Where( Function( k ) bOrg.GetFolderItems( k )( 1 ).Name = "Type 1" ) _
            //   .ToList

            var bOrg = BrowserOrganization
                .GetCurrentBrowserOrganizationForSheets(doc);

            var ids
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Where(s => bOrg.GetFolderItems(s.Id).First().Name.Equals(
                        folder_name))
                    .Select(e => e.Id);

            return ids;
        }

        #endregion // Filter for sheets based on browser organisation

        #region Retrieve all family names both standard and system

        /// <summary>
        ///     Retrieve all family names both standard and system
        /// </summary>
        private static IEnumerable<string> GetFamilyNames(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ElementType))
                .Cast<ElementType>()
                .Select(a => a.FamilyName)
                .Distinct();
        }

        #endregion // Retrieve all family names both standard and system

        #region Retrieve generic family symbols whose name contains "test"

        /// <summary>
        ///     Retrieve generic family symbols whose name contains "test"
        /// </summary>
        private static FilteredElementCollector
            GetGenericFamilySymbolsNamedTest(
                Document doc)
        {
            // Set up the parameter filter for the symbol name

            var id = new ElementId(BuiltInParameter
                .ALL_MODEL_TYPE_NAME);

            var provider
                = new ParameterValueProvider(id);

            FilterStringRuleEvaluator evaluator
                = new FilterStringContains();

            //FilterRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, "test", false );

            FilterRule rule = new FilterStringRule( // 2022
                provider, evaluator, "test");

            var filter
                = new ElementParameterFilter(rule);

            var genericSymbolsNamedTest
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_GenericModel)
                    .WherePasses(filter);

            return genericSymbolsNamedTest;
        }

        #endregion // Retrieve generic family symbols whose name contains "test"

        #region Test FilterStringContains false positives

        // for REVIT-172990 [Parameter filter with FilterStringContains returns false positive]
        // https://forums.autodesk.com/t5/revit-api-forum/string-parameter-filtering-is-retrieving-false-data/td-p/10012518
        public void TestFilterStringContains(Document doc)
        {
            var bip = BuiltInParameter.FIRE_RATING;

            var pid = new ElementId(bip);

            var provider
                = new ParameterValueProvider(pid);

            FilterStringRuleEvaluator evaluator
                = new FilterStringContains();

            //FilterStringRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, "/", false );

            var rule = new FilterStringRule( // 2022
                provider, evaluator, "/");

            var filter
                = new ElementParameterFilter(rule);

            var myWalls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WherePasses(filter);

            var false_positive_ids
                = new List<ElementId>();

            foreach (var wall in myWalls)
            {
                var param = wall.get_Parameter(bip);
                if (null == param) false_positive_ids.Add(wall.Id);
            }

            var s = string.Join(", ",
                false_positive_ids.Select(
                    id => id.IntegerValue.ToString()));

            var dlg = new TaskDialog("False Positives");
            dlg.MainInstruction = "False filtered walls ids: ";
            dlg.MainContent = s;
            dlg.Show();
        }

        #endregion // Test FilterStringContains false positives

        #region Retrieve door family symbols that can be used in a curtain wall

        /// <summary>
        ///     Given an existing selected curtain wall door
        ///     instance, return all other door symbols that
        ///     can be used in this curtain wall, from
        ///     https://forums.autodesk.com/t5/revit-api-forum/builtincategory-of-doors-and-curtain-wall-doors/m-p/9002988
        /// </summary>
        private static IEnumerable<FamilySymbol>
            GetDoorSymbolsForCurtainWall(
                FamilyInstance door_inst)
        {
            var doc = door_inst.Document;
            var symbol = door_inst.Symbol;
            var CW_doors
                = new FilteredElementCollector(
                        doc, symbol.GetSimilarTypes())
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .Cast<FamilySymbol>();
            return CW_doors;
        }

        #endregion // Retrieve door family symbols that can be used in a curtain wall

        #region Retrieve All (Material) Tags

        /// <summary>
        ///     Return all tags, optionally
        ///     material tags only
        /// </summary>
        private IEnumerable<IndependentTag> GetMaterialTags(
            Document doc,
            bool material_only)
        {
            var tags
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(IndependentTag))
                    .Cast<IndependentTag>();

            return material_only
                ? tags
                : tags.Where(
                    tag => tag.IsMaterialTag);
        }

        #endregion // Retrieve All (Material) Tags

        #region Pull Text from Annotation Tags

        /// <summary>
        ///     Return the text from all annotation tags
        ///     in a list of strings
        /// </summary>
        private List<string> PullTextFromAnnotationTags(
            Document doc)
        {
            var tags
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(IndependentTag));

            return new List<string>(tags
                .Cast<IndependentTag>()
                .Select(
                    t => t.TagText));
        }

        #endregion // Pull Text from Annotation Tags

        #region Retrieve openings in wall

        /// <summary>
        ///     Retrieve all openings in a given wall.
        /// </summary>
        private void GetOpeningsInWall(
            Document doc,
            Wall wall)
        {
            var id = wall.Id;

            var bic
                = BuiltInCategory.OST_SWallRectOpening;

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Opening));
            collector.OfCategory(bic);

            // explicit iteration and manual
            // checking of a property:

            var openings = new List<Element>();

            foreach (Opening e in collector)
                if (e.Host.Id.Equals(id))
                    openings.Add(e);

            // using LINQ:

            var openingsOnLevelLinq =
                from e in collector.Cast<Opening>()
                where e.Host.LevelId.Equals(id)
                select e;

            // using an anonymous method:

            var openingsOnLevelAnon =
                collector.Cast<Opening>().Where(e
                    => e.Host.Id.Equals(id));
        }

        #endregion // Retrieve openings in wall

        #region Retrieve ducts and pipes intersecting wall

        /// <summary>
        ///     Retrieve ducts and pipes intersecting a given wall.
        /// </summary>
        private FilteredElementCollector GetWallMepClashes(Wall wall)
        {
            var doc = wall.Document;

            var cats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_PipeCurves
            };

            var mepfilter = new ElementMulticategoryFilter(cats);

            var bb = wall.get_BoundingBox(null);
            var o = new Outline(bb.Min, bb.Max);

            var bbfilter = new BoundingBoxIsInsideFilter(o);

            var clashingElements
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(mepfilter)
                    .WherePasses(bbfilter);

            return clashingElements;
        }

        #endregion // Retrieve ducts and pipes intersecting wall

        #region Retrieve pipes belonging to specific system type

        /// <summary>
        ///     Retrieve all pipes belonging to
        ///     a given pipe system type, cf.
        ///     https://forums.autodesk.com/t5/revit-api-forum/filteredelementcollector-by-pipe-system-types/m-p/8620113
        /// </summary>
        private FilteredElementCollector GetPipesForSystemType(
            Document doc,
            string system_name)
        {
            // Identify the parameter to be filtered by

            var pvp
                = new ParameterValueProvider(new ElementId(
                    BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM));

            // Set string evaluator so that it equals string

            FilterStringRuleEvaluator fsre = new FilterStringEquals();

            // Create a filter rule where string value equals system_name

            //FilterRule fr = new FilterStringRule( // 2021
            //  pvp, fsre, system_name, true );

            FilterRule fr = new FilterStringRule( // 2022
                pvp, fsre, system_name);

            // Create Filter

            var epf
                = new ElementParameterFilter(fr);

            // Apply filter to filtered element collector

            return new FilteredElementCollector(doc)
                .WherePasses(epf);
        }

        #endregion // Retrieve pipes belonging to specific system type

        #region Retrieve descriptions from all AssemblyInstance objects and their members

        /// <summary>
        ///     Retrieve descriptions from all
        ///     AssemblyInstance objects and their members, cf.
        ///     15478004 [Get list of elements in Assembly]
        ///     https://forums.autodesk.com/t5/revit-api-forum/get-list-of-elements-in-assembly/m-p/8857972
        /// </summary>
        private List<string> ListElementsInAssembly(
            Document doc)
        {
            // 'FP Description' shared parameter GUID

            var guid = new Guid(
                "ac6ed937-ffb7-4b18-9c69-7541f5c0319d");

            var assemblies
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(AssemblyInstance));

            var descriptions = new List<string>();

            int n;
            string s;

            foreach (AssemblyInstance a in assemblies)
            {
                var ids = a.GetMemberIds();

                n = ids.Count;

                s = $"\r\nAssembly {a.get_Parameter(guid).AsString()} has {n} member{Util.PluralSuffix(n)}{Util.DotOrColon(n)}";

                descriptions.Add(s);

                n = 0;

                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);

                    descriptions.Add($"{n++}: {e.get_Parameter(guid).AsString()}");
                }
            }

            Debug.Print(string.Join("\r\n", descriptions));

            return descriptions;
        }

        #endregion // Retrieve descriptions from all AssemblyInstance objects and their members

        #region Retrieve all edges in model

        private void RetrieveEdges(
            Document doc,
            Dictionary<Curve, ElementId> curves)
        {
            var collector
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent();

            var opt = new Options();

            foreach (var el in collector)
                if (null != el.Category)
                {
                    var geo = el.get_Geometry(opt);
                    if (geo != null)
                        foreach (var obj in geo)
                        {
                            var sol = obj as Solid;
                            if (null != sol)
                                foreach (Edge edge in sol.Edges)
                                {
                                    var edgecurve = edge.AsCurve();
                                    curves.Add(edgecurve, el.Id);
                                }
                        }
                }
        }

        #endregion // Retrieve all edges in model

        #region Retrieve linked documents

        private IEnumerable<Document> GetLinkedDocuments(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_RvtLinks)
                .WhereElementIsNotElementType()
                .Cast<RevitLinkInstance>()
                .Select(
                    link => link.GetLinkDocument());
        }

        #endregion // Retrieve linked documents

        #region Retrieve stairs on level

        /// <summary>
        ///     Retrieve all stairs on a given level.
        /// </summary>
        private FilteredElementCollector
            GetStairsOnLevel(
                Document doc,
                Level level)
        {
            var id = level.Id;

            var bic
                = BuiltInCategory.OST_Stairs;

            var collector
                = new FilteredElementCollector(doc);

            collector.OfCategory(bic);

            // explicit iteration and manual
            // checking of a property:

            var stairs = new List<Element>();

            foreach (var e in collector)
                if (e.LevelId.Equals(id))
                    stairs.Add(e);

            // using LINQ:

            var stairsOnLevelLinq =
                from e in collector
                where e.LevelId.Equals(id)
                select e;

            // using an anonymous method:

            var stairsOnLevelAnon =
                collector.Where(e
                    => e.LevelId.Equals(id));

            // using a parameter filter:

            var bip
                = BuiltInParameter.STAIRS_BASE_LEVEL_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericEquals();

            FilterRule rule = new FilterElementIdRule(
                provider, evaluator, id);

            var filter
                = new ElementParameterFilter(rule);

            return collector.WherePasses(filter);
        }

        #endregion // Retrieve stairs on level

        #region Helper method to create some elements to play with

        /// <summary>
        ///     Create a new level at the given elevation.
        /// </summary>
        private Level CreateLevel(int elevation)
        {
            //Level level = _doc.Create.NewLevel( elevation ); // 2015
            var level = Level.Create(_doc, elevation); // 2016
            level.Name = $"Level {elevation}";
            return level;
        }

        #endregion // Helper method to create some elements to play with

        #region BenchmarkAllLevels

        /// <summary>
        ///     Benchmark several different approaches to
        ///     using filtered collectors to retrieve
        ///     all levels in the model,
        ///     and measure the time required to
        ///     create IList and List collections
        ///     from them.
        /// </summary>
        private void BenchmarkAllLevels(int nLevels)
        {
            var t = typeof(Level);
            int n;

            using (var pt = new JtTimer(
                "Empty method *"))
            {
                EmptyMethod(t);
            }

            using (var pt = new JtTimer(
                "NotElementType *"))
            {
                var a
                    = GetNonElementTypeElements();
            }

            using (var pt = new JtTimer(
                "NotElementType as IList *"))
            {
                var a
                    = GetNonElementTypeElements().ToElements();
                n = a.Count;
            }

            Debug.Assert(nLevels <= n,
                "expected to retrieve all non-element-type elements");

            using (var pt = new JtTimer(
                "NotElementType as List *"))
            {
                var a = new List<Element>(
                    GetNonElementTypeElements());

                n = a.Count;
            }

            Debug.Assert(nLevels <= n,
                "expected to retrieve all non-element-type elements");

            using (var pt = new JtTimer("Explicit"))
            {
                var a
                    = GetElementsOfTypeUsingExplicitCode(t);

                n = a.Count;
            }

            Debug.Assert(nLevels == n,
                "expected to retrieve all levels");

            using (var pt = new JtTimer("Linq"))
            {
                var a =
                    GetElementsOfTypeUsingLinq(t);

                n = a.Count();
            }

            Debug.Assert(nLevels == n,
                "expected to retrieve all levels");

            using (var pt = new JtTimer(
                "Linq as List"))
            {
                var a = new List<Element>(
                    GetElementsOfTypeUsingLinq(t));

                n = a.Count;
            }

            Debug.Assert(nLevels == n,
                "expected to retrieve all levels");

            using (var pt = new JtTimer("Collector"))
            {
                var a
                    = GetElementsOfType(t);
            }

            using (var pt = new JtTimer(
                "Collector as IList"))
            {
                var a
                    = GetElementsOfType(t).ToElements();

                n = a.Count;
            }

            Debug.Assert(nLevels == n,
                "expected to retrieve all levels");

            using (var pt = new JtTimer(
                "Collector as List"))
            {
                var a = new List<Element>(
                    GetElementsOfType(t));

                n = a.Count;
            }

            Debug.Assert(nLevels == n,
                "expected to retrieve all levels");
        }

        #endregion // BenchmarkAllLevels

        #region BenchmarkSpecificLevel

        /// <summary>
        ///     Benchmark the use of a parameter filter versus
        ///     various kinds of post processing of the
        ///     results returned by the filtered element
        ///     collector to find the level specified by
        ///     iLevel.
        /// </summary>
        private void BenchmarkSpecificLevel(int iLevel)
        {
            var t = typeof(Level);
            var name = $"Level {iLevel}";
            Element level;

            using (var pt = new JtTimer(
                "Empty method *"))
            {
                level = EmptyMethod(
                    t, name);
            }

            level = null;

            using (var pt = new JtTimer(
                "Collector with no name check *"))
            {
                level = GetFirstElementOfType(t);
            }

            Debug.Assert(null != level, "expected to find a valid level");

            level = null;

            using (var pt = new JtTimer(
                "Parameter filter"))
            {
                //level = GetFirstElementOfTypeWithBipString(
                //  t, BuiltInParameter.ELEM_NAME_PARAM, name );

                level = GetFirstElementOfTypeWithBipString(
                    t, BuiltInParameter.DATUM_TEXT, name);
            }

            Debug.Assert(null != level,
                "expected to find a valid level");

            level = null;

            using (var pt = new JtTimer("Explicit"))
            {
                level = GetFirstNamedElementOfTypeUsingExplicitCode(
                    t, name);
            }

            Debug.Assert(null != level, "expected to find a valid level");
            level = null;

            using (var pt = new JtTimer("Linq"))
            {
                level = GetFirstNamedElementOfTypeUsingLinq(
                    t, name);
            }

            Debug.Assert(null != level, "expected to find a valid level");
            level = null;

            using (var pt = new JtTimer(
                "Anonymous named"))
            {
                level = GetFirstNamedElementOfTypeUsingAnonymousButNamedMethod(
                    t, name);
            }

            Debug.Assert(null != level, "expected to find a valid level");
            level = null;

            using (var pt = new JtTimer("Anonymous"))
            {
                level = GetFirstNamedElementOfTypeUsingAnonymousMethod(
                    t, name);
            }

            Debug.Assert(null != level, "expected to find a valid level");
        }

        #endregion // BenchmarkSpecificLevel

        #region Family tree test

        public static void CreateFamilyTreeTest(
            Document myDoc)
        {
            IEnumerable<Element> familiesCollector =
                new FilteredElementCollector(myDoc)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    // (family, familyInstances):
                    .GroupBy(fi => fi.Symbol.Family)
                    .Select(f => f.Key);

            var mapCatToFam = new Dictionary<string,
                List<Element>>();

            var categoryList = new Dictionary<string,
                Category>();

            foreach (var f in familiesCollector)
            {
                var catName = f.Category.Name;

                if (mapCatToFam.ContainsKey(catName))
                {
                    mapCatToFam[catName].Add(f);
                }
                else
                {
                    mapCatToFam.Add(catName,
                        new List<Element> {f});

                    categoryList.Add(catName,
                        f.Category);
                }
            }
        }

        #endregion // Family tree test

        #region Is element hidden in view by crop box, visibility or category?

        /// <summary>
        ///     Checks whether a given Revit element 'e' is
        ///     hidden in a specified view 'v'.
        ///     If v has a crop box defined, e is
        ///     considered hidden if its bounding box is
        ///     outside or less than 25% contained in the
        ///     crop box. If e is not eliminated as hidden
        ///     by that test, its IsHidden predicate is
        ///     checked, followed by the visibility of its
        ///     category and all its parent categories in
        ///     the given view.
        ///     Return true if the given element e is hidden
        ///     in the view v. This might be due to:
        ///     - e lies outside the view crop box
        ///     - e is specifically hidden in the view, by element
        ///     - the category of e or one of its parent
        ///     categories is hidden in v.
        /// </summary>
        private bool IsElementHiddenInView(
            Element e,
            View v)
        {
            if (v.CropBoxActive)
            {
                var viewBox = v.CropBox;
                var elBox = e.get_BoundingBox(v);

                var transInv = v.CropBox.Transform.Inverse;

                elBox.Max = transInv.OfPoint(elBox.Max);
                elBox.Min = transInv.OfPoint(elBox.Min);

                // The transform above might switch 
                // max and min values.

                if (elBox.Min.X > elBox.Max.X)
                {
                    var tmpP = elBox.Min;
                    elBox.Min = new XYZ(elBox.Max.X, elBox.Min.Y, 0);
                    elBox.Max = new XYZ(tmpP.X, elBox.Max.Y, 0);
                }

                if (elBox.Min.Y > elBox.Max.Y)
                {
                    var tmpP = elBox.Min;
                    elBox.Min = new XYZ(elBox.Min.X, elBox.Max.Y, 0);
                    elBox.Max = new XYZ(tmpP.X, elBox.Min.Y, 0);
                }

                if (elBox.Min.X > viewBox.Max.X
                    || elBox.Max.X < viewBox.Min.X
                    || elBox.Min.Y > viewBox.Max.Y
                    || elBox.Max.Y < viewBox.Min.Y)
                    return true;

                var inside = new BoundingBoxXYZ();

                double x, y;

                x = elBox.Max.X;

                if (elBox.Max.X > viewBox.Max.X)
                    x = viewBox.Max.X;

                y = elBox.Max.Y;

                if (elBox.Max.Y > viewBox.Max.Y)
                    y = viewBox.Max.Y;

                inside.Max = new XYZ(x, y, 0);

                x = elBox.Min.X;

                if (elBox.Min.X < viewBox.Min.X)
                    x = viewBox.Min.X;

                y = elBox.Min.Y;

                if (elBox.Min.Y < viewBox.Min.Y)
                    y = viewBox.Min.Y;

                inside.Min = new XYZ(x, y, 0);

                var eBBArea = (elBox.Max.X - elBox.Min.X)
                              * (elBox.Max.Y - elBox.Min.Y);

                var einsideArea =
                    (inside.Max.X - inside.Min.X)
                    * (inside.Max.Y - inside.Min.Y);

                var factor = einsideArea / eBBArea;

                if (factor < 0.25)
                    return true;
            }

            var hidden = e.IsHidden(v);

            if (!hidden)
            {
                var cat = e.Category;

                while (null != cat && !hidden)
                {
                    hidden = !cat.get_Visible(v);
                    cat = cat.Parent;
                }
            }

            return hidden;
        }

        #endregion // Is element hidden in view by crop box, visibility or category?

        #region Return element id of crop box for a given view

        // http://thebuildingcoder.typepad.com/blog/2013/09/rotating-a-plan-view.html#comment-3734421721
        /// <summary>
        ///     Return element id of crop box for a given view.
        ///     The built-in parameter ID_PARAM of the crop box
        ///     contains the element id of the view it is used in;
        ///     e.g., the crop box 'points' to the view using it
        ///     via ID_PARAM. Therefore, we can use a parameter
        ///     filter to retrieve all crop boxes with the
        ///     view's element id in that parameter.
        /// </summary>
        private ElementId GetCropBoxFor(View view)
        {
            var provider
                = new ParameterValueProvider(new ElementId(
                    (int) BuiltInParameter.ID_PARAM));

            var rule
                = new FilterElementIdRule(provider,
                    new FilterNumericEquals(), view.Id);

            var filter
                = new ElementParameterFilter(rule);

            return new FilteredElementCollector(view.Document)
                .WherePasses(filter)
                .ToElementIds()
                .Where(a => a.IntegerValue
                            != view.Id.IntegerValue)
                .FirstOrDefault();
        }

        #endregion // Get element id of crop box

        #region Retrieve Family Instances Satisfying Filter Rule

        // https://forums.autodesk.com/t5/revit-api-forum/how-to-filter-element-which-satisfy-filter-rule/m-p/8021978
        /// <summary>
        ///     Retrieve Family Instances Satisfying Filter Rule
        /// </summary>
        private void GetFamilyInstancesSatisfyingFilterRule(
            Document doc)
        {
            var pfes
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ParameterFilterElement));

            foreach (ParameterFilterElement pfe in pfes)
            {
                #region Get Filter Name, Category and Elements underlying the categories

                var catfilter
                    = new ElementMulticategoryFilter(
                        pfe.GetCategories());

                var elemsByFilter
                    = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .WherePasses(catfilter);

                //foreach( FilterRule rule in pfe.GetRules() ) // 2018
                //{
                //  IEnumerable<Element> elemsByFilter2
                //    = elemsByFilter.Where( e
                //      => rule.ElementPasses( e ) );
                //}

                var ef = pfe.GetElementFilter(); // 2019
                IEnumerable<Element> elemsByFilter2
                    = elemsByFilter.WherePasses(ef);

                #endregion
            }
        }

        #endregion // Retrieve Family Instances Satisfying Filter Rule

        #region Determine element count for each type of each category

        /// <summary>
        ///     Determine element count for each type of each category
        /// </summary>
        private void GetCountPerTypePerCategory(Document doc)
        {
            var els
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType();

            // Map category id to dictionary mapping
            // type id to element count

            var map
                = new Dictionary<ElementId, Dictionary<ElementId, int>>();

            foreach (var e in els)
            {
                var cat = e.Category;

                var idCat = null == cat
                    ? ElementId.InvalidElementId
                    : e.Category.Id;

                var idTyp = e.GetTypeId()
                            ?? ElementId.InvalidElementId;

                if (!map.ContainsKey(idCat)) map.Add(idCat, new Dictionary<ElementId, int>());
                if (!map[idCat].ContainsKey(idTyp)) map[idCat].Add(idTyp, 0);
                ++map[idCat][idTyp];
            }

            var idsCat = new List<ElementId>(map.Keys);
            idsCat.Sort();
            var n = idsCat.Count;
            Debug.Print("{0} categor{1}:", n, Util.PluralSuffixY(n));

            foreach (var id in idsCat)
            {
                var idsTyp = new List<ElementId>(map[id].Keys);
                idsTyp.Sort();
                n = idsTyp.Count;
                Debug.Print("  {0} type{1}:", n, Util.PluralSuffix(n));

                foreach (var id2 in idsTyp)
                {
                    n = map[id][id2];
                    Debug.Print("    {0} element{1}:", n, Util.PluralSuffix(n));
                }
            }
        }

        #endregion // Get count of all elements of each type of each category

        #region Return element id of "Light Source" graphics style

        /// <summary>
        ///     Return element id of "Light Source" graphics style
        /// </summary>
        private ElementId GetLightSourceGraphicsStyleElementId(
            Document doc)
        {
            var graphic_styles
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(GraphicsStyle));

            return graphic_styles
                .First(e
                    => e.Name.ToLower().Contains("light source"))
                .Id;
        }

        #endregion // Return element id of "Light Source" graphics style

        private void RunBenchmark()
        {
            // Create a number of levels for us to play with:

            var maxLevel = 1000;
            for (var i = 3; i < maxLevel; ++i) CreateLevel(i);

            // Run a specified number of tests
            // to retrieve all levels in different
            // ways:

            var nLevels = GetElementsOfType(typeof(Level))
                .ToElements().Count;

            var nRuns = 1000;

            var totalTimer = new JtTimer(
                "TOTAL TIME");

            using (totalTimer)
            {
                for (var i = 0; i < nRuns; ++i) BenchmarkAllLevels(nLevels);
            }

            totalTimer.Report("Retrieve all levels:");

            // Run a specified number of tests
            // to retrieve a randomly selected
            // specific level:

            nRuns = 1000;
            var rand = new Random();
            totalTimer.Restart("TOTAL TIME");

            using (totalTimer)
            {
                for (var i = 0; i < nRuns; ++i)
                {
                    var iLevel = rand.Next(1, maxLevel);
                    BenchmarkSpecificLevel(iLevel);
                }
            }

            totalTimer.Report(
                "Retrieve specific named level:");
        }

        #region Retrieve all exterior walls

        /// <summary>
        ///     Wall type predicate for exterior wall function
        /// </summary>
        private bool IsExterior(WallType wallType)
        {
            var p = wallType.get_Parameter(
                BuiltInParameter.FUNCTION_PARAM);

            Debug.Assert(null != p, "expected wall type "
                                    + "to have wall function parameter");

            var f = (WallFunction) p.AsInteger();

            return WallFunction.Exterior == f;
        }

        /// <summary>
        ///     Return all exterior walls, cf.
        ///     https://forums.autodesk.com/t5/revit-api-forum/how-do-i-get-all-the-outermost-walls-in-the-model/m-p/7998948
        /// </summary>
        private IEnumerable<Element> GetAllExteriorWalls(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w =>
                    IsExterior(w.WallType));
        }

        #endregion // Retrieve all exterior walls

        #region Get Families of a given Category

        // for http://forums.autodesk.com/t5/revit-api-forum/having-trouble-filtering-to-ost-titleblocks/m-p/6827759
        private static bool FamilyFirstSymbolCategoryEquals(
            Family f,
            BuiltInCategory bic)
        {
            var doc = f.Document;

            var ids = f.GetFamilySymbolIds();

            var cat = 0 == ids.Count
                ? null
                : doc.GetElement(ids.First()).Category;

            return null != cat
                   && cat.Id.IntegerValue.Equals((int) bic);
        }

        private static void GetFamiliesOfCategory(
            Document doc,
            BuiltInCategory bic)
        {
            // This does not work:

            //FilteredElementCollector collector
            //  = new FilteredElementCollector( doc );
            //ICollection<Element> titleFrames = collector
            //  .OfCategory( BuiltInCategory.OST_TitleBlocks )
            //  .OfClass( typeof( Family ) )
            //  .ToElements();

            var families
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(f =>
                        FamilyFirstSymbolCategoryEquals(f, bic));
        }

        #endregion // Get Families of a given Category

        #region Get all model elements

        /// <summary>
        ///     Return all model elements, cf.
        ///     http://forums.autodesk.com/t5/revit-api/traverse-all-model-elements-in-a-project-top-down-approach/m-p/5815247
        /// </summary>
        private IEnumerable<Element> GetAllModelElements(
            Document doc)
        {
            var opt = new Options();

            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .Where(e
                    => null != e.Category
                       && null != e.get_Geometry(opt));
        }

        private IList<Element> GetFamilyInstanceModelElements(
            Document doc)
        {
            var familyInstanceFilter
                = new ElementClassFilter(
                    typeof(FamilyInstance));

            var familyInstanceCollector
                = new FilteredElementCollector(doc);

            var elementsCollection
                = familyInstanceCollector.WherePasses(
                    familyInstanceFilter).ToElements();

            IList<Element> modelElements
                = new List<Element>();

            foreach (var e in elementsCollection)
                if (null != e.Category
                    && null != e.LevelId
                    && null != e.get_Geometry(new Options())
                )
                    modelElements.Add(e);
            return modelElements;
        }

        /// <summary>
        ///     Select all physical items, cf.
        ///     http://forums.autodesk.com/t5/revit-api-forum/select-all-physical-items-in-model/m-p/6822940
        /// </summary>
        private IEnumerable<Element> SelectAllPhysicalElements(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .Where(e => e.IsPhysicalElement());
        }

        #endregion // Get all model elements

        #region Retrieve a sorted list of all levels

        /// <summary>
        ///     Return a sorted list of all levels
        /// </summary>
        private IOrderedEnumerable<Level> GetSortedLevels(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderByDescending(lev => lev.Elevation);
        }

        /// <summary>
        ///     Return a suitable level for the given element.
        ///     If the element has no level defined, return the
        ///     closest one below the given element Z.
        /// </summary>
        public static Level GetLevelFor(
            Element e,
            double element_z,
            IOrderedEnumerable<Level> sorted_levels)
        {
            Level level = null;

            // Retrieve the element's Level property:

            var lid = e.LevelId;

            if (null != lid
                && ElementId.InvalidElementId != lid)
            {
                var doc = e.Document;
                level = doc.GetElement(lid) as Level;
            }
            else
            {
                //// If no level is defined, grab the first
                //// one below the element's Z from the list 
                //// of levels sorted by elevation:

                //if( element_z < sorted_levels.First().Elevation )
                //{
                //  level = sorted_levels.First();
                //}
                //else
                //{
                //  foreach( Level l in sorted_levels )
                //  {
                //    double elev = l.Elevation;

                //    if( Util.IsEqual( element_z, elev )
                //      || element_z <= elev )
                //    {
                //      level = l;
                //      break;
                //    }
                //  }
                //  if( null == level )
                //  {
                //    level = sorted_levels.Last();
                //  }
                //}

                // Improved algorithm picking
                // closest level below or equal

                //foreach( Level l in sorted_levels )
                //{
                //  if( Util.IsLessOrEqual( 
                //    l.Elevation, element_z ) )
                //  {
                //    level = l;
                //    break;
                //  }
                //}

                level = sorted_levels.FirstOrDefault(
                    l => Util.IsLessOrEqual(
                        l.Elevation, element_z));

                if (null == level) level = sorted_levels.Last();
            }

            return level;
        }

        #endregion // Retrieve a sorted list of all levels

        #region Retrieve all areas belonging to a specific area scheme

        /// <summary>
        ///     Return the area scheme name of a given area element
        ///     using only generic Element Parameter access.
        /// </summary>
        private static string GetAreaSchemeNameFromArea(Element e)
        {
            if (!(e is Area))
                throw new ArgumentException(
                    "Expected Area element input argument.");

            var doc = e.Document;

            var p = e.get_Parameter(
                BuiltInParameter.AREA_SCHEME_ID);

            if (null == p)
                throw new ArgumentException(
                    "element lacks AREA_SCHEME_ID parameter");

            var areaScheme = doc.GetElement(p.AsElementId());

            p = areaScheme.get_Parameter(
                BuiltInParameter.AREA_SCHEME_NAME);

            if (null == p)
                throw new ArgumentException(
                    "area scheme lacks AREA_SCHEME_NAME parameter");

            return p.AsString();
        }

        /// <summary>
        ///     Retrieve all areas belonging to
        ///     a specific area scheme.
        /// </summary>
        public IEnumerable<Element> GetAreasInAreaScheme(
            Document doc,
            string areaSchemeName)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .OfClass(typeof(SpatialElement))
                .Where(e => areaSchemeName.Equals(
                    GetAreaSchemeNameFromArea(e)));
        }

        #endregion // Retrieve all areas belonging to a specific area scheme

        #region Filter for concrete ramps

        /// <summary>
        ///     Retrieve all ramps
        /// </summary>
        private void f_ramps(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Ramps)
                    .WhereElementIsNotElementType();

            foreach (var e in collector) Debug.Print(e.GetType().Name);
        }

        /// <summary>
        ///     Retrieve all concrete ramps
        /// </summary>
        private IEnumerable<Element> findConcreteRamps(Document doc)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Ramps)
                //.Where( e => e.Category.Id.IntegerValue.Equals( 
                //  (int) BuiltInCategory.OST_Ramps ) )
                .Where(e =>
                {
                    var id = e.GetValidTypes().First(
                        id2 => id2.Equals(e.GetTypeId()));

                    var m = doc.GetElement(doc.GetElement(id)
                        .get_Parameter(
                            BuiltInParameter.RAMP_ATTR_MATERIAL)
                        .AsElementId()) as Material;

                    return m.Name.Contains("Concrete");
                });
        }

        #endregion // Filter for concrete ramps

        #region Filter for detail curves

        private void f_detail_curves()
        {
            var collector
                = new FilteredElementCollector(_doc);

            collector.OfClass(typeof(DetailCurve));
        }

        public void GetListOfLinestyles(Document doc)
        {
            var c = doc.Settings.Categories.get_Item(
                BuiltInCategory.OST_Lines);

            var subcats = c.SubCategories;

            foreach (Category lineStyle in subcats)
                TaskDialog.Show("Line style", $"Linestyle {lineStyle.Name} id {lineStyle.Id}");


            var collector
                = new FilteredElementCollector(doc);

            var fi
                = new ElementCategoryFilter(
                    BuiltInCategory.OST_GenericLines, true);

            ICollection<Element> collection
                = collector.OfClass(typeof(CurveElement))
                    .WherePasses(fi)
                    .ToElements();

            TaskDialog.Show("Number of curves",
                collection.Count.ToString());

            var detail_lines = new List<Element>();

            foreach (var e in collection)
                if (e is DetailLine)
                    detail_lines.Add(e);

            TaskDialog.Show("Number of Detail Lines",
                detail_lines.Count.ToString());

            var some_detail_lines = new List<Element>();
            foreach (DetailLine dl in detail_lines)
                if (dl.LineStyle.Name == "MyNewLineStyle")
                    some_detail_lines.Add(dl);

            TaskDialog.Show(
                "Number of Detail Lines of MyNewLineStyle",
                some_detail_lines.Count.ToString());


            Category targetLineStyle = null;

            var gstyles
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(GraphicsStyle))
                    .Cast<GraphicsStyle>()
                    .Where(gs => gs.GraphicsStyleCategory.Id.IntegerValue
                                 == targetLineStyle.Id.IntegerValue);

            var targetGraphicsStyleId
                = gstyles.FirstOrDefault().Id;

            var filter_detail
                = new CurveElementFilter(
                    CurveElementType.DetailCurve);

            var frule_typeId
                = ParameterFilterRuleFactory.CreateEqualsRule(
                    new ElementId(
                        BuiltInParameter.BUILDING_CURVE_GSTYLE),
                    targetGraphicsStyleId);

            var filter_type
                = new ElementParameterFilter(
                    new List<FilterRule> {frule_typeId});

            IEnumerable<Element> lines
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsCurveDriven()
                    .WherePasses(filter_detail)
                    .WherePasses(filter_type);
        }

        #endregion // Filter for detail curves

        #region Filter for views

        private FilteredElementCollector GetViews(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View));
        }

        public static List<View> GetElementViews(
            List<Element> a,
            List<View> views)
        {
            var returnViews = new List<View>();
            foreach (var view in views)
            {
                var coll
                    = new FilteredElementCollector(
                        view.Document, view.Id);

                coll = coll.WhereElementIsNotElementType();

                var elementList = coll.ToList();

                foreach (var e1 in a)
                {
                    var e2 = elementList.Where(
                            x => x.Id == e1.Id)
                        .FirstOrDefault();

                    if (e2 != null
                        && null == returnViews.Where(
                            x => x.Id == view.Id).FirstOrDefault())
                        returnViews.Add(view);
                }
            }

            return returnViews;
        }

        public static void GetViewsAndDrawingSheets1(
            Document doc,
            List<View> views,
            List<ViewSheet> viewSheets)
        {
            var coll
                = new FilteredElementCollector(doc);

            coll.OfClass(typeof(View));

            foreach (var e in coll)
                if (e is View view)
                {
                    if (null != view.CropBox)
                        views.Add(view);
                }
                else if (e is ViewSheet sheet)
                {
                    viewSheets.Add(sheet);
                }
        }

        public static void GetViewsAndDrawingSheets(
            Document doc,
            List<View> views,
            List<ViewSheet> viewSheets)
        {
            var coll
                = new FilteredElementCollector(doc);

            coll.OfClass(typeof(View));

            foreach (var e in coll)
                if (e is View view)
                {
                    if (!view.IsTemplate)
                        views.Add(view);
                }
                else if (e is ViewSheet sheet)
                {
                    viewSheets.Add(sheet);
                }
        }

        /// <summary>
        ///     Predicate for views with template for
        ///     GetViewsWithTemplate
        /// </summary>
        private static bool ViewHasTemplate(View v)
        {
            return !v.IsTemplate
                   && (v.CanUseTemporaryVisibilityModes()
                       || ViewType.Schedule == v.ViewType
                       && !((ViewSchedule) v).IsTitleblockRevisionSchedule);
        }

        /// <summary>
        ///     Return all views with a "View Template"
        ///     parameter for
        ///     https://forums.autodesk.com/t5/revit-api-forum/get-all-views-that-accept-view-template/m-p/9104937
        /// </summary>
        private static IEnumerable<View> GetViewsWithTemplate(
            Document doc)
        {
            var views
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(View));

            //BuiltInParameter bip_t
            //  = BuiltInParameter.VIEW_TEMPLATE;
            //IEnumerable<View> views_w_t = views
            //  .Where( v
            //    => null != v.get_Parameter( bip_t ) )
            //  .Cast<View>();

            IEnumerable<View> views_w_t = views
                .Cast<View>()
                .Where(v => ViewHasTemplate(v))
                .OrderBy(v => v.Name);

            return views_w_t;
        }

        #endregion // Filter for views

        #region Retrieve all family instances of specific named family and type

        /// <summary>
        ///     Get instances by family name then type name
        /// </summary>
        private static IEnumerable<FamilyInstance>
            GetFamilyInstancesByFamilyAndType(
                Document doc,
                string familyName,
                string typeName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.Family.Name.Equals(familyName)) // family
                .Where(x => x.Symbol.Name.Equals(typeName)); // family type               
        }

        /// <summary>
        ///     Get instances by element type
        /// </summary>
        private static IEnumerable<Element> GetInstancesOfElementType(
            ElementType type)
        {
            var iid = type.Id.IntegerValue;
            return new FilteredElementCollector(type.Document)
                .WhereElementIsNotElementType()
                //.OfClass( typeof( FamilyInstance ) ) // excludes walls, floors, pipes, etc.; all system family elements
                .Where(e => e.GetTypeId().IntegerValue.Equals(
                    iid));
        }

        #endregion // Retrieve all family instances of specific named family and type

        #region Return first title block family symbol of specific named family and type

        /// <summary>
        ///     Get title block family symbol (= definition)
        ///     by family name then type name
        /// </summary>
        private static FamilySymbol
            GetTitleBlockSymbolByFamilyAndType(
                Document doc,
                string familyName,
                string typeName)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals(familyName)) // family
                .FirstOrDefault(x => x.Name == typeName); // family type
        }

        /// <summary>
        ///     Predicate returning true for the desired title
        ///     block type and false for all others.
        /// </summary>
        private bool IsCorrectTitleBlock(Element e)
        {
            return false;
        }

        private ElementId GetSpecificTitleBlockType(Document doc)
        {
            // Create a filter to get a specific title block type:

            var title_block_type
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsElementType()
                    .FirstOrDefault(e
                        => IsCorrectTitleBlock(e));

            // Use null-conditional Elvis operator:

            return title_block_type?.Id;
        }

        #endregion // Return first title block family symbol of specific named family and type

        #region Retrieve named family symbols using either LINQ or a parameter filter

        /// <summary>
        ///     Return the family symbols that can be used for
        ///     note blocks, excluding the ones that lack a specific
        ///     paramter. For
        ///     https://forums.autodesk.com/t5/revit-api-forum/determine-if-a-parameter-exists-for-noteblockfamiles/m-p/9442331
        /// </summary>
        private static IEnumerable<FamilySymbol>
            GetNoteBlockSymbolsLackingParameterNamed(
                Document doc,
                string parameter_name)
        {
            var ids_family
                = ViewSchedule.GetValidFamiliesForNoteBlock(
                    doc);

            var symbols
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(sym => ids_family.Contains(sym.Family.Id))
                    .Where(sym => null == sym.LookupParameter(parameter_name));

            return symbols;
        }

        private static FilteredElementCollector
            GetStructuralColumnSymbolCollector(
                Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol));
        }

        private static IEnumerable<Element> Linq(
            Document doc,
            string familySymbolName)
        {
            return GetStructuralColumnSymbolCollector(doc)
                .Where(x => x.Name == familySymbolName);
        }

        private static IEnumerable<Element> Linq2(
            Document doc,
            string familySymbolName)
        {
            return GetStructuralColumnSymbolCollector(doc)
                .Where(x => x.get_Parameter(
                        BuiltInParameter.SYMBOL_NAME_PARAM)
                    .AsString() == familySymbolName);
        }

        private static IEnumerable<Element> FilterRule(
            Document doc,
            string familySymbolName)
        {
            //FilterStringRule r = new FilterStringRule( // 2021
            //  new ParameterValueProvider(
            //    new ElementId( BuiltInParameter.SYMBOL_NAME_PARAM ) ),
            //  new FilterStringEquals(), familySymbolName, true );

            var r = new FilterStringRule( // 2022
                new ParameterValueProvider(
                    new ElementId(BuiltInParameter.SYMBOL_NAME_PARAM)),
                new FilterStringEquals(), familySymbolName);

            return GetStructuralColumnSymbolCollector(doc)
                .WherePasses(new ElementParameterFilter(r));
        }

        private static IEnumerable<Element> Factory(
            Document doc,
            string familySymbolName)
        {
            return GetStructuralColumnSymbolCollector(doc)
                .WherePasses(
                    new ElementParameterFilter(
                        ParameterFilterRuleFactory.CreateEqualsRule(
                            new ElementId(BuiltInParameter.SYMBOL_NAME_PARAM),
                            familySymbolName, true)));
        }

        #endregion // Retrieve named family symbols using either LINQ or a parameter filter

        #region Retrieve family instances intersecting BIM element

        /// <summary>
        ///     Retrieve all family instances intersecting a
        ///     given BIM element, e.g. all columns
        ///     intersecting a wall.
        /// </summary>
        private void GetInstancesIntersectingElement(Element e)
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

            var doc = e.Document;

            var solid = e.get_Geometry(new Options())
                .OfType<Solid>()
                .Where(s => null != s && !s.Edges.IsEmpty)
                .FirstOrDefault();

            var intersectingInstances
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .WherePasses(new ElementIntersectsSolidFilter(
                        solid));

            var n1 = intersectingInstances.Count();

            intersectingInstances
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .WherePasses(new ElementIntersectsElementFilter(
                        e));

            var n = intersectingInstances.Count();

            Debug.Assert(n.Equals(n1),
                "expected solid intersection to equal element intersection");

            var result = string.Format(
                "{0} family instance{1} intersect{2} the "
                + "selected element {3}{4}",
                n, Util.PluralSuffix(n),
                1 == n ? "s" : "",
                Util.ElementDescription(e),
                Util.DotOrColon(n));

            var id_list = 0 == n
                ? string.Empty
                : $"{string.Join(", ", intersectingInstances.Select(x => x.Id.IntegerValue.ToString()))}.";

            Util.InfoMsg2(result, id_list);
        }

        /// <summary>
        ///     Retrieve all beam family instances
        ///     intersecting two columns, cf.
        ///     http://forums.autodesk.com/t5/revit-api/check-to-see-if-beam-exists/m-p/6223562
        /// </summary>
        private FilteredElementCollector
            GetBeamsIntersectingTwoColumns(
                Element column1,
                Element column2)
        {
            var doc = column1.Document;

            if (column2.Document.GetHashCode() != doc.GetHashCode())
                throw new ArgumentException(
                    "Expected two columns from same document.");

            var intersectingStructuralFramingElements
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WherePasses(new ElementIntersectsElementFilter(column1))
                    .WherePasses(new ElementIntersectsElementFilter(column2));

            var n = intersectingStructuralFramingElements.Count();

            var result = string.Format(
                "{0} structural framing family instance{1} "
                + "intersect{2} the two beams{3}",
                n, Util.PluralSuffix(n),
                1 == n ? "s" : "",
                Util.DotOrColon(n));

            var id_list = 0 == n
                ? string.Empty
                : $"{string.Join(", ", intersectingStructuralFramingElements.Select(x => x.Id.IntegerValue.ToString()))}.";

            Util.InfoMsg2(result, id_list);

            return intersectingStructuralFramingElements;
        }

        /// <summary>
        ///     Dummy placeholder function to return solid from element, cf.
        ///     https://thebuildingcoder.typepad.com/blog/2012/06/real-world-concrete-corner-coordinates.html
        /// </summary>
        private Solid GetSolid(Element e)
        {
            return null;
        }

        /// <summary>
        ///     Collect the element ids of all elements in the
        ///     linked documents intersecting the given element.
        /// </summary>
        /// <param name="e">Target element</param>
        /// <param name="links">Linked documents</param>
        /// <param name="ids">Return intersecting element ids</param>
        /// <returns>Number of intersecting elements found</returns>
        private int GetIntersectingLinkedElementIds(
            Element e,
            IList<RevitLinkInstance> links,
            List<ElementId> ids)
        {
            var count = ids.Count();
            var solid = GetSolid(e);

            foreach (var i in links)
            {
                var transform = i.GetTransform(); // GetTransform or GetTotalTransform or what?
                if (!transform.AlmostEqual(Transform.Identity))
                    solid = SolidUtils.CreateTransformed(
                        solid, transform.Inverse);
                var filter
                    = new ElementIntersectsSolidFilter(solid);

                var intersecting
                    = new FilteredElementCollector(i.GetLinkDocument())
                        .WherePasses(filter);

                ids.AddRange(intersecting.ToElementIds());
            }

            return ids.Count - count;
        }

        #endregion // Retrieve family instances intersecting BIM element

        #region More parameter filter samples

        // 383_param_filter.htm

        private void f1(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc);

            ICollection<Element> levels
                = collector.OfClass(typeof(Level))
                    .ToElements();

            for (var i = 0; i < levels.Count; i++)
            {
                var levelId = levels.ElementAt(i).Id;

                var levelFilter
                    = new ElementLevelFilter(levelId);

                collector = new FilteredElementCollector(doc);

                ICollection<Element> allOnLevel
                    = collector.WherePasses(levelFilter)
                        .ToElements();

                // . . .
            }
        }

        private void f2(Document doc, Level level)
        {
            var collector
                = new FilteredElementCollector(doc);

            collector.OfCategory(
                BuiltInCategory.OST_StructuralFraming);

            collector.OfClass(typeof(FamilyInstance));

            var bip = BuiltInParameter
                .INSTANCE_REFERENCE_LEVEL_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericGreater();

            var idRuleValue = level.Id;

            var rule
                = new FilterElementIdRule(
                    provider, evaluator, idRuleValue);

            var filter
                = new ElementParameterFilter(rule);

            collector.WherePasses(filter);
        }

        [Transaction(TransactionMode.ReadOnly)]
        public class RevitCommand : IExternalCommand
        {
            public Result Execute(
                ExternalCommandData commandData,
                ref string messages,
                ElementSet elements)
            {
                var app = commandData.Application;
                var doc = app.ActiveUIDocument.Document;

                var id = new ElementId(
                    BuiltInParameter.ELEM_ROOM_NUMBER);

                var provider
                    = new ParameterValueProvider(id);

                FilterStringRuleEvaluator evaluator
                    = new FilterStringEquals();

                var sRoomNumber = "1";

                //FilterRule rule = new FilterStringRule( // 2021
                //  provider, evaluator, sRoomNumber, false );

                FilterRule rule = new FilterStringRule( // 2022
                    provider, evaluator, sRoomNumber);

                var filter
                    = new ElementParameterFilter(rule);

                var collector
                    = new FilteredElementCollector(doc);

                var s = string.Empty;

                foreach (var e in collector) s += $"{e.Name}{e.Category.Name}\n";
                MessageBox.Show(s);

                return Result.Succeeded;
            }
        }

        private void f3(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Level));
            var id = new ElementId(
                BuiltInParameter.DATUM_TEXT);

            var provider
                = new ParameterValueProvider(id);

            FilterStringRuleEvaluator evaluator
                = new FilterStringContains();

            //FilterRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, "Level", false );

            FilterRule rule = new FilterStringRule( // 2022
                provider, evaluator, "Level");

            var filter
                = new ElementParameterFilter(rule);
        }

        private void f4(Document doc)
        {
            // Use numeric evaluator and integer rule to test ElementId parameter
            // Filter levels whose id is greater than specified id value

            var testParam
                = BuiltInParameter.ID_PARAM;

            var pvp
                = new ParameterValueProvider(
                    new ElementId((int) testParam));

            FilterNumericRuleEvaluator fnrv
                = new FilterNumericGreater();

            // filter elements whose Id is greater than 99

            var ruleValId = new ElementId(99);

            FilterRule paramFr = new FilterElementIdRule(
                pvp, fnrv, ruleValId);

            var epf
                = new ElementParameterFilter(paramFr);

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(ViewPlan))
                .WherePasses(epf); // only deal with ViewPlan

            // Use numeric evaluator and integer rule to test bool parameter
            // Filter levels whose crop view is false

            var ruleValInt = 0;

            testParam = BuiltInParameter.VIEWER_CROP_REGION;

            pvp = new ParameterValueProvider(
                new ElementId((int) testParam));

            fnrv = new FilterNumericEquals();

            paramFr = new FilterIntegerRule(
                pvp, fnrv, ruleValInt);

            epf = new ElementParameterFilter(paramFr);

            collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(ViewPlan))
                .WherePasses(epf); // only deal with ViewPlan

            // Use numeric evaluator and double rule to test double parameter
            // Filter levels whose top offset is greater than specified value

            double ruleValDb = 10;

            testParam =
                BuiltInParameter.VIEWER_BOUND_OFFSET_TOP;

            pvp = new ParameterValueProvider(
                new ElementId((int) testParam));

            fnrv = new FilterNumericGreater();

            paramFr = new FilterDoubleRule(
                pvp, fnrv, ruleValDb, double.Epsilon);

            collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(ViewPlan))
                .WherePasses(epf); // only deal with ViewPlan

            // Use string evaluator and string rule to test string parameter
            // Filter all elements whose view name contains level

            var ruleValStr = "Level";

            testParam = BuiltInParameter.VIEW_NAME;

            pvp = new ParameterValueProvider(
                new ElementId((int) testParam));

            FilterStringRuleEvaluator fnrvStr
                = new FilterStringContains();

            //paramFr = new FilterStringRule( // 2021
            //  pvp, fnrvStr, ruleValStr, false );

            paramFr = new FilterStringRule( // 2022
                pvp, fnrvStr, ruleValStr);

            collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(ViewPlan))
                .WherePasses(epf); // only deal with ViewPlan
        }

        #endregion // More parameter filter samples

        #region Methods to measure collector and post processing speed

        /// <summary>
        ///     An empty method that does nothing.
        /// </summary>
        private Element EmptyMethod(Type type)
        {
            return null;
        }

        /// <summary>
        ///     An empty method that does nothing.
        /// </summary>
        private Element EmptyMethod(Type type, string name)
        {
            return null;
        }

        /// <summary>
        ///     Return all non ElementType elements.
        /// </summary>
        /// <returns></returns>
        private FilteredElementCollector GetNonElementTypeElements()
        {
            return new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType();
        }

        /// <summary>
        ///     Return a collector of all elements of the given type.
        /// </summary>
        private FilteredElementCollector GetElementsOfType(
            Type type)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(type);
        }

        /// <summary>
        ///     Return the first element of the given
        ///     type without any further filtering.
        /// </summary>
        private Element GetFirstElementOfType(
            Type type)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(type)
                .FirstElement();
        }

        /// <summary>
        ///     Use a parameter filter to return the first element
        ///     of the given type and with the specified string-valued
        ///     built-in parameter matching the given name.
        /// </summary>
        private Element GetFirstElementOfTypeWithBipString(
            Type type,
            BuiltInParameter bip,
            string name)
        {
            var a
                = GetElementsOfType(type);

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterStringRuleEvaluator evaluator
                = new FilterStringEquals();

            //FilterRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, name, true );

            FilterRule rule = new FilterStringRule( // 2022
                provider, evaluator, name);

            var filter
                = new ElementParameterFilter(rule);

            return a.WherePasses(filter).FirstElement();
        }

        #region Methods to measure post processing speed retrieving all elements

        /// <summary>
        ///     Return a list of all elements matching
        ///     the given type using explicit code to test
        ///     the element type.
        /// </summary>
        private List<Element> GetElementsOfTypeUsingExplicitCode(
            Type type)
        {
            var a
                = GetNonElementTypeElements();

            var b = new List<Element>();
            foreach (var e in a)
                if (e.GetType().Equals(type))
                    b.Add(e);
            return b;
        }

        /// <summary>
        ///     Return a list of all elements matching
        ///     the given type using a LINQ query to test
        ///     the element type.
        /// </summary>
        private IEnumerable<Element> GetElementsOfTypeUsingLinq(
            Type type)
        {
            var a
                = GetNonElementTypeElements();

            var b =
                from e in a
                where e.GetType().Equals(type)
                select e;

            return b;
        }

        #endregion // Methods to measure post processing speed retrieving all elements

        #region Methods to measure post processing speed retrieving a named element

        /// <summary>
        ///     Return the first element of the given
        ///     type and name using explicit code.
        /// </summary>
        private Element GetFirstNamedElementOfTypeUsingExplicitCode(
            Type type,
            string name)
        {
            var a
                = GetElementsOfType(type);

            // explicit iteration and manual checking of a property:

            Element ret = null;
            foreach (var e in a)
                if (e.Name.Equals(name))
                {
                    ret = e;
                    break;
                }

            return ret;
        }

        /// <summary>
        ///     Return the first element of the given
        ///     type and name using LINQ.
        /// </summary>
        private Element GetFirstNamedElementOfTypeUsingLinq(
            Type type,
            string name)
        {
            var a
                = GetElementsOfType(type);

            // using LINQ:

            var elementsByName =
                from e in a
                where e.Name.Equals(name)
                select e;

            return elementsByName.First();
        }

        /// <summary>
        ///     Return the first element of the given
        ///     type and name using an anonymous method
        ///     to define a named method.
        /// </summary>
        private Element GetFirstNamedElementOfTypeUsingAnonymousButNamedMethod(
            Type type,
            string name)
        {
            var a
                = GetElementsOfType(type);

            // using an anonymous method to define a named method:

            Func<Element, bool> nameEquals = e => e.Name.Equals(name);
            return a.First(nameEquals);
        }

        /// <summary>
        ///     Return the first element of the given
        ///     type and name using an anonymous method.
        /// </summary>
        private Element GetFirstNamedElementOfTypeUsingAnonymousMethod(
            Type type,
            string name)
        {
            var a
                = GetElementsOfType(type);

            // using an anonymous method:

            return a.First(
                e => e.Name.Equals(name));
        }

        #endregion // Methods to measure post processing speed retrieving a named element

        #endregion // Methods to measure collector and post processing speed

        #region Retrieve column family instances sorted as in schedule

        public class ColumnMarkComparer : IComparer<FamilyInstance>
        {
            int IComparer<FamilyInstance>.Compare(
                FamilyInstance x,
                FamilyInstance y)
            {
                if (x == null) return y == null ? 0 : -1;
                if (y == null)
                    return 1;

                var mark1 = x.GetColumnLocationMark()
                    .Split('(', ')');

                var mark2 = y.GetColumnLocationMark()
                    .Split('(', ')');

                if (mark1.Length < 4) return mark2.Length < 4 ? 0 : -1;
                if (mark2.Length < 4)
                    return 1;

                // gridsequence A
                var res = string.Compare(
                    mark1[0], mark2[0]);

                if (res != 0)
                    return res;

                // gridsequence 1
                var m12 = mark1[2].Remove(0, 1);
                var m22 = mark2[2].Remove(0, 1);
                res = string.Compare(m12, m22);
                if (res != 0)
                    return res;

                // value xxxx
                double d1 = 0;
                double d2 = 0;
                double.TryParse(mark1[1], out d1);
                double.TryParse(mark2[1], out d2);
                if (Math.Round(d1 - d2, 4) != 0)
                {
                    if ((d1 < 0) ^ (d2 < 0))
                        return d1 < 0 ? 1 : -1;
                    return Math.Abs(d1) < Math.Abs(d2)
                        ? -1
                        : 1;
                }

                // value yyyy
                double.TryParse(mark1[3], out d1);
                double.TryParse(mark2[3], out d2);
                if (Math.Round(d1 - d2, 4) != 0)
                {
                    if ((d1 < 0) ^ (d2 < 0))
                        return d1 < 0 ? 1 : -1;
                    return Math.Abs(d1) < Math.Abs(d2)
                        ? -1
                        : 1;
                }

                return 0;
            }
        }

        /// <summary>
        ///     Return a sorted list of all structural columns.
        ///     The sort order is defined by ColumnMarkComparer
        ///     as requested to to replicate the graphical column
        ///     schedule sort order with C# in
        ///     https://forums.autodesk.com/t5/revit-api-forum/replicate-graphical-column-schedule-sort-order-with-c/m-p/9105470
        /// </summary>
        private List<FamilyInstance> GetSortedColumns(Document doc)
        {
            var colums
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .ToList();

            colums.Sort(new ColumnMarkComparer());

            return colums;
        }

        #endregion // Retrieve column family instances sorted as in schedule
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