#region Header

//
// CmdSetTagType.cs - create a wall, door, door tag, then create and set a new door tag type
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Create a wall, door, door tag, then
    ///     create and set a new door tag type.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdSetTagType : IExternalCommand
    {
        private const double MeterToFeet = 3.2808399;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var createApp
                = app.Application.Create;

            var createDoc
                = doc.Create;

            // Determine the wall endpoints:

            var length = 5 * MeterToFeet;

            var pts = new XYZ[2];

            pts[0] = XYZ.Zero;
            pts[1] = new XYZ(length, 0, 0);

            // Determine the levels where
            // the wall will be located:

            Level levelBottom = null;
            Level levelTop = null;

            if (!GetBottomAndTopLevels(doc,
                ref levelBottom, ref levelTop))
            {
                message = "Unable to determine "
                          + "wall bottom and top levels";

                return Result.Failed;
            }

            using var t = new Transaction(doc);
            t.Start("Create Wall, Door and Tag");

            // Create a wall:

            var topLevelParam
                = BuiltInParameter.WALL_HEIGHT_TYPE;

            var topLevelId = levelTop.Id;

            //Line line = createApp.NewLineBound( pts[0], pts[1] ); // 2013
            var line = Line.CreateBound(pts[0], pts[1]); // 2014

            //Wall wall = createDoc.NewWall( // 2012
            //  line, levelBottom, false );

            var wall = Wall.Create( // 2013
                doc, line, levelBottom.Id, false);

            var param = wall.get_Parameter(
                topLevelParam);

            param.Set(topLevelId);

            // Determine wall thickness for tag
            // offset and profile growth:

            //double wallThickness = wall.WallType.CompoundStructure.Layers.get_Item( 0 ).Thickness; // 2011

            var wallThickness = wall.WallType.GetCompoundStructure().GetLayers()[0].Width; // 2012

            // Add door to wall;
            // note that the NewFamilyInstance method
            // does not automatically add a door tag,
            // like the UI command does:

            var doorSymbol = GetFirstFamilySymbol(
                doc, BuiltInCategory.OST_Doors);

            if (null == doorSymbol)
            {
                message = "No door symbol found.";
                return Result.Failed;
            }

            var midpoint = Util.Midpoint(pts[0], pts[1]);

            var door = createDoc
                .NewFamilyInstance(
                    midpoint, doorSymbol, wall, levelBottom,
                    StructuralType.NonStructural);

            // Create door tag:

            var view = doc.ActiveView;

            var tagOffset = 3 * wallThickness;

            midpoint += tagOffset * XYZ.BasisY;

            // 2011: TagOrientation.TAG_HORIZONTAL

            //IndependentTag tag = createDoc.NewTag(
            //  view, door, false, TagMode.TM_ADDBY_CATEGORY,
            //  TagOrientation.Horizontal, midpoint ); // 2012

            var tag = IndependentTag.Create(
                doc, view.Id, new Reference(door),
                false, TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal, midpoint); // 2018

            // Create and assign new door tag type:

            var doorTagType
                = GetFirstFamilySymbol(
                    doc, BuiltInCategory.OST_DoorTags);

            doorTagType = doorTagType.Duplicate(
                "New door tag type") as FamilySymbol;

            tag.ChangeTypeId(doorTagType.Id);

            // Demonstrate changing name of
            // family instance and family symbol:

            door.Name = $"{door.Name} modified";
            door.Symbol.Name = $"{door.Symbol.Name} modified";

            t.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return all elements of the requested class,
        ///     i.e. System.Type, matching the given built-in
        ///     category in the given document.
        /// </summary>
        private static FilteredElementCollector
            GetElementsOfType(
                Document doc,
                Type type,
                BuiltInCategory bic)
        {
            var collector
                = new FilteredElementCollector(doc);

            collector.OfCategory(bic);
            collector.OfClass(type);

            return collector;
        }

        /// <summary>
        ///     Return all family symbols in the given document
        ///     matching the given built-in category.
        ///     Todo: Compare this with the FamilySymbolFilter class.
        /// </summary>
        private static FilteredElementCollector
            GetFamilySymbols(
                Document doc,
                BuiltInCategory bic)
        {
            return GetElementsOfType(doc,
                typeof(FamilySymbol), bic);
        }

        /// <summary>
        ///     Return the first family symbol found in the given document
        ///     matching the given built-in category, or null if none is found.
        /// </summary>
        private static FamilySymbol GetFirstFamilySymbol(
            Document doc,
            BuiltInCategory bic)
        {
            var s = GetFamilySymbols(doc, bic)
                .FirstElement() as FamilySymbol;

            Debug.Assert(null != s, $"expected at least one {bic.ToString()} symbol in project");

            return s;
        }

        /// <summary>
        ///     Determine bottom and top levels for creating walls.
        ///     In a default empty Revit Architecture project,
        ///     'Level 1' and 'Level 2' will be returned.
        /// </summary>
        /// <returns>True if the two levels are successfully determined.</returns>
        private static bool GetBottomAndTopLevels(
            Document doc,
            ref Level levelBottom,
            ref Level levelTop)
        {
            var levels
                = GetElementsOfType(doc, typeof(Level),
                    BuiltInCategory.OST_Levels);

            foreach (var e in levels)
                if (null == levelBottom)
                    levelBottom = e as Level;
                else if (null == levelTop)
                    levelTop = e as Level;
                else
                    break;

            if (levelTop.Elevation < levelBottom.Elevation)
            {
                var tmp = levelTop;
                levelTop = levelBottom;
                levelBottom = tmp;
            }

            return null != levelBottom && null != levelTop;
        }

        #region Set Tag Colour to Element Colour

        // for https://forums.autodesk.com/t5/revit-api-forum/macro-doesnt-work-properly-on-big-projects/m-p/10186076
        public void SetTagColorToElementColor(UIDocument uiDoc)
        {
            //current document
            //UIDocument uiDoc = new UIDocument( Document );
            //Document doc = this.Application.ActiveUIDocument.Document;

            var doc = uiDoc.Document;

            //current view and open views
            var curView = doc.ActiveView;
            var openViews = uiDoc.GetOpenUIViews();
            var openV1 = new List<ElementId>();

            foreach (var view in openViews)
            {
                var i = view.ViewId;
                openV1.Add(i);
            }

            //get builtincategory of selected tag
            var sel = uiDoc.Selection.PickObject(ObjectType.Element);
            var ele = doc.GetElement(sel.ElementId);
            var cat = ele.Category;
            var builtInCat = (BuiltInCategory) cat.Id.IntegerValue;
            //BuiltInCategory builtInCat = (BuiltInCategory) Enum.Parse(
            //  typeof( BuiltInCategory ), cat.Category.Id.ToString() );

            // Get all non-template plan views in document
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate)
                .Where(v
                    => 0 < new FilteredElementCollector(doc, v.Id)
                        .OfCategory(builtInCat)
                        .GetElementCount());

            // Get all the views with same tag in it
            //List<View> viewInclude = new List<View>();

            //foreach( var view in views )
            //{
            //  uiDoc.ActiveView = view as View;
            //  var e = new FilteredElementCollector( doc, doc.ActiveView.Id )
            //    .OfCategory( builtInCat );

            //  if( e.GetElementCount() != 0 )
            //  {
            //    viewInclude.Add( view as View );
            //  }
            //}

            // Loop through each view and set color for tags
            foreach (var view in views)
            {
                //uiDoc.ActiveView = view as View;

                //get list of tags in active view
                var tags = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(builtInCat)
                    .WhereElementIsNotElementType();

                //get list of tagged elements
                var taggedElements = new List<Element>();

                foreach (var e in tags)
                {
                    var tag = doc.GetElement(e.Id) as IndependentTag;
                    //Element taggedElem = tag.GetTaggedLocalElement(); // 2021
                    //taggedElements.Add( taggedElem );
                    var taggedElems = tag.GetTaggedLocalElements(); // 2022
                    taggedElements.AddRange(taggedElems);
                }

                //get the color of the ductsystem
                var tagColor = new List<Color>();

                var builtInParam
                    = (BuiltInParameter) Enum.Parse(
                        typeof(BuiltInParameter),
                        taggedElements[0]
                            .GetParameters("System Type")[0]
                            .Id.ToString());

                foreach (var e in taggedElements)
                {
                    var elem = doc.GetElement(e.Id);
                    var param = elem.get_Parameter(builtInParam);
                    if (param == null)
                        return;
                    var systemType = doc.GetElement(param.AsElementId())
                        as MEPSystemType;

                    var c = systemType.LineColor;

                    var systemColorRed = c.Red;
                    var systemColorGreen = c.Green;
                    var systemColorblue = c.Blue;

                    var color = new Color(systemColorRed,
                        systemColorGreen, systemColorblue);
                    tagColor.Add(color);
                }

                var overRide = new OverrideGraphicSettings();

                var trans1 = new Transaction(doc);

                //change the color of the tags in revit
                trans1.Start("change color tag");

                var index = 0;

                foreach (var e in tags)
                {
                    doc.ActiveView.SetElementOverrides(e.Id,
                        overRide.SetProjectionLineColor(
                            tagColor[index]));
                    index += 1;
                }

                trans1.Commit();

                index = 0;
            }
        }

        #endregion // Set Tag Colour to Element Colour
    }
}