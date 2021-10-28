#region Header

//
// CmdSlabSides.cs - determine vertical slab 'side' faces
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSlabSides : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var floors = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                floors, uidoc, typeof(Floor)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some floor elements."
                    : "No floor elements found.";
                return Result.Failed;
            }

            var faces = new List<Face>();
            var opt = app.Application.Create.NewGeometryOptions();

            foreach (Floor floor in floors)
            {
                var geo = floor.get_Geometry(opt);
                //GeometryObjectArray objects = geo.Objects; // 2012
                //foreach( GeometryObject obj in objects ) // 2012
                foreach (var obj in geo) // 2013
                {
                    var solid = obj as Solid;
                    if (solid != null) GetSideFaces(faces, solid);
                }
            }

            var n = faces.Count;

            Debug.Print(
                "{0} side face{1} found.",
                n, Util.PluralSuffix(n));

            using var t = new Transaction(doc);
            t.Start("Draw Face Triangle Normals");
            var creator = new Creator(doc);
            foreach (var f in faces) creator.DrawFaceTriangleNormals(f);
            t.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Determine the vertical boundary faces
        ///     of a given "horizontal" solid object
        ///     such as a floor slab. Currently only
        ///     supports planar and cylindrical faces.
        /// </summary>
        /// <param name="verticalFaces">Return solid vertical boundary faces, i.e. 'sides'</param>
        /// <param name="solid">Input solid</param>
        private void GetSideFaces(
            List<Face> verticalFaces,
            Solid solid)
        {
            var faces = solid.Faces;
            foreach (Face f in faces)
                switch (f)
                {
                    case PlanarFace face:
                    {
                        if (Util.IsVertical(face))
                            verticalFaces.Add(face);
                        break;
                    }
                    case CylindricalFace cylindricalFace:
                    {
                        if (Util.IsVertical(cylindricalFace))
                            verticalFaces.Add(cylindricalFace);
                        break;
                    }
                }
        }
    }
}