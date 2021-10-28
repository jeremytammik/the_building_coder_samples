#region Header

//
// CmdSharedParamGuids.cs - list all shared parameter GUIDs
//
// Copyright (C) 2017-2020 by Alexander Ignatovich and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// Written by Alexander Ignatovich.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdSharedParamGuids : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var bindingMap = doc.ParameterBindings;
            var it = bindingMap.ForwardIterator();
            it.Reset();

            while (it.MoveNext())
            {
                var definition = (InternalDefinition) it.Key;

                if (doc.GetElement(
                    definition.Id) is not SharedParameterElement sharedParameterElement)
                    TaskDialog.Show("non-shared parameter",
                        definition.Name);
                else
                    TaskDialog.Show("shared parameter",
                        $"{sharedParameterElement.GuidValue}- {{definition.Name}}");
            }

            return Result.Succeeded;
        }
    }
}