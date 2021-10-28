#region Header

//
// CmdMirror.cs - mirror some elements.
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdMirror : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;

            var app = uiapp.Application;
            var doc = uidoc.Document;

            // 'Document.Mirror(ElementSet, Line)' is obsolete:
            // Use one of the replace methods in ElementTransformUtils.
            //
            //Line line = app.Create.NewLine(
            //  XYZ.Zero, XYZ.BasisX, true ); // 2011
            //
            //ElementSet els = uidoc.Selection.Elements; // 2011
            //
            //doc.Mirror( els, line ); // 2011

            //Plane plane = new Plane( XYZ.BasisY, XYZ.Zero ); // added in 2012, used until 2016
            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisY, XYZ.Zero); // 2017

            var elementIds
                = uidoc.Selection.GetElementIds(); // 2012

            //ElementTransformUtils.MirrorElements(
            //  doc, elementIds, plane ); // 2012-2015

            using var t = new Transaction(doc);
            t.Start("Mirror Elements");

            ElementTransformUtils.MirrorElements(
                doc, elementIds, plane, true); // 2016

            t.Commit();

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class CmdMirrorListAdded : IExternalCommand
    {
        private static List<ElementId> _addedElementIds;
        private readonly string _msg = "The following {0} element{1} were mirrored:\r\n";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            using var tx = new Transaction(doc);
            tx.Start("Mirror and List Added");
            //Line line = app.Create.NewLine(
            //  XYZ.Zero, XYZ.BasisX, true ); // 2011

            //ElementSet els = uidoc.Selection.Elements; // 2011

            //Plane plane = new Plane( XYZ.BasisY, XYZ.Zero ); // added in 2012, used until 2016

            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisY, XYZ.Zero); // 2017

            var elementIds
                = uidoc.Selection.GetElementIds(); // 2012

            using (var t = new SubTransaction(doc))
            {
                // determine newly added elements relying on the
                // element sequence as returned by the filtered collector.
                // this approach works in both Revit 2010 and 2011:

                t.Start();

                var n = GetElementCount(doc);

                //doc.Mirror( els, line ); // 2011

                //ElementTransformUtils.MirrorElements(
                //  doc, elementIds, plane ); // 2012-2015

                ElementTransformUtils.MirrorElements(
                    doc, elementIds, plane, true); // 2016

                var a = GetElementsAfter(n, doc);

                t.RollBack();
            }

            using (var t = new SubTransaction(doc))
            {
                // here is an idea for a new approach in 2011:
                // determine newly added elements relying on
                // monotonously increasing element id values:

                t.Start();

                var a = GetElements(doc);
                var i = a.Max(e => e.Id.IntegerValue);
                var maxId = new ElementId(i);

                // doc.Mirror( els, line ); // 2011

                //ElementTransformUtils.MirrorElements(
                //  doc, elementIds, plane ); // 2012-2015

                ElementTransformUtils.MirrorElements(
                    doc, elementIds, plane, true); // 2016

                // get all elements in document with an
                // element id greater than maxId:

                a = GetElementsAfter(doc, maxId);

                Report(a);

                t.RollBack();
            }

            using (var t = new SubTransaction(doc))
            {
                // similar to the above approach relying on
                // monotonously increasing element id values,
                // but apply a quick filter first:

                t.Start();

                var a = GetElements(doc);
                var i = a.Max(e => e.Id.IntegerValue);
                var maxId = new ElementId(i);

                //doc.Mirror( els, line ); // 2011

                //ElementTransformUtils.MirrorElements(
                //  doc, elementIds, plane ); // 2012-2015

                ElementTransformUtils.MirrorElements(
                    doc, elementIds, plane, true); // 2016

                // only look at non-ElementType elements
                // instead of all document elements:

                a = GetElements(doc);
                a = GetElementsAfter(a, maxId);

                Report(a);

                t.RollBack();
            }

            using (var t = new SubTransaction(doc))
            {
                // use a local and temporary DocumentChanged event
                // handler to directly obtain a list of all newly
                // created elements.
                // unfortunately, this canot be tested in this isolated form,
                // since the DocumentChanged event is only triggered when the
                // real outermost Revit transaction is committed, i.e. our
                // local sub-transaction makes no difference. since we abort
                // the sub-transaction before the command terminates and no
                // elements are really added to the database, our event
                // handler is never called:

                t.Start();

                app.DocumentChanged
                    += app_DocumentChanged;

                //doc.Mirror( els, line ); // 2011

                //ElementTransformUtils.MirrorElements(
                //  doc, elementIds, plane ); // 2012-2015

                ElementTransformUtils.MirrorElements(
                    doc, elementIds, plane, true); // 2016

                app.DocumentChanged
                    -= app_DocumentChanged;

                Debug.Assert(null == _addedElementIds,
                    "never expected the event handler to be called");

                if (null != _addedElementIds)
                {
                    var n = _addedElementIds.Count;

                    var s = string.Format(_msg, n,
                        Util.PluralSuffix(n));

                    foreach (var id in _addedElementIds)
                    {
                        var e = doc.GetElement(id);

                        s += $"\r\n  {Util.ElementDescription(e)}";
                    }

                    Util.InfoMsg(s);
                }

                t.RollBack();
            }

            tx.RollBack();

            return Result.Succeeded;
        }

        private void Report(FilteredElementCollector a)
        {
            var n = 0;
            var s = _msg;

            foreach (var e in a)
            {
                ++n;
                s += $"\r\n  {Util.ElementDescription(e)}";
            }

            s = string.Format(s, n, Util.PluralSuffix(n));

            Util.InfoMsg(s);
        }

        /// <summary>
        ///     Return all elements that are not ElementType objects.
        /// </summary>
        private FilteredElementCollector GetElements(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc);
            return collector.WhereElementIsNotElementType();
        }

        /// <summary>
        ///     Return the current number of non-ElementType elements.
        /// </summary>
        private int GetElementCount(Document doc)
        {
            return GetElements(doc).ToElements().Count;
        }

        /// <summary>
        ///     Return all database elements after the given number n.
        /// </summary>
        private List<Element> GetElementsAfter(int n, Document doc)
        {
            var a = new List<Element>();
            var c = GetElements(doc);
            var i = 0;

            foreach (var e in c)
            {
                ++i;

                if (n < i) a.Add(e);
            }

            return a;
        }

        /// <summary>
        ///     Return all document elements whose
        ///     element id is greater than 'lastId'.
        /// </summary>
        private FilteredElementCollector GetElementsAfter(
            Document doc,
            ElementId lastId)
        {
            var bip = BuiltInParameter.ID_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericGreater();

            FilterRule rule = new FilterElementIdRule(
                provider, evaluator, lastId);

            var filter
                = new ElementParameterFilter(rule);

            var collector
                = new FilteredElementCollector(doc);

            return collector.WherePasses(filter);
        }

        /// <summary>
        ///     Return all elements from the given collector
        ///     whose element id is greater than 'lastId'.
        /// </summary>
        private FilteredElementCollector GetElementsAfter(
            FilteredElementCollector input,
            ElementId lastId)
        {
            var bip = BuiltInParameter.ID_PARAM;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterNumericRuleEvaluator evaluator
                = new FilterNumericGreater();

            FilterRule rule = new FilterElementIdRule(
                provider, evaluator, lastId);

            var filter
                = new ElementParameterFilter(rule);

            return input.WherePasses(filter);
        }

        private void app_DocumentChanged(
            object sender,
            DocumentChangedEventArgs e)
        {
            if (null == _addedElementIds) _addedElementIds = new List<ElementId>();

            _addedElementIds.Clear();
            _addedElementIds.AddRange(
                e.GetAddedElementIds());
        }

        #region HighlightLastElement

        // from https://forums.autodesk.com/t5/revit-api-forum/getting-the-last-element-placed-in-a-model/m-p/10645949
        /// <summary>
        ///     Find and highlight last family instance element
        ///     by adding it to the current selection
        /// </summary>
        public void HighlightLastElement(
            UIDocument uidoc)
        {
            var doc = uidoc.Document;
            var selection = uidoc.Selection;

            var instances
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance));

            var opt = new Options();

            var id_max = instances
                .Where(e => null != e.Category)
                .Where(e => null != e.LevelId
                            && ElementId.InvalidElementId != e.LevelId)
                .Where(e => null != e.get_Geometry(opt))
                .Max<Element, int>(e => e.Id.IntegerValue);

            var last_eid = new ElementId(id_max);

            if (last_eid != null)
                selection.SetElementIds(
                    new List<ElementId>(
                        new[] {last_eid}));
        }

        #endregion // HighlightLastElement
    }
}