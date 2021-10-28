#region Header

//
// CmdDocumentVersion.cs - list DocumentVersion data, i.e. document GUID and save count
//
// Copyright (C) 2014-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
    internal class CmdDocumentVersion : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData revit,
            ref string message,
            ElementSet elements)
        {
            var uiapp = revit.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var path = doc.PathName;

            var info = BasicFileInfo.Extract(
                path);

            var v = info.GetDocumentVersion();

            var n = v.NumberOfSaves;

            Util.InfoMsg($"Document '{path}' has GUID {v.VersionGUID} and {n} save{Util.PluralSuffix(n)}.");

            return Result.Succeeded;
        }
    }
}