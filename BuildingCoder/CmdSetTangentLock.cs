#region Header

//
// CmdSetTangentLock.cs - set tangent lock on adjoining curve elements
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Written by Christian @chhadidg73 in the
    ///     Revit API discussion forum thread
    ///     http://forums.autodesk.com/t5/revit-api-forum/settangentlock-in-profilesketch/m-p/6587402
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdSetTangentLock : IExternalCommand
    {
        // Conversion constants from millimetres to feet.

        private const double mm_per_inch = 25.4;
        private const double inches_per_foot = 12.0;
        private const double mm = mm_per_inch * inches_per_foot;

        /// <summary>
        ///     Target radius one.
        /// </summary>
        private const double r1 = 16 * mm;

        /// <summary>
        ///     Target radius two.
        /// </summary>
        private const double r2 = 12 * mm;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            message = "Sorry, no sample model available. "
                      + "Please refer to the Revit API discussion "
                      + "forum thread and blog post instead.";

            return Result.Failed;
        }

        private void SetTangentLockInProfileSketch1(
            Document famdoc,
            Form[] extrusions)
        {
            ICollection<ElementId> delIds = null;
            var enmIDs = new List<ElementId>();

            using (var delTrans = new SubTransaction(famdoc))
            {
                try
                {
                    delTrans.Start();
                    delIds = famdoc.Delete(extrusions[0].Id);
                    delTrans.RollBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            // Get the model lines in the profile and use the 
            // end points for reference the sketch dimensions diameter

            var mArcsR1 = new List<ModelArc>();
            var mArcsR2 = new List<ModelArc>();
            var mLines = new List<ModelLine>();

            foreach (var id in delIds) enmIDs.Add(id);

            for (var i = 0; i < enmIDs.Count; i++)
            {
                var ele = famdoc.GetElement(enmIDs[i]);
                if (ele is ModelArc ma)
                {
                    var c = ma.GeometryCurve;
                    var a = c as Arc;

                    if (Math.Round(r1, 6) == Math.Round(a.Radius, 6)) mArcsR1.Add(ma);
                    if (Math.Round(r2, 6) == Math.Round(a.Radius, 6)) mArcsR2.Add(ma);
                }

                if (ele is ModelLine ml)
                {
                    Element before = null;
                    Element after = null;
                    ElementId beforeId = null;
                    ElementId afterId = null;

                    if (i > 0)
                    {
                        before = famdoc.GetElement(enmIDs[i - 1]);
                        beforeId = enmIDs[i - 1];
                    }
                    else
                    {
                        before = famdoc.GetElement(enmIDs[enmIDs.Count - 1]);
                        beforeId = enmIDs[enmIDs.Count - 1];
                    }

                    if (i == enmIDs.Count - 1)
                    {
                        after = famdoc.GetElement(enmIDs[0]);
                        afterId = enmIDs[0];
                    }
                    else
                    {
                        after = famdoc.GetElement(enmIDs[i + 1]);
                        afterId = enmIDs[i + 1];
                    }

                    if (before is ModelArc && after is ModelArc)
                    {
                        ml.SetTangentLock(0, beforeId, true);
                        ml.SetTangentLock(1, afterId, true);
                    }
                }
            }
        }

        private void SetTangentLockInProfileSketch2(
            Document famdoc,
            Form[] extrusions)
        {
            ICollection<ElementId> delIds = null;
            var enmIDs = new List<ElementId>();

            using (var delTrans = new SubTransaction(famdoc))
            {
                try
                {
                    delTrans.Start();
                    delIds = famdoc.Delete(extrusions[0].Id);
                    delTrans.RollBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            // Get the model lines in the profile and use the end
            // points for reference the sketch dimensions diameter

            var mArcsR1 = new List<ModelArc>();
            var mArcsR2 = new List<ModelArc>();
            var mLines = new List<ModelLine>();

            foreach (var id in delIds) enmIDs.Add(id);

            for (var i = 0; i < enmIDs.Count; i++)
            {
                var ele = famdoc.GetElement(enmIDs[i]);
                if (ele is ModelArc ma)
                {
                    var c = ma.GeometryCurve;
                    var a = c as Arc;

                    if (Math.Round(r1, 6) == Math.Round(a.Radius, 6)) mArcsR1.Add(ma);
                    if (Math.Round(r2, 6) == Math.Round(a.Radius, 6)) mArcsR2.Add(ma);
                }

                if (ele is ModelLine ml)
                {
                    ElementId beforeId = null;
                    ElementId afterId = null;

                    var joinedBefore = ml.GetAdjoinedCurveElements(0);
                    foreach (var id in joinedBefore)
                    {
                        var joinedEle = famdoc.GetElement(id);

                        if (joinedEle is ModelArc)
                        {
                            beforeId = id;
                            break;
                        }
                    }

                    var joinedAfter = ml.GetAdjoinedCurveElements(1);
                    foreach (var id in joinedAfter)
                    {
                        var joinedEle = famdoc.GetElement(id);

                        if (joinedEle is ModelArc)
                        {
                            afterId = id;
                            break;
                        }
                    }

                    if (beforeId != null
                        && afterId != null
                        && ml.HasTangentJoin(0, beforeId)
                        && ml.HasTangentJoin(1, afterId))
                    {
                        ml.SetTangentLock(0, beforeId, true);
                        ml.SetTangentLock(1, afterId, true);
                    }
                }
            }
        }
    }
}