#region Header

//
// CmdNewCrossFitting.cs - Create a new pipe cross fitting
//
// Copyright (C) 2014-2020 by Joe Ye and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewCrossFitting : IExternalCommand
    {
        /// <summary>
        ///     External command mainline. Run in the
        ///     sample model TestCrossFitting.rvt, e.g.
        /// </summary>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            IList<Element> pipes = null;
            var n = 0;

            // Ensure that 2, 3 or 4 pipes are selected.

            while (n is < 2 or > 4)
            {
                if (0 != n)
                    Util.InfoMsg(string.Format(
                        "You picked {0} pipe{1}. "
                        + "Please only pick 2, 3 or 4.",
                        n, Util.PluralSuffix(n)));

                try
                {
                    var sel = app.ActiveUIDocument.Selection;

                    pipes = sel.PickElementsByRectangle(
                        new JtElementsOfClassSelectionFilter<Pipe>(),
                        "Please pick some pipes.");
                }
                catch (InvalidOperationException)
                {
                    return Result.Cancelled;
                }

                n = pipes.Count;
            }

            XYZ pt = null;

            using var tx = new Transaction(doc);
            tx.Start("CreateConnector");
            if (pipes.Count() <= 1)
                return Result.Cancelled;

            var pipe1 = pipes[0] as Pipe;
            var pipe2 = pipes[1] as Pipe;

            var curve1 = pipe1.GetCurve();
            var curve2 = pipe2.GetCurve();

            var p1 = curve1.GetEndPoint(0);
            var q1 = curve1.GetEndPoint(1);

            var p2 = curve2.GetEndPoint(0);
            var q2 = curve2.GetEndPoint(1);

            if (q1.DistanceTo(p2) < 0.1)
            {
                pt = (q1 + p2) * 0.5;
            }
            else if (q1.DistanceTo(q2) < 0.1)
            {
                pt = (q1 + q2) * 0.5;
            }
            else if (p1.DistanceTo(p2) < 0.1)
            {
                pt = (p1 + p2) * 0.5;
            }
            else if (p1.DistanceTo(q2) < 0.1)
            {
                pt = (p1 + q2) * 0.5;
            }
            else
            {
                message = "Please select two pipes "
                          + "with near-by endpoints.";

                return Result.Failed;
            }

            var c1 = Util.GetConnectorClosestTo(
                pipe1, pt);

            var c2 = Util.GetConnectorClosestTo(
                pipe2, pt);

            switch (pipes.Count())
            {
                case 2 when IsPipeParallel(pipe1, pipe2):
                    doc.Create.NewUnionFitting(c1, c2);
                    break;
                case 2:
                    doc.Create.NewElbowFitting(c1, c2);
                    break;
                case 3:
                {
                    var pipe3 = pipes[2] as Pipe;

                    var v1 = GetPipeDirection(pipe1);
                    var v2 = GetPipeDirection(pipe2);
                    var v3 = GetPipeDirection(pipe3);

                    var c3 = Util.GetConnectorClosestTo(
                        pipe3, pt);

                    if (Math.Sin(v1.AngleTo(v2)) < 0.01) //平行
                    {
                        doc.Create.NewTeeFitting(c1, c2, c3);
                    }
                    else //v1, 和v2 垂直.
                    {
                        if (Math.Sin(v3.AngleTo(v1)) < 0.01) //v3, V1 平行
                            doc.Create.NewTeeFitting(c3, c1, c2);
                        else //v3, v2 平行
                            doc.Create.NewTeeFitting(c3, c2, c1);
                    }

                    break;
                }
                case 4:
                {
                    var pipe3 = pipes[2] as Pipe;
                    var pipe4 = pipes[3] as Pipe;

                    var c3 = Util.GetConnectorClosestTo(
                        pipe3, pt);

                    var c4 = Util.GetConnectorClosestTo(
                        pipe4, pt);

                    //以从哪c1为入口.

                    // The required connection order for a cross 
                    // fitting is main – main – side - side.

                    if (IsPipeParallel(pipe1, pipe2))
                        doc.Create.NewCrossFitting(
                            c1, c2, c3, c4);
                    else if (IsPipeParallel(pipe1, pipe3))
                        try
                        {
                            doc.Create.NewCrossFitting(
                                c1, c3, c2, c4);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(
                                "Cannot insert cross fitting",
                                ex.Message);
                        }
                    else if (IsPipeParallel(pipe1, pipe4))
                        doc.Create.NewCrossFitting(
                            c1, c4, c2, c3);

                    break;
                }
            }

            tx.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return the normalised direction of the given pipe.
        /// </summary>
        private XYZ GetPipeDirection(Pipe pipe)
        {
            var c = pipe.GetCurve();
            var dir = c.GetEndPoint(1) - c.GetEndPoint(1);
            dir = dir.Normalize();
            return dir;
        }

        /// <summary>
        ///     Are the two given pipes parallel?
        /// </summary>
        private bool IsPipeParallel(Pipe p1, Pipe p2)
        {
            var c1 = p1.GetCurve() as Line;
            var c2 = p2.GetCurve() as Line;
            return Math.Sin(c1.Direction.AngleTo(
                c2.Direction)) < 0.01;
        }

        #region Using routing preferences with NewTakeoffFitting

        private Connector CreateTemporaryConnectorForTap()
        {
            return null;
        }

        private void SelectAndPlaceTakeOffFitting(Document doc)
        {
            var mainDuctId = ElementId.InvalidElementId;

            // Get DuctType - we need this for its
            // RoutingPreferenceManager. This is how we assign
            // our tap object to be used. This is the settings
            // for the duct object we attach our tap to.

            var duct = doc.GetElement(mainDuctId) as Duct;

            var ductType = duct.DuctType;

            var routePrefManager
                = ductType.RoutingPreferenceManager;

            // Set Junction Prefernce to Tap.

            routePrefManager.PreferredJunctionType
                = PreferredJunctionType.Tap;

            // For simplicity sake, I remove all previous rules
            // for taps so I can just add what I want here.
            // This will probably vary.

            var initRuleCount = routePrefManager.GetNumberOfRules(
                RoutingPreferenceRuleGroupType.Junctions);

            for (var i = 0; i != initRuleCount; ++i)
                routePrefManager.RemoveRule(
                    RoutingPreferenceRuleGroupType.Junctions, 0);

            // Get FamilySymbol for Tap I want to use.

            FamilySymbol tapSym = null;
            doc.LoadFamilySymbol("C:/FamilyLocation/MyTap.rfa",
                "MyTap", out tapSym);

            // Symbol needs to be activated before use.

            if (!tapSym.IsActive && tapSym != null)
            {
                tapSym.Activate();
                doc.Regenerate();
            }

            // Create Rule that utilizes the Tap. Use the argument
            // MEPPartId = ElementId for the desired FamilySymbol.

            var newRule
                = new RoutingPreferenceRule(tapSym.Id, "MyTap");

            routePrefManager.AddRule(
                RoutingPreferenceRuleGroupType.Junctions, newRule);

            // To create a solid tap, we need to use the Revit
            // doc.Create.NewTakeoffFitting routine. For this,
            // we need a connector. If we don't have one, we
            // just create a temporary object with a connector
            // where we want it.

            var tmpConn = CreateTemporaryConnectorForTap();

            // Create our tap.

            var tapInst
                = doc.Create.NewTakeoffFitting(tmpConn, duct);
        }

        #endregion // Using routing preferences with NewTakeoffFitting
    }
}