#region Header

//
// CmdPurgeTextNoteTypes.cs - purge TextNote types, i.e. delete all unused TextNote type instances
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdPurgeTextNoteTypes : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var unusedTextNoteTypes
                = GetUnusedTextNoteTypes(doc);

            var n = unusedTextNoteTypes.Count;

            var nLoop = 100;

            var sw = new Stopwatch();

            sw.Reset();
            sw.Start();

            for (var i = 0; i < nLoop; ++i)
            {
                unusedTextNoteTypes
                    = GetUnusedTextNoteTypes(doc);

                Debug.Assert(unusedTextNoteTypes.Count == n,
                    "expected same number of unused text note types");
            }

            sw.Stop();
            var ms = sw.ElapsedMilliseconds
                     / (double) nLoop;

            sw.Reset();
            sw.Start();

            for (var i = 0; i < nLoop; ++i)
            {
                unusedTextNoteTypes
                    = GetUnusedTextNoteTypesExcluding(doc);

                Debug.Assert(unusedTextNoteTypes.Count == n,
                    "expected same number of unused texct note types");
            }

            sw.Stop();
            var msExcluding
                = sw.ElapsedMilliseconds
                  / (double) nLoop;

            var t = new Transaction(doc,
                "Purging unused text note types");

            t.Start();

            sw.Reset();
            sw.Start();

            doc.Delete(unusedTextNoteTypes);

            sw.Stop();
            var msDeleting
                = sw.ElapsedMilliseconds
                  / (double) nLoop;

            t.Commit();

            Util.InfoMsg(string.Format(
                "{0} text note type{1} purged. "
                + "{2} ms to collect, {3} ms to collect "
                + "excluding, {4} ms to delete.",
                n, Util.PluralSuffix(n),
                ms, msExcluding, msDeleting));

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return all unused text note types by collecting all
        ///     existing types in the document and removing the
        ///     ones that are used afterwards.
        /// </summary>
        private ICollection<ElementId> GetUnusedTextNoteTypes(
            Document doc)
        {
            var collector
                = new FilteredElementCollector(doc);

            ICollection<ElementId> textNoteTypes
                = collector.OfClass(typeof(TextNoteType))
                    .ToElementIds()
                    .ToList();

            var textNotes
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNote));

            foreach (TextNote textNote in textNotes)
            {
                var removed = textNoteTypes.Remove(
                    textNote.TextNoteType.Id);
            }

            return textNoteTypes;
        }

        /// <summary>
        ///     Return all unused text note types by first
        ///     determining all text note types in use and
        ///     then collecting all the others using an
        ///     exclusion filter.
        /// </summary>
        private ICollection<ElementId>
            GetUnusedTextNoteTypesExcluding(
                Document doc)
        {
            ICollection<ElementId> usedTextNotesTypeIds
                = new Collection<ElementId>();

            var textNotes
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNote));

            foreach (TextNote textNote in textNotes)
                usedTextNotesTypeIds.Add(
                    textNote.TextNoteType.Id);

            var unusedTypeCollector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType));

            if (0 < usedTextNotesTypeIds.Count)
                unusedTypeCollector.Excluding(
                    usedTextNotesTypeIds);

            var unusedTypes
                = unusedTypeCollector.ToElementIds();

            return unusedTypes;
        }
    }
}