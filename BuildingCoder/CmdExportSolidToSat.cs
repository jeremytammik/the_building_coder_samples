#region Header

//
// CmdExportSolidToSat.cs - Create a solid in memory and export it to a SAT file
//
// Copyright (C) 2013-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdExportSolidToSat : IExternalCommand
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
            var sel = uidoc.Selection;

            // Retrieve all floors from the model

            var floors
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .ToElements()
                    .Cast<Floor>()
                    .ToList();

            if (2 != floors.Count)
            {
                message = "Please create two intersected floors";
                return Result.Failed;
            }

            // Retrieve the floor solids

            var opt = new Options();

            var geometry1 = floors[0].get_Geometry(opt);
            var geometry2 = floors[1].get_Geometry(opt);

            var solid1 = geometry1.FirstOrDefault() as Solid;
            var solid2 = geometry2.FirstOrDefault() as Solid;

            // Calculate the intersection solid

            var intersectedSolid = BooleanOperationsUtils
                .ExecuteBooleanOperation(solid1, solid2,
                    BooleanOperationsType.Intersect);

            // Search for the metric mass family template file

            var template_path = DirSearch(
                app.FamilyTemplatePath,
                "Metric Mass.rft");

            // Create a new temporary family

            var family_doc = app.NewFamilyDocument(
                template_path);

            // Create a free form element 
            // from the intersection solid

            using (var t = new Transaction(family_doc))
            {
                t.Start("Add Free Form Element");

                var freeFormElement = FreeFormElement.Create(
                    family_doc, intersectedSolid);

                t.Commit();
            }

            var dir = Path.GetTempPath();

            var filepath = Path.Combine(dir,
                "floor_intersection_family.rfa");

            var sao = new SaveAsOptions
            {
                OverwriteExistingFile = true
            };

            family_doc.SaveAs(filepath, sao);

            // Create 3D View

            var viewFamilyType
                = new FilteredElementCollector(family_doc)
                    .OfClass(typeof(ViewFamilyType))
                    .OfType<ViewFamilyType>()
                    .FirstOrDefault(x =>
                        x.ViewFamily == ViewFamily.ThreeDimensional);

            View3D threeDView;

            using (var t = new Transaction(family_doc))
            {
                t.Start("Create 3D View");

                threeDView = View3D.CreateIsometric(
                    family_doc, viewFamilyType.Id);

                t.Commit();
            }

            // Export to SAT

            var viewSet = new List<ElementId>
            {
                threeDView.Id
            };

            var exportOptions
                = new SATExportOptions();

            var res = family_doc.Export(dir,
                "SolidFile.sat", viewSet, exportOptions);

            return Result.Succeeded;
        }

        #region Intersect solid with another solid from a linked file

        public static double GetIntersectedSolidArea(
            Document host,
            Solid hostElement,
            RevitLinkInstance rins,
            Solid linkedElement)
        {
            // Step 1 "Determine the transformation T from the linked document Q coordinates to P's."

            var transForm = rins.GetTransform();

            // Step 2 "Open the linked project Q and retrieve the solid Sb of B."

            // linkedElement is Solid of linked Link

            // Step 3 "Transform it to P's coordinate space: T * Sb."

            var tmp = SolidUtils.CreateTransformed(linkedElement, transForm);

            // Step 4 "Retrieve the solid Sa of A"
            // hostElement is hostElementSolid

            var result = BooleanOperationsUtils.ExecuteBooleanOperation(
                hostElement, tmp, BooleanOperationsType.Intersect);


            return result.SurfaceArea;
        }

        #endregion // Intersect solid with another solid from a linked file

        #region Clone Solid Workaround before new Revit 2016 Solid.Clone API

        public static Solid Clone( /*this*/ Solid solid)
        {
            if (solid == null) return null;

            // Better than unioning the solid with itself:
            // use a small cube contained within the original 
            // solid instead, e.g. a 1x1x1 cube at the origin 
            // or something.

            return BooleanOperationsUtils
                .ExecuteBooleanOperation(solid, solid,
                    BooleanOperationsType.Union);
        }

        #endregion // Clone Solid Workaround

        /// <summary>
        ///     Return the full path of the first file
        ///     found matching the given filename pattern
        ///     in a recursive search through all
        ///     subdirectories of the given starting folder.
        /// </summary>
        private string DirSearch(
            string start_dir,
            string filename_pattern)
        {
            foreach (var d in Directory.GetDirectories(
                start_dir))
            {
                foreach (var f in Directory.GetFiles(
                    d, filename_pattern))
                    return f;

                var f2 = DirSearch(d, filename_pattern);

                if (null != f2) return f2;
            }

            return null;
        }
    }
}