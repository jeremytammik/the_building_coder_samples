#region Header

//
// CmdDeleteUnusedRefPlanes.cs - delete unnamed non-hosting reference planes
//
// Copyright (C) 2014-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    #region Obsolete solution for Revit 2014

    // Original problem description and solution:
    // http://thebuildingcoder.typepad.com/blog/2012/03/melbourne-devlab.html#2
    // Fix for Revit 2014:
    // http://thebuildingcoder.typepad.com/blog/2014/02/deleting-unnamed-non-hosting-reference-planes.html

    /// <summary>
    ///     Delete all reference planes that have not been
    ///     named and are not hosting any elements.
    ///     In other words, check whether the reference
    ///     plane has been named.
    ///     If not, check whether it hosts any elements.
    ///     If not, delete it.
    ///     Actually, to check whether it hosts any
    ///     elements, we delete it temporarily anyway, as
    ///     described in
    ///     Object Relationships http://thebuildingcoder.typepad.com/blog/2010/03/object-relationships.html
    ///     Object Relationships in VB http://thebuildingcoder.typepad.com/blog/2010/03/object-relationships-in-vb.html
    ///     Temporary Transaction Trick Touchup http://thebuildingcoder.typepad.com/blog/2012/11/temporary-transaction-trick-touchup.html
    ///     The deletion returns the number of elements
    ///     deleted. If this number is greater than one (the
    ///     ref plane itself), it hosted something. In that
    ///     case, roll back the transaction and do not delete.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdDeleteUnusedRefPlanes_2014 : IExternalCommand
    {
        private static int _i;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // Construct a parameter filter to get only 
            // unnamed reference planes, i.e. reference 
            // planes whose name equals the empty string:

            var bip
                = BuiltInParameter.DATUM_TEXT;

            var provider
                = new ParameterValueProvider(
                    new ElementId(bip));

            FilterStringRuleEvaluator evaluator
                = new FilterStringEquals();

            //FilterStringRule rule = new FilterStringRule( // 2021
            //  provider, evaluator, "", false );

            var rule = new FilterStringRule( // 2022
                provider, evaluator, "");

            var filter
                = new ElementParameterFilter(rule);

            var col
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ReferencePlane))
                    .WherePasses(filter);

            var n = 0;
            var nDeleted = 0;

            // No need to cast ... this is pretty nifty,
            // I find ... grab the elements as ReferencePlane
            // instances, since the filter guarantees that 
            // only ReferencePlane instances are selected.
            // In Revit 2014, this attempt to delete the 
            // reference planes while iterating over the 
            // filtered element collector throws an exception:
            // Autodesk.Revit.Exceptions.InvalidOperationException:
            // HResult=-2146233088
            // Message=The iterator cannot proceed due to 
            // changes made to the Element table in Revit's 
            // database (typically, This can be the result 
            // of an Element deletion).
            //
            //foreach( ReferencePlane rp in col )
            //{
            //  ++n;
            //  nDeleted += DeleteIfNotHosting( rp ) ? 1 : 0;
            //}

            var ids = col.ToElementIds();

            n = ids.Count();

            if (0 < n)
            {
                using var tx = new Transaction(doc);
                tx.Start($"Delete {n} ReferencePlane{Util.PluralSuffix(n)}");

                // This also causes the exception "One or more of 
                // the elementIds cannot be deleted. Parameter 
                // name: elementIds
                //
                //ICollection<ElementId> ids2 = doc.Delete(
                //  ids );
                //nDeleted = ids2.Count();

                var ids2 = new List<ElementId>(
                    ids);

                foreach (var id in ids2)
                    try
                    {
                        var ids3 = doc.Delete(
                            id);

                        nDeleted += ids3.Count;
                    }
                    catch (ArgumentException)
                    {
                    }

                tx.Commit();
            }

            Util.InfoMsg(string.Format(
                "{0} unnamed reference plane{1} examined, "
                + "{2} element{3} in total were deleted.",
                n, Util.PluralSuffix(n),
                nDeleted, Util.PluralSuffix(nDeleted)));

            return Result.Succeeded;
        }

        /// <summary>
        ///     Delete the given reference plane
        ///     if it is not hosting anything.
        /// </summary>
        /// <returns>
        ///     True if the given reference plane
        ///     was in fact deleted, else false.
        /// </returns>
        private bool DeleteIfNotHosting(ReferencePlane rp)
        {
            var rc = false;

            var doc = rp.Document;

            using var tx = new Transaction(doc);
            tx.Start($"Delete ReferencePlane {++_i}");

            // Deletion simply fails if the reference 
            // plane hosts anything. If so, the return 
            // value ids collection is null.
            // In Revit 2014, in that case, the call 
            // throws an exception "ArgumentException: 
            // ElementId cannot be deleted."

            try
            {
                var ids = doc.Delete(
                    rp.Id);

                tx.Commit();
                rc = true;
            }
            catch (System.ArgumentException)
            {
                tx.RollBack();
            }

            return rc;
        }
    }

    #endregion // Obsolete solution for Revit 2014

    #region Broken command in Revit 2019 shared by Austin Sudtelgte

    [TransactionAttribute(TransactionMode.Manual)]
    public class BrokenCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            // There is likely an easier way to do this using 
            // an exclusion filter but this being my first foray 
            // into filtering with Revit, I couldn't get that working.

            var filt = new FilteredElementCollector(doc);
            var refIDs = filt.OfClass(typeof(ReferencePlane)).ToElementIds().ToList();

            using var tg = new TransactionGroup(doc);
            tg.Start("Remove Un-Used Reference Planes");
            foreach (var id in refIDs)
            {
                var filt2 = new ElementClassFilter(typeof(FamilyInstance));

                var filt3 = new ElementParameterFilter(new FilterElementIdRule(new ParameterValueProvider(new ElementId(BuiltInParameter.HOST_ID_PARAM)),
                    new FilterNumericEquals(), id));
                var filt4 = new LogicalAndFilter(filt2, filt3);

                var thing = new FilteredElementCollector(doc);

                using var t = new Transaction(doc);
                // Check for hosted elements on the plane
                if (thing.WherePasses(filt4).Count() == 0)
                {
                    t.Start("Do The Thing");

#if Revit2018
              if (doc.GetElement(id).GetDependentElements(new ElementClassFilter(typeof(FamilyInstance))).Count == 0)
              {
                doc.Delete(id);
              }

              t.Commit();
#else
                    // Make sure there is nothing measuring to the plane

                    if (doc.Delete(id).Count() > 1)
                        t.Dispose();
                    // Skipped
                    else
                        // Deleted
                        t.Commit();
#endif
                }
            }

            tg.Assimilate();

            return Result.Succeeded;
        }
    }

    #endregion // Broken command in Revit 2019 by Austin Sudtelgte

    #region New working command in Revit 2019 by Austin Sudtelgte

    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdDeleteUnusedRefPlanes : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var refplaneids
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ReferencePlane))
                    .ToElementIds();

            using var tg = new TransactionGroup(doc);
            tg.Start("Remove unused reference planes");

            var instances
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance));

            var toKeep
                = new Dictionary<ElementId, int>();

            foreach (FamilyInstance i in instances)
                // Ensure the element is hosted

                if (null != i.Host)
                {
                    var hostId = i.Host.Id;

                    // Check list to see if we've already added this plane

                    if (!toKeep.ContainsKey(hostId)) toKeep.Add(hostId, 0);
                    ++toKeep[hostId];
                }

            // Loop through reference planes and 
            // delete the ones not in the list toKeep.

            foreach (var refid in refplaneids)
                if (!toKeep.ContainsKey(refid))
                {
                    using var t = new Transaction(doc);
                    t.Start($"Removing plane {doc.GetElement(refid).Name}");

                    // Ensure there are no dimensions measuring to the plane

                    if (doc.Delete(refid).Count > 1)
                        t.Dispose();
                    else
                        t.Commit();
                }

            tg.Assimilate();

            return Result.Succeeded;
        }
    }

    #endregion // New working command in Revit 2019 by Austin Sudtelgte
}