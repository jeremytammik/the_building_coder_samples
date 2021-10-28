#region Header

//
// CmdDeleteMacros.cs - retrieve MacroManager and delete all macros
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Macros;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Macros;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdDeleteMacros : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var uiapp_mgr = UIMacroManager
                .GetMacroManager(uiapp);

            var uidoc_mgr = UIMacroManager
                .GetMacroManager(uidoc);

            var nModulesApp = uiapp_mgr.MacroManager.Count;
            var nModulesDoc = uidoc_mgr.MacroManager.Count;

            var nMacrosDoc = uidoc_mgr.MacroManager
                .Aggregate(0,
                    (n, m) => n + m.Count());

            var dlg = new TaskDialog("Delete Document Macros");

            dlg.MainInstruction = "Are you really sure you "
                                  + "want to delete all document macros?";

            dlg.MainContent = string.Format(
                "{0} application module{1} "
                + "and {2} document macro module{3} "
                + "defining {4} macro{5}.",
                nModulesApp, Util.PluralSuffix(nModulesApp),
                nModulesDoc, Util.PluralSuffix(nModulesDoc),
                nMacrosDoc, Util.PluralSuffix(nMacrosDoc));

            dlg.MainIcon = TaskDialogIcon.TaskDialogIconWarning;

            dlg.CommonButtons = TaskDialogCommonButtons.Yes
                                | TaskDialogCommonButtons.Cancel;

            var rslt = dlg.Show();

            if (TaskDialogResult.Yes == rslt)
            {
                var mgr = MacroManager.GetMacroManager(doc);
                var it = mgr.GetMacroManagerIterator();

                // Several possibilities to iterate macros:
                //for( it.Reset(); !it.IsDone(); it.MoveNext() ) { }
                //IEnumerator<MacroModule> e = mgr.GetEnumerator();

                var n = 0;
                foreach (var mod in mgr)
                {
                    Debug.Print($"module {mod.Name}");
                    foreach (var mac in mod)
                    {
                        Debug.Print($"macro {mac.Name}");
                        mod.RemoveMacro(mac);
                        ++n;
                    }

                    // Exception thrown: 'Autodesk.Revit.Exceptions
                    // .InvalidOperationException' in RevitAPIMacros.dll
                    // Cannot remove the UI module
                    //mgr.RemoveModule( mod );
                }

                TaskDialog.Show("Document Macros Deleted",
                    $"{n} document macro{Util.PluralSuffix(n)} deleted.");
            }

            return Result.Succeeded;
        }
    }
}