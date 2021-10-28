#region Header

//
// CmdCurtainWallGeom.cs - retrieve curtain wall geometry
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdCurtainWallGeom : IExternalCommand
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

            if (Util.SelectSingleElementOfType(
                uidoc, typeof(Wall), "a curtain wall", false) is not Wall wall)
            {
                message = "Please select a single "
                          + "curtain wall element.";

                return Result.Failed;
            }

            var locationcurve
                = wall.Location as LocationCurve;

            var curve = locationcurve.Curve;

            // move whole geometry over by length of wall:

            var p = curve.GetEndPoint(0);
            var q = curve.GetEndPoint(1);
            var v = q - p;

            var tv = Transform.CreateTranslation(v);

            //curve = curve.get_Transformed( tv ); // 2013
            curve = curve.CreateTransformed(tv); // 2014

            var creator = new Creator(doc);
            creator.CreateModelCurve(curve);

            var opt = app.Create.NewGeometryOptions();
            opt.IncludeNonVisibleObjects = true;

            var e = wall.get_Geometry(opt);

            using var t = new Transaction(doc);
            t.Start("Create Model Curves");

            foreach (var obj in e)
            {
                curve = obj as Curve;

                if (null != curve)
                {
                    //curve = curve.get_Transformed( tv ); // 2013
                    curve = curve.CreateTransformed(tv); // 2014
                    creator.CreateModelCurve(curve);
                }
            }

            t.Commit();

            return Result.Succeeded;
        }

        #region List Wall Geometry

        private void list_wall_geom(Wall w, Application app)
        {
            var s = "";

            var cgrid = w.CurtainGrid;

            var options
                = app.Create.NewGeometryOptions();

            options.ComputeReferences = true;
            options.IncludeNonVisibleObjects = true;

            var geomElem
                = w.get_Geometry(options);

            foreach (var obj in geomElem)
            {
                var vis = obj.Visibility;

                var visString = vis.ToString();

                var arc = obj as Arc;
                var line = obj as Line;
                var solid = obj as Solid;

                if (arc != null)
                {
                    var length = arc.ApproximateLength;

                    s += $"Length (arc) ({visString}): {length}\n";
                }

                if (line != null)
                {
                    var length = line.ApproximateLength;

                    s += $"Length (line) ({visString}): {length}\n";
                }

                if (solid != null)
                {
                    var faceCount = solid.Faces.Size;

                    s += $"Faces: {faceCount}\n";

                    foreach (Face face in solid.Faces)
                        s += $"Face area ({visString}): {face.Area}\n";
                }

                if (line == null && solid == null && arc == null) s += "<Other>\n";
            }

            TaskDialog.Show("revit", s);
        }

        #endregion // List Wall Geometry

        #region Retrieve Curtain Wall Panel Geometry with Basic Wall Panel

        /// <summary>
        ///     GetElementSolids dummy placeholder function.
        ///     The real one would retrieve all solids from the
        ///     given element geometry.
        /// </summary>
        private List<Solid> GetElementSolids(Element e)
        {
            return null;
        }

        /// <summary>
        ///     GetCurtainWallPanelGeometry retrieves all solids
        ///     from a curtain wall, including Basic panel walls.
        /// </summary>
        private void GetCurtainWallPanelGeometry(
            Document doc,
            ElementId curtainWallId,
            List<Solid> solids)
        {
            // First, find solid geometry from panel ids.
            // Note that the panel which contains a basic
            // wall has NO geometry!

            var wall = doc.GetElement(curtainWallId) as Wall;
            var grid = wall.CurtainGrid;

            foreach (var id in grid.GetPanelIds())
            {
                var e = doc.GetElement(id);
                solids.AddRange(GetElementSolids(e));
            }

            // Secondly, find corresponding panel wall
            // for the curtain wall and retrieve the actual
            // geometry from that.

            var cwPanels
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_CurtainWallPanels)
                    .OfClass(typeof(Wall));

            foreach (Wall cwp in cwPanels)
                // Find panel wall belonging to this curtain wall
                // and retrieve its geometry

                if (cwp.StackedWallOwnerId == curtainWallId)
                    solids.AddRange(GetElementSolids(cwp));
        }

        #endregion // Retrieve Curtain Wall Panel Geometry with Basic Wall Panel
    }
}

// C:\a\j\adn\case\bsd\1259898\attach\curtain_wall.rvt