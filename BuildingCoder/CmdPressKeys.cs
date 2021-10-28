#region Header

//
// CmdPressKeys.cs - press keys to launch 'Create Similar' and other Revit commands
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    #region Press class: encapsulates PostMessage and provides Keys method

    public class Press
    {
        public enum KEYBOARD_MSG : uint
        {
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 0x101
        }

        [DllImport("USER32.DLL")]
        public static extern bool PostMessage(
            IntPtr hWnd, uint msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(
            uint uCode, uint uMapType);

        /// <summary>
        ///     Post one single keystroke.
        /// </summary>
        public static void OneKey(IntPtr handle, char letter)
        {
            var scanCode = MapVirtualKey(letter,
                (uint) MVK_MAP_TYPE.VKEY_TO_SCANCODE);

            var keyDownCode = (uint)
                              WH_KEYBOARD_LPARAM.KEYDOWN
                              | (scanCode << 16);

            var keyUpCode = (uint)
                            WH_KEYBOARD_LPARAM.KEYUP
                            | (scanCode << 16);

            PostMessage(handle,
                (uint) KEYBOARD_MSG.WM_KEYDOWN,
                letter, keyDownCode);

            PostMessage(handle,
                (uint) KEYBOARD_MSG.WM_KEYUP,
                letter, keyUpCode);
        }

        /// <summary>
        ///     Post a sequence of keystrokes.
        /// </summary>
        public static void Keys(
            IntPtr revitHandle,
            string command)
        {
            //IntPtr revitHandle = System.Diagnostics.Process // 2018
            //  .GetCurrentProcess().MainWindowHandle; // 2018

            foreach (var letter in command) OneKey(revitHandle, letter);
        }

        private enum WH_KEYBOARD_LPARAM : uint
        {
            KEYDOWN = 0x00000001,
            KEYUP = 0xC0000001
        }

        private enum MVK_MAP_TYPE : uint
        {
            VKEY_TO_SCANCODE = 0,
            SCANCODE_TO_VKEY = 1,
            VKEY_TO_CHAR = 2,
            SCANCODE_TO_LR_VKEY = 3
        }
    }

    #endregion //Press class: encapsulates PostMessage and provides Keys method

    [Transaction(TransactionMode.Manual)]
    internal class CmdPressKeys : IExternalCommand
    {
        /// <summary>
        ///     Here is a part or our code to start a Revit command.
        ///     The aim of the code is to set a wall type current in the Revit property window.
        ///     We only start up the wall command with the API and let the user do the drawing of the wall.
        ///     This solution can also be used to launch other Revit commands.
        /// </summary>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            var hRvt = uiapp.MainWindowHandle; // 2019

            // Name of target wall type that we want to use:

            var wallTypeName = "Generic - 203";

            var wallType = GetFirstWallTypeNamed(
                doc, wallTypeName);

            var wall = GetFirstWallUsingType(
                doc, wallType);

            // Select the wall in the UI

            //uidoc.Selection.Elements.Add( wall ); // 2014

            var ids = new List<ElementId>(1);
            ids.Add(wall.Id);
            uidoc.Selection.SetElementIds(ids); // 2015

            //if( 0 == uidoc.Selection.Elements.Size ) // 2014

            if (0 == uidoc.Selection.GetElementIds().Count) // 2015
            {
                // No wall with the correct wall type found

                var collector
                    = new FilteredElementCollector(doc);

                var ll = collector
                    .OfClass(typeof(Level))
                    .FirstElement() as Level;

                // place a new wall with the
                // correct wall type in the project

                //Line geomLine = app.Create.NewLineBound( XYZ.Zero, new XYZ( 2, 0, 0 ) ); // 2013
                var geomLine = Line.CreateBound(XYZ.Zero, new XYZ(2, 0, 0)); // 2014

                var t = new Transaction(
                    doc, "Create dummy wall");

                t.Start();

                //Wall nw = doc.Create.NewWall( geomLine, // 2012
                //  wallType, ll, 1, 0, false, false );

                var nw = Wall.Create(doc, geomLine, // 2013
                    wallType.Id, ll.Id, 1, 0, false, false);

                t.Commit();

                // Select the new wall in the project

                //uidoc.Selection.Elements.Add( nw ); // 2014

                ids.Clear();
                ids.Add(nw.Id);
                uidoc.Selection.SetElementIds(ids); // 2015

                // Start command create similar. In the
                // property menu, our wall type is set current

                Press.Keys(hRvt, "CS");

                // Select the new wall in the project,
                // so we can delete it

                //uidoc.Selection.Elements.Add( nw ); // 2014

                ids.Clear();
                ids.Add(nw.Id);
                uidoc.Selection.SetElementIds(ids); // 2015

                // Erase the selected wall (remark:
                // doc.delete(nw) may not be used,
                // this command will undo)

                Press.Keys(hRvt, "DE");

                // Start up wall command

                Press.Keys(hRvt, "WA");
            }
            else
            {
                // The correct wall is already selected:

                Press.Keys(hRvt, "CS"); // Start "create similar"
            }

            return Result.Succeeded;
        }

        #region GetFirstWallTypeNamed

        /// <summary>
        ///     Return the first wall type with the given name.
        /// </summary>
        private static WallType GetFirstWallTypeNamed(
            Document doc,
            string name)
        {
            // built-in parameter storing this
            // wall type's name:

            var bip
                = BuiltInParameter.SYMBOL_NAME_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterStringRuleEvaluator evaluator
                = new FilterStringEquals();

            //FilterRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, name, false );

            FilterRule rule = new FilterStringRule( // 2022
                provider, evaluator, name);

            var filter
                = new ElementParameterFilter(rule);

            var collector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .WherePasses(filter);

            return collector.FirstElement() as WallType;
        }

        #endregion // GetFirstWallTypeNamed

        #region GetFirstWallUsingType

        /// <summary>
        ///     Return the first wall found that
        ///     uses the given wall type.
        /// </summary>
        private static Wall GetFirstWallUsingType(
            Document doc,
            WallType wallType)
        {
            // built-in parameter storing this
            // wall's wall type element id:

            var bip
                = BuiltInParameter.ELEM_TYPE_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericEquals();

            FilterRule rule = new FilterElementIdRule(
                provider, evaluator, wallType.Id);

            var filter
                = new ElementParameterFilter(rule);

            var collector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WherePasses(filter);

            return collector.FirstElement() as Wall;
        }

        #endregion // GetFirstWallUsingType
    }
}