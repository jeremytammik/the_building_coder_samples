#region Header

//
// JtPairPicker.cs - helper class to pick a pair of elements
//
// Copyright (C) 2014-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Pick a pair of elements of a specific type.
    ///     If exactly two exist in the entire model,
    ///     take them. If there are less than two, give
    ///     up. If elements have been preselected, use
    ///     those. Otherwise, prompt for interactive
    ///     picking.
    /// </summary>
    internal class JtPairPicker<T> where T : Element
    {
        private readonly Document _doc;
        private readonly UIDocument _uidoc;
        private List<T> _a;

        public JtPairPicker(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _doc = _uidoc.Document;
        }

        /// <summary>
        ///     Return selection result.
        /// </summary>
        public IList<T> Selected => _a;

        /// <summary>
        ///     Run the automatic or interactive
        ///     selection process.
        /// </summary>
        public Result Pick()
        {
            // Retrieve all T elements in the entire model.

            _a = new List<T>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(T))
                    .ToElements()
                    .Cast<T>());

            var n = _a.Count;

            // If there are less than two, 
            // there is nothing we can do.

            if (2 > n) return Result.Failed;

            // If there are exactly two, pick those.

            if (2 == n) return Result.Succeeded;

            // There are more than two to choose from.
            // Check for a pre-selection.

            _a.Clear();

            var sel = _uidoc.Selection;

            var ids
                = sel.GetElementIds();

            n = ids.Count;

            Debug.Print("{0} pre-selected elements.", n);

            // If two or more T elements were pre-
            // selected, use the first two encountered.

            if (1 < n)
                foreach (var id in ids)
                {
                    var e = _doc.GetElement(id) as T;

                    Debug.Assert(null != e,
                        "only elements of type T can be picked");

                    _a.Add(e);

                    if (2 == _a.Count)
                    {
                        Debug.Print("Found two pre-selected "
                                    + "elements of desired type and "
                                    + "ignoring everything else.");

                        break;
                    }
                }

            // None or less than two elements were pre-
            // selected, so prompt for an interactive 
            // post-selection instead.

            if (2 != _a.Count)
            {
                _a.Clear();

                // Select first element.

                ISelectionFilter filter
                    = new JtElementsOfClassSelectionFilter<T>();

                try
                {
                    var r = sel.PickObject(
                        ObjectType.Element, filter,
                        "Please pick first element.");

                    _a.Add(_doc.GetElement(r.ElementId)
                        as T);
                }
                catch (OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                // Select second element.

                try
                {
                    var r = sel.PickObject(
                        ObjectType.Element, filter,
                        "Please pick second element.");

                    _a.Add(_doc.GetElement(r.ElementId)
                        as T);
                }
                catch (OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }

            return Result.Succeeded;
        }
    }
}