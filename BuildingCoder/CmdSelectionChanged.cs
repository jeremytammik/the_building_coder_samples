#region Header

//
// CmdSelectionChanged.cs - Implement and subscribe to a custom element selection changed event using UI Automation
//
// Copyright (C) 2015-2020 by Vilo and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Windows;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdSelectionChanged : IExternalCommand
    {
        private static UIApplication _uiapp;
        private static bool _subscribed;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            _uiapp = commandData.Application;

            foreach (var tab in
                ComponentManager.Ribbon.Tabs)
                if (tab.Id == "Modify")
                {
                    if (_subscribed)
                    {
                        tab.PropertyChanged -= PanelEvent;
                        _subscribed = false;
                    }
                    else
                    {
                        tab.PropertyChanged += PanelEvent;
                        _subscribed = true;
                    }

                    break;
                }

            Debug.Print("CmdSelectionChanged: _subscribed = {0}", _subscribed);

            return Result.Succeeded;
        }

        private void PanelEvent(
            object sender,
            PropertyChangedEventArgs e)
        {
            Debug.Assert(sender is RibbonTab,
                "expected sender to be a ribbon tab");

            if (e.PropertyName == "Title")
            {
                var ids = _uiapp
                    .ActiveUIDocument.Selection.GetElementIds();

                var n = ids.Count;

                var s = 0 == n
                    ? "<nil>"
                    : string.Join(", ",
                        ids.Select(
                            id => id.IntegerValue.ToString()));

                Debug.Print(
                    $"CmdSelectionChanged: selection changed: {s}");
            }
        }
    }
}