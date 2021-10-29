#region Header

//
// CmdPlaceFamilyInstance.cs - call PromptForFamilyInstancePlacement
// to place family instances and use the DocumentChanged event to
// capture the newly added element ids
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
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdPlaceFamilyInstance : IExternalCommand
    {
        /// <summary>
        ///     Set this flag to true to abort after
        ///     placing the first instance.
        /// </summary>
        private static readonly bool _place_one_single_instance_then_abort
            = true;

        private readonly List<ElementId> _added_element_ids
            = new();

        /// <summary>
        ///     Send messages to main Revit application window.
        /// </summary>
        //IWin32Window _revit_window; // 2018
        private IntPtr _revit_window; // 2019

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            //_revit_window
            //  = new JtWindowHandle(
            //    ComponentManager.ApplicationWindow ); // 2018

            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            _revit_window = uiapp.MainWindowHandle; // 2019

            var collector
                = new FilteredElementCollector(doc);

            collector.OfCategory(BuiltInCategory.OST_Doors);
            collector.OfClass(typeof(FamilySymbol));

            var symbol = collector.FirstElement()
                as FamilySymbol;

            _added_element_ids.Clear();

            app.DocumentChanged
                += OnDocumentChanged;

            //PromptForFamilyInstancePlacementOptions opt 
            //  = new PromptForFamilyInstancePlacementOptions();

            try
            {
                uidoc.PromptForFamilyInstancePlacement(symbol);
            }
            catch (OperationCanceledException ex)
            {
                Debug.Print(ex.Message);
            }

            app.DocumentChanged
                -= OnDocumentChanged;

            var n = _added_element_ids.Count;

            TaskDialog.Show(
                "Place Family Instance",
                $"{n} element{(1 == n ? "" : "s")} added.");

            return Result.Succeeded;
        }

        private void OnDocumentChanged(
            object sender,
            DocumentChangedEventArgs e)
        {
            var idsAdded
                = e.GetAddedElementIds();

            var n = idsAdded.Count;

            Debug.Print("{0} id{1} added.",
                n, Util.PluralSuffix(n));

            // This does not work, because the handler will
            // be called each time a new instance is added,
            // overwriting the previous ones recorded:

            //_added_element_ids = e.GetAddedElementIds();

            _added_element_ids.AddRange(idsAdded);

            if (_place_one_single_instance_then_abort
                && 0 < n)
            {
                // Why do we send the WM_KEYDOWN message twice?
                // I tried sending it once only, and that does
                // not work. Maybe the proper thing to do would 
                // be something like the Press.OneKey method...
                //
                //Press.OneKey( _revit_window.Handle,
                //  (char) Keys.Escape );
                //
                // Nope, that did not work.
                //
                // Answer: When you place instances with 
                // PromptForFamilyInstancePlacement, the previous 
                // one remains selected just until you drop the 
                // next one. The first esc key hit removes that 
                // selection while still allowing you to continue 
                // adding instances to the model. Only a second 
                // esc hit aborts the command. 

                //Press.PostMessage( _revit_window.Handle,
                //  (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
                //  (uint) Keys.Escape, 0 ); // 2018

                //Press.PostMessage( _revit_window.Handle,
                //  (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
                //  (uint) Keys.Escape, 0 ); // 2018

                Press.PostMessage(_revit_window,
                    (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
                    (uint) Keys.Escape, 0); // 2019

                Press.PostMessage(_revit_window,
                    (uint) Press.KEYBOARD_MSG.WM_KEYDOWN,
                    (uint) Keys.Escape, 0); // 2019
            }
        }
    }
}