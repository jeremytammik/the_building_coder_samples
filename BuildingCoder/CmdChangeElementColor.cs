#region Header

//
// CmdChangeElementColor.cs - Change element colour using OverrideGraphicSettings for active view
//
// Also change its category's material to a random material
//
// Copyright (C) 2020-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    public class CmdChangeElementColor : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            var view = doc.ActiveView;
            ElementId id;

            try
            {
                var sel = uidoc.Selection;
                var r = sel.PickObject(
                    ObjectType.Element,
                    "Pick element to change its colour");
                id = r.ElementId;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }

            ChangeElementColor(doc, id);

            ChangeElementMaterial(doc, id);

            return Result.Succeeded;
        }

        private void ChangeElementColor(Document doc, ElementId id)
        {
            var color = new Color(
                200, 100, 100);

            var ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(color);

            using var tx = new Transaction(doc);
            tx.Start("Change Element Color");
            doc.ActiveView.SetElementOverrides(id, ogs);
            tx.Commit();
        }

        private void ChangeElementMaterial(Document doc, ElementId id)
        {
            var e = doc.GetElement(id);

            if (null != e.Category)
            {
                var im = e.Category.Material.Id.IntegerValue;

                var materials = new List<Material>(
                    new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(Material))
                        .ToElements()
                        .Where(m
                            => m.Id.IntegerValue != im)
                        .Cast<Material>());

                var r = new Random();
                var i = r.Next(materials.Count);

                using var tx = new Transaction(doc);
                tx.Start("Change Element Material");
                e.Category.Material = materials[i];
                tx.Commit();
            }
        }

        #region Paint Stairs

        // https://forums.autodesk.com/t5/revit-api-forum/paint-stair-faces/m-p/10388359
        private void PaintStairs(UIDocument uidoc, Material mat)
        {
            var doc = uidoc.Document;
            var sel = uidoc.Selection;

            //FaceSelectionFilter filter = new FaceSelectionFilter();
            var pickedRef = sel.PickObject(
                ObjectType.PointOnElement,
                //filter, 
                "Please select a Face");

            var elem = doc.GetElement(pickedRef);

            var geoObject = elem
                .GetGeometryObjectFromReference(pickedRef);

            var fc = geoObject as Face;

            if (elem.Category.Id.IntegerValue == -2000120) // Stairs
            {
                var flag = false;
                var str = elem as Stairs;
                var landings = str.GetStairsLandings();
                var runs = str.GetStairsLandings();
                using var transaction = new Transaction(doc);
                transaction.Start("Paint Material");
                foreach (var id in landings)
                {
                    doc.Paint(id, fc, mat.Id);
                    flag = true;
                    break;
                }

                if (!flag)
                    foreach (var id in runs)
                    {
                        doc.Paint(id, fc, mat.Id);
                        break;
                    }

                transaction.Commit();
            }
        }

        /// <summary>
        ///     Prompt user to pick a face and paint it
        ///     with the given material. If the face belongs
        ///     to a stair run or landing, paint that part
        ///     of the stair specifically.
        /// </summary>
        private void PaintSelectedFace(UIDocument uidoc, Material mat)
        {
            var doc = uidoc.Document;
            var sel = uidoc.Selection;
            var errors = new List<string>();
            //FaceSelectionFilter filter = new FaceSelectionFilter();
            var pickedRef = sel.PickObject(
                ObjectType.PointOnElement,
                //filter, 
                "Please select a face to paint");

            var elem = doc.GetElement(pickedRef);

            var geoObject = elem
                .GetGeometryObjectFromReference(pickedRef);

            var selected_face = geoObject as Face;

            using var transaction = new Transaction(doc);
            transaction.Start("Paint Selected Face");

            if (elem.Category.Id.IntegerValue.Equals(
                (int) BuiltInCategory.OST_Stairs))
            {
                var str = elem as Stairs;
                var IsLand = false;

                var landings = str.GetStairsLandings();
                var runs = str.GetStairsRuns();

                foreach (var id in landings)
                {
                    var land = doc.GetElement(id);
                    var solids = GetElemSolids(
                        land.get_Geometry(new Options()));

                    IsLand = SolidsContainFace(solids, selected_face);

                    if (IsLand) break;
                }

                if (IsLand)
                    foreach (var id in landings)
                    {
                        doc.Paint(id, selected_face, mat.Id);
                        break;
                    }
                else
                    foreach (var id in runs)
                    {
                        doc.Paint(id, selected_face, mat.Id);
                        break;
                    }
            }
            else
            {
                try
                {
                    doc.Paint(elem.Id, selected_face, mat.Id);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error painting selected face",
                        ex.Message);
                }
            }

            transaction.Commit();
        }

        /// <summary>
        ///     Does the given face belong to one of the given solids?
        /// </summary>
        private bool SolidsContainFace(List<Solid> solids, Face face)
        {
            foreach (var s in solids)
                if (null != s
                    && 0 < s.Volume)
                    foreach (Face f in s.Faces)
                        if (f == face)
                            return true;
                        else if (f.HasRegions)
                            foreach (var f2 in f.GetRegions())
                                if (f2 == face)
                                    return true;
            return false;
        }

        /// <summary>
        ///     Recursively collect all solids
        ///     contained in the given element geomety
        /// </summary>
        private List<Solid> GetElemSolids(GeometryElement geomElem)
        {
            var solids = new List<Solid>();

            if (null != geomElem)
                foreach (var geomObj in geomElem)
                    switch (geomObj)
                    {
                        case Solid solid when solid.Faces.Size > 0:
                            solids.Add(solid);
                            continue;
                        case GeometryInstance geomInst:
                            solids.AddRange(GetElemSolids(
                                geomInst.GetInstanceGeometry()));
                            break;
                    }

            return solids;
        }

        #endregion // Paint Stairs
    }
}