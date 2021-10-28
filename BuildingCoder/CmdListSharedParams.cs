#region Header

//
// CmdListSharedParams.cs - list all shared parameters
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//using Autodesk.Revit.Collections;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListSharedParams : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var bindings = doc.ParameterBindings;

            var n = bindings.Size;

            Debug.Print("{0} shared parementer{1} defined{2}",
                n, Util.PluralSuffix(n), Util.DotOrColon(n));

            if (0 < n)
            {
                var it
                    = bindings.ForwardIterator();

                while (it.MoveNext())
                {
                    var d = it.Key;
                    var b = it.Current as Binding;

                    Debug.Assert(b is ElementBinding,
                        "all Binding instances are ElementBinding instances");

                    Debug.Assert(b is InstanceBinding or TypeBinding,
                        "all bindings are either instance or type");

                    // All definitions obtained in this manner
                    // are InternalDefinition instances, even
                    // if they are actually associated with
                    // shared parameters, i.e. external.

                    Debug.Assert(d is InternalDefinition,
                        "all definitions obtained from BindingMap are internal");

                    var sbinding = b is InstanceBinding
                        ? "instance"
                        : "type";

                    Debug.Print("{0}: {1}", d.Name, sbinding);
                }
            }

            return Result.Succeeded;
        }

        #region Obsolete code that was never used

        /// <summary>
        ///     Get GUID for a given shared param name.
        /// </summary>
        /// <param name="app">Revit application</param>
        /// <param name="defGroup">Definition group name</param>
        /// <param name="defName">Definition name</param>
        /// <returns>GUID</returns>
        private static Guid SharedParamGuid(Application app, string defGroup, string defName)
        {
            var guid = Guid.Empty;
            try
            {
                var file = app.OpenSharedParameterFile();
                var group = file.Groups.get_Item(defGroup);
                var definition = group.Definitions.get_Item(defName);
                var externalDefinition = definition as ExternalDefinition;
                guid = externalDefinition.GUID;
            }
            catch (Exception)
            {
            }

            return guid;
        }

        public Result ExecuteObsolete(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var bindings = doc.ParameterBindings;
            //Dictionary<string, Guid> guids = new Dictionary<string, Guid>();
            var mapDefToGuid = new Dictionary<Definition, object>();

            var n = bindings.Size;
            Debug.Print("{0} shared parementer{1} defined{2}",
                n, Util.PluralSuffix(n), Util.DotOrColon(n));

            if (0 < n)
            {
                var it
                    = bindings.ForwardIterator();

                while (it.MoveNext())
                {
                    var d = it.Key;
                    var b = it.Current as Binding;
                    if (d is ExternalDefinition definition)
                    {
                        var g = definition.GUID;
                        Debug.Print($"{definition.Name}: {g}");
                        mapDefToGuid.Add(definition, g);
                    }
                    else
                    {
                        Debug.Assert(d is InternalDefinition);

                        // this built-in parameter is INVALID:

                        var bip = ((InternalDefinition) d).BuiltInParameter;
                        Debug.Print($"{d.Name}: {bip}");

                        // if have a definition file and group name, we can still determine the GUID:

                        //Guid g = SharedParamGuid( app, "Identity data", d.Name );

                        mapDefToGuid.Add(d, null);
                    }
                }
            }

            var walls = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                walls, uidoc, typeof(Wall)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some wall elements."
                    : "No wall elements found.";
            }
            else
            {
                //List<string> keys = new List<string>( mapDefToGuid.Keys );
                //keys.Sort();

                foreach (Wall wall in walls)
                {
                    Debug.Print(Util.ElementDescription(wall));

                    foreach (var d in mapDefToGuid.Keys)
                    {
                        var o = mapDefToGuid[d];

                        var p = null == o
                            ? wall.get_Parameter(d)
                            : wall.get_Parameter((Guid) o);

                        var s = null == p
                            ? "<null>"
                            : p.AsValueString();

                        Debug.Print($"{d.Name}: {s}");
                    }
                }
            }

            return Result.Failed;
        }

        #endregion // Obsolete code that was never used
    }
}