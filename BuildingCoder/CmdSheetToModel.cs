#region Header

//
// CmdSheetToModel.cs - Convert sheet to model coordinates and convert DWF markup to model elements
//
// Copyright (C) 2015-2020 by Paolo Serra and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.UI;
// Miro Ambiguities
using ApplicationRvt = Autodesk.Revit.ApplicationServices.Application;
using ViewRvt = Autodesk.Revit.DB.View;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSheetToModel : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var doc = uiapp.ActiveUIDocument.Document;

            QTO_2_PlaceHoldersFromDWFMarkups(
                doc, "DWF Markup");

            return Result.Succeeded;
        }

        public void QTO_2_PlaceHoldersFromDWFMarkups(
            Document doc,
            string activityId)
        {
            var activeView = doc.ActiveView;

            if (!(activeView is ViewSheet vs))
            {
                TaskDialog.Show("QTO",
                    "The current view must be a Sheet View with DWF markups");
                return;
            }

            var vp = doc.GetElement(
                vs.GetAllViewports().First()) as Viewport;

            var plan = doc.GetElement(vp.ViewId) as View;

            var scale = vp.Parameters.Cast<Parameter>()
                .First(x => x.Id.IntegerValue.Equals(
                    (int) BuiltInParameter.VIEW_SCALE))
                .AsInteger();

            var dwfMarkups
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ImportInstance))
                    .WhereElementIsNotElementType()
                    .Where(x => x.Name.StartsWith("Markup")
                                && x.OwnerViewId.IntegerValue.Equals(
                                    activeView.Id.IntegerValue));

            using var tg = new TransactionGroup(doc);
            tg.Start("DWF markups placeholders");

            using (var t = new Transaction(doc))
            {
                t.Start("DWF Transfer");

                plan.Parameters.Cast<Parameter>()
                    .First(x => x.Id.IntegerValue.Equals(
                        (int) BuiltInParameter.VIEWER_CROP_REGION))
                    .Set(1);

                var VC = (plan.CropBox.Min + plan.CropBox.Max) / 2;

                var BC = vp.GetBoxCenter();

                t.RollBack();

                foreach (var e in dwfMarkups)
                {
                    var GeoElem = e.get_Geometry(new Options());

                    var gi = GeoElem.Cast<GeometryInstance>().First();

                    var gei = gi.GetSymbolGeometry();

                    IList<GeometryObject> gos = new List<GeometryObject>();

                    if (gei.Count(x => x is Arc) > 0) continue;

                    foreach (var go in gei)
                    {
                        var med = new XYZ();

                        if (go is PolyLine pl)
                        {
                            var min = new XYZ(pl.GetCoordinates().Min(p => p.X),
                                pl.GetCoordinates().Min(p => p.Y),
                                pl.GetCoordinates().Min(p => p.Z));

                            var max = new XYZ(pl.GetCoordinates().Max(p => p.X),
                                pl.GetCoordinates().Max(p => p.Y),
                                pl.GetCoordinates().Max(p => p.Z));

                            med = (min + max) / 2;
                        }

                        med = med - BC;

                        // Convert DWF sheet coordinates into model coordinates

                        var a = VC + new XYZ(med.X * scale, med.Y * scale, 0);
                    }
                }

                t.Start("DWF Transfer");

                foreach (var e in dwfMarkups)
                {
                    var GeoElem = e.get_Geometry(new Options());

                    var gi = GeoElem.Cast<GeometryInstance>().First();

                    var gei = gi.GetSymbolGeometry();

                    IList<GeometryObject> gos = new List<GeometryObject>();

                    if (gei.Count(x => x is Arc) == 0) continue;

                    foreach (var go in gei)
                        if (go is Arc)
                        {
                            var c = go as Curve;

                            var med = c.Evaluate(0.5, true);

                            med = med - BC;

                            var a = VC + new XYZ(med.X * scale, med.Y * scale, 0);

                            // Warning CS0618: 
                            // Autodesk.Revit.Creation.ItemFactoryBase.NewTextNote(
                            //   View, XYZ, XYZ, XYZ, double, TextAlignFlags, string) 
                            // is obsolete: 
                            // This method is deprecated in Revit 2016. 
                            // Please use one of the TextNote.Create methods instead.

                            //doc.Create.NewTextNote( plan,
                            //                       a,
                            //                       XYZ.BasisX,
                            //                       XYZ.BasisY,
                            //                       MMtoFeet( 5 ),
                            //                       TextAlignFlags.TEF_ALIGN_CENTER,
                            //                       activityId );

                            var textTypeId = new FilteredElementCollector(doc)
                                .OfClass(typeof(TextNoteType))
                                .FirstElementId();

                            TextNote.Create(doc, plan.Id, a, activityId, textTypeId);
                        }

                    t.Commit();
                }
            }

            tg.Assimilate();
        }
    }

    #region CmdMiroTest2

    [Transaction(TransactionMode.Manual)]
    public class CmdMiroTest2 : IExternalCommand
    {
        public ApplicationRvt _app;

        // KIS - public fields
        public UIApplication _appUI;
        public Document _doc;
        public UIDocument _docUI;

        Result IExternalCommand.Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // cache admin data
            _appUI = commandData.Application;
            _app = _appUI.Application;
            _docUI = commandData.Application.ActiveUIDocument;
            _doc = _docUI.Document;

            try // generic
            {
                // Current View must be Sheet
                if (_doc.ActiveView is not ViewSheet sheet)
                {
                    Util.ErrorMsg("Current View is NOT a Sheet!");
                    return Result.Cancelled;
                }

                // There must be a Floor Plan named "Level 0" 
                // which is the "master" to align to
                Viewport vpMaster = null;
                // There must be at least one more Floor Plan 
                // View to align (move)
                var vpsSlave = new List<Viewport>();
                // Find them:
                foreach (var idVp in sheet.GetAllViewports())
                {
                    var vp = _doc.GetElement(idVp) as Viewport;
                    var v = _doc.GetElement(vp.ViewId) as ViewRvt;
                    if (v.ViewType == ViewType.FloorPlan)
                    {
                        if (v.Name.Equals("Level 0", StringComparison
                            .CurrentCultureIgnoreCase))
                            vpMaster = vp;
                        else
                            vpsSlave.Add(vp);
                    } //if FloorPlan
                } //foreeach idVp

                // Check if got them all
                if (null == vpMaster)
                {
                    Util.ErrorMsg("NO 'Level 0' Floor Plan on the Sheet!");
                    return Result.Cancelled;
                }

                if (vpsSlave.Count == 0)
                {
                    Util.ErrorMsg("NO other Floor Plans to adjust on the Sheet!");
                    return Result.Cancelled;
                }

                // Process Master
                // --------------

                var ptMasterVpCenter = vpMaster.GetBoxCenter();
                var viewMaster = _doc.GetElement(
                    vpMaster.ViewId) as ViewRvt;
                double scaleMaster = viewMaster.Scale;

                // Process Slaves
                // --------------

                using var t = new Transaction(_doc);
                t.Start("Set Box Centres");

                foreach (var vpSlave in vpsSlave)
                {
                    var ptSlaveVpCenter = vpSlave.GetBoxCenter();
                    var viewSlave = _doc.GetElement(
                        vpSlave.ViewId) as ViewRvt;
                    double scaleSlave = viewSlave.Scale;
                    // MUST be the same scale, otherwise can't really overlap
                    if (scaleSlave != scaleMaster) continue;

                    // Work out how to move the center of Slave 
                    // Viewport to coincide model-wise with Master
                    // (must use center as only Viewport.SetBoxCenter 
                    // is provided in API)
                    // We can ignore View.Outline as Viewport.GetBoxOutline 
                    // is ALWAYS the same dimensions enlarged by 
                    // 0.01 ft in each direction.
                    // This guarantees that the center of View is 
                    // also center of Viewport, BUT there is a 
                    // problem when any Elevation Symbols outside 
                    // the crop box are visible (can't work out why
                    // - BUG?, or how to calculate it all if BY-DESIGN)

                    var bbm = viewMaster.CropBox;
                    var bbs = viewSlave.CropBox;

                    // 0) Center points in WCS
                    var wcsCenterMaster = 0.5 * bbm.Min.Add(bbm.Max);
                    var wcsCenterSlave = 0.5 * bbs.Min.Add(bbs.Max);

                    // 1) Delta (in model's feet) of the slave center w.r.t master center
                    var deltaX = wcsCenterSlave.X - wcsCenterMaster.X;
                    var deltaY = wcsCenterSlave.Y - wcsCenterMaster.Y;

                    // 1a) Scale to Delta in Sheet's paper-space feet
                    deltaX *= 1.0 / scaleMaster;
                    deltaY *= 1.0 / scaleMaster;

                    // 2) New center point for the slave viewport, so *models* "overlap":
                    var newCenter = new XYZ(
                        ptMasterVpCenter.X + deltaX,
                        ptMasterVpCenter.Y + deltaY,
                        ptSlaveVpCenter.Z);
                    vpSlave.SetBoxCenter(newCenter);
                }

                t.Commit();
            }
            catch (Exception ex)
            {
                Util.ErrorMsg($"Generic exception: {ex.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    } // CmdMiroTest2

    #endregion // CmdMiroTest2
}