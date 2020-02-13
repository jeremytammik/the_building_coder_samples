#region Header
//
// CmdDeleteMacros.cs - retrieve MacroManager and delete all macros
//
// Copyright (C) 2010-2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Macros;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Macros;
using System.Diagnostics;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdDeleteMacros : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      UIMacroManager uiapp_mgr = UIMacroManager
        .GetMacroManager( uiapp );

      UIMacroManager uidoc_mgr = UIMacroManager
        .GetMacroManager( uidoc );

      int nModulesApp = uiapp_mgr.MacroManager.Count;
      int nModulesDoc = uidoc_mgr.MacroManager.Count;

      int nMacrosDoc = uidoc_mgr.MacroManager
        .Aggregate<MacroModule, int>( 0, 
          ( n, m ) => n + m.Count<Macro>() );

      TaskDialog dlg = new TaskDialog( "Delete Document Macros" );

      dlg.MainInstruction = "Are you really sure you "
        + "want to delete all document macros?";

      dlg.MainContent = string.Format(
        "{0} application module{1} "
        + "and {2} document macro module{3} "
        + "defining {4} macro{5}.",
        nModulesApp, Util.PluralSuffix( nModulesApp ),
        nModulesDoc, Util.PluralSuffix( nModulesDoc ),
        nMacrosDoc, Util.PluralSuffix( nMacrosDoc ) );

      dlg.MainIcon = TaskDialogIcon.TaskDialogIconWarning;

      dlg.CommonButtons = TaskDialogCommonButtons.Yes
        | TaskDialogCommonButtons.Cancel;

      TaskDialogResult rslt = dlg.Show();

      if(TaskDialogResult.Yes == rslt )
      {
        MacroManager mgr = MacroManager.GetMacroManager( doc );
        MacroManagerIterator it = mgr.GetMacroManagerIterator();

        // Several possibilities to iterate macros:
        //for( it.Reset(); !it.IsDone(); it.MoveNext() ) { }
        //IEnumerator<MacroModule> e = mgr.GetEnumerator();

        int n = 0;
        foreach( MacroModule mod in mgr )
        {
          Debug.Print( "module " + mod.Name );
          foreach( Macro mac in mod )
          {
            Debug.Print( "macro " + mac.Name );
            mod.RemoveMacro( mac );
            ++n;
          }

          // Exception thrown: 'Autodesk.Revit.Exceptions
          // .InvalidOperationException' in RevitAPIMacros.dll
          // Cannot remove the UI module
          //mgr.RemoveModule( mod );
        }
        TaskDialog.Show( "Document Macros Deleted",
          string.Format(
            "{0} document macro{1} deleted.",
            n, Util.PluralSuffix( n ) ) );
      }
      return Result.Succeeded;
    }
  }
}
