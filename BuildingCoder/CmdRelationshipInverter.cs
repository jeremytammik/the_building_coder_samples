#region Header

//
// CmdRelationshipInverter.cs
//
// Determine door and window to wall relationships,
// i.e. hosted --> host, and invert it to obtain
// a map host --> list of hosted elements.
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdRelationshipInverter : IExternalCommand
    {
        private Document m_doc;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            m_doc = app.ActiveUIDocument.Document;

            // filter for family instance and (door or window):

            var fFamInstClass = new ElementClassFilter(typeof(FamilyInstance));
            var fDoorCat = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            var fWindowCat = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            var fCat = new LogicalOrFilter(fDoorCat, fWindowCat);
            var f = new LogicalAndFilter(fCat, fFamInstClass);
            var openings = new FilteredElementCollector(m_doc);
            openings.WherePasses(f);

            // map with key = host element id and
            // value = list of hosted element ids:

            var ids =
                GetElementIds(openings);

            DumpHostedElements(ids);
            m_doc = null;

            return Result.Succeeded;
        }

        private string ElementDescription(ElementId id)
        {
            var e = m_doc.GetElement(id);
            return Util.ElementDescription(e);
        }

        /// <summary>
        ///     From a list of openings, determine
        ///     the wall hoisting each and return a mapping
        ///     of element ids from host to all hosted.
        /// </summary>
        /// <param name="elements">Hosted elements</param>
        /// <returns>
        ///     Map of element ids from host to
        ///     hosted
        /// </returns>
        private Dictionary<ElementId, List<ElementId>>
            GetElementIds(FilteredElementCollector elements)
        {
            var dict =
                new Dictionary<ElementId, List<ElementId>>();

            var fmt = "{0} is hosted by {1}";

            foreach (FamilyInstance fi in elements)
            {
                var id = fi.Id;
                var idHost = fi.Host.Id;

                Debug.Print(fmt,
                    Util.ElementDescription(fi),
                    ElementDescription(idHost));

                if (!dict.ContainsKey(idHost)) dict.Add(idHost, new List<ElementId>());
                dict[idHost].Add(id);
            }

            return dict;
        }

        private void DumpHostedElements(
            Dictionary<ElementId, List<ElementId>> ids)
        {
            foreach (var idHost in ids.Keys)
            {
                var s = string.Empty;

                foreach (var id in ids[idHost])
                {
                    if (0 < s.Length) s += ", ";
                    s += ElementDescription(id);
                }

                var n = ids[idHost].Count;

                Debug.Print(
                    "{0} hosts {1} opening{2}: {3}",
                    ElementDescription(idHost),
                    n, Util.PluralSuffix(n), s);
            }
        }
    }
}