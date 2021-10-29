#region Header

//
// CmdIdling.cs - subscribe to the Idling event
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdIdling : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Log("Execute begin");

            var uiapp = commandData.Application;

            uiapp.Idling
                += OnIdling;

            Log("Execute end");

            return Result.Succeeded;
        }

        private void Log(string msg)
        {
            var dt = DateTime.Now.ToString("u");
            Debug.Print($"{dt} {msg}");
        }

        private void OnIdling(object sender, IdlingEventArgs e)
        {
            // access active document from sender:

            //Application app = sender as Application; // 2011
            //Debug.Assert( null != app, // 2011
            //  "expected a valid Revit application instance" ); // 2011
            //UIApplication uiapp = new UIApplication( app ); // 2011

            var uiapp = sender as UIApplication; // 2012
            var doc = uiapp.ActiveUIDocument.Document;

            Log($"OnIdling with active document {doc.Title}");
        }
    }
}