#region Header

//
// CmdNewProjectDoc.cs - create a new project document
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//using Autodesk.Revit.Collections;
//
//

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdNewProjectDoc : IExternalCommand
    {
        private const string _template_file_path
            = "C:/Documents and Settings/All Users"
              + "/Application Data/Autodesk/RAC 2010"
              + "/Metric Templates/DefaultMetric.rte";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application.Application;

            var doc = app.NewProjectDocument(
                _template_file_path);

            doc.SaveAs("C:/tmp/new_project.rvt");

            return Result.Succeeded;
        }
    }
}