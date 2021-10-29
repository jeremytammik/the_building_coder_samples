#region Header

//
// CmdNamedGuidStorage.cs - Test the named Guid storage class
//
// Copyright (C) 2015-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNamedGuidStorage : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            var rslt = Result.Failed;

            var name = "TrackChanges_project_identifier";
            Guid named_guid;

            var rc = JtNamedGuidStorage.Get(doc,
                name, out named_guid, false);

            if (rc)
            {
                Util.InfoMsg($"This document already has a project identifier: {name} = {named_guid.ToString()}");

                rslt = Result.Succeeded;
            }
            else
            {
                rc = JtNamedGuidStorage.Get(doc,
                    name, out named_guid);

                if (rc)
                {
                    Util.InfoMsg($"Created a new project identifier for this document: {name} = {named_guid.ToString()}");

                    rslt = Result.Succeeded;
                }
                else
                {
                    Util.ErrorMsg("Something went wrong");
                }
            }

            return rslt;
        }
    }
}